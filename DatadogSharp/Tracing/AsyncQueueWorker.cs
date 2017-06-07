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
        readonly Action<Exception> logException;

        readonly TimeSpan bufferingTime;
        readonly int bufferingCount;

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

        async Task ConsumeQueue()
        {
            var buffer = new StructBuffer<Span[]>(bufferingCount);
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    buffer.Clear();
                    var addCount = 0;
                    for (int i = 0; i < bufferingCount; i++)
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
            }
        }

        public void Enqueue(Span[] value)
        {
            q.Enqueue(value);
        }

        public void Complete()
        {
            if (Interlocked.Increment(ref isDisposed) == 1)
            {
                cancellationTokenSource.Cancel();
                processingTask.Wait();
                try
                {
                    // rest line...
                    var lastLine = q.ToArray();
                    client.Traces(lastLine).Wait();
                }
                catch (Exception ex)
                {
                    logException(ex);
                }
            }
        }
    }
}