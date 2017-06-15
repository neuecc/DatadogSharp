using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DatadogSharp.Tracing
{
    internal class AsyncQueueWorker
    {
        readonly ConcurrentQueue<Span[]> q = new ConcurrentQueue<Span[]>();
        readonly DatadogClient client;
        Action<Exception> logException;

        TimeSpan bufferingTime;
        int bufferingCount;

        readonly Task processingTask;
        int isDisposed = 0;
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public AsyncQueueWorker(DatadogClient client, int bufferingCount = 10, int bufferingTimeMilliseconds = 5000, Action<Exception> logException = null)
        {
            this.processingTask = Task.Factory.StartNew(ConsumeQueue, TaskCreationOptions.LongRunning).Unwrap();
            this.client = client;
            this.logException = logException ?? ((Exception ex) => { });
            this.bufferingCount = bufferingCount;
            this.bufferingTime = TimeSpan.FromMilliseconds(bufferingTimeMilliseconds);
        }

        public void SetBufferingParameter(int bufferingCount, int bufferingTimeMilliseconds)
        {
            this.bufferingCount = bufferingCount;
            this.bufferingTime = TimeSpan.FromMilliseconds(bufferingTimeMilliseconds);
        }

        public void SetExceptionLogger(Action<Exception> logger)
        {
            this.logException = logger;
        }

        async Task ConsumeQueue()
        {
            var buffer = new StructBuffer<Span[]>(bufferingCount);
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var loopCount = bufferingCount;

                    buffer.Clear();
                    var addCount = 0;
                    for (int i = 0; i < loopCount; i++)
                    {
                        Span[] nextTrace;
                        if (q.TryDequeue(out nextTrace))
                        {
                            addCount++;
                            buffer.Add(ref nextTrace);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (addCount == 0)
                    {
                        try
                        {
                            await Task.Delay(bufferingTime, cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                    }
                    else
                    {
                        var traces = buffer.ToArray();
                        await client.Traces(traces).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logException(ex);
                }
            }
        }

        public void Enqueue(Span[] value)
        {
            q.Enqueue(value);
        }

        public void Complete(TimeSpan waitTimeout)
        {
            if (Interlocked.Increment(ref isDisposed) == 1)
            {
                try
                {
                    cancellationTokenSource.Cancel();
                    processingTask.Wait(waitTimeout);

                    // rest line...
                    var lastLine = q.ToArray();
                    client.Traces(lastLine).Wait(waitTimeout);
                }
                catch (Exception ex)
                {
                    logException(ex);
                }
            }
        }
    }
}