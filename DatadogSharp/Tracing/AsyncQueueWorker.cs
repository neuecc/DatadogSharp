using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DatadogSharp.Tracing
{
    internal class AsyncQueueWorker
    {
        // to datadog agent concurrent request
        const int ConcurrentRequestCountLimit = 4;

        // ConcurrentQueue vs locked simple array, I choose locked array.
        StructBuffer<Span[]> globalQueue = new StructBuffer<Span[]>(); // avoid readonly because this is mutable struct
        readonly object queueLock = new object();

        readonly DatadogClient client;

        Action<Exception> logException;
        TimeSpan bufferingTime;
        int bufferingCount;

        readonly Task processingTask;
        int isDisposed = 0;
        readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public AsyncQueueWorker(DatadogClient client, int bufferingCount = 10, int bufferingTimeMilliseconds = 5000, Action<Exception> logException = null)
        {
            this.globalQueue = new StructBuffer<Span[]>(16);
            this.client = client;
            this.logException = logException ?? ((Exception ex) => { });
            this.bufferingCount = bufferingCount;
            this.bufferingTime = TimeSpan.FromMilliseconds(bufferingTimeMilliseconds);

            // Start
            this.processingTask = Task.Factory.StartNew(ConsumeQueue, TaskCreationOptions.LongRunning).Unwrap();
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
                    Task waiter = null;
                    Span[][] singleTraces = null;
                    Span[][] multipleTraces = null;

                    lock (queueLock)
                    {
                        var rawEnqueuedArray = globalQueue.GetBuffer();

                        if (rawEnqueuedArray.Count == 0)
                        {
                            waiter = Task.Delay(bufferingTime, cancellationTokenSource.Token);
                        }
                        else if (rawEnqueuedArray.Count < bufferingCount)
                        {
                            singleTraces = new Span[rawEnqueuedArray.Count][];
                            Array.Copy(rawEnqueuedArray.Array, singleTraces, singleTraces.Length);

                            globalQueue.ClearStrict();
                        }
                        else
                        {
                            multipleTraces = new Span[rawEnqueuedArray.Count][];
                            Array.Copy(rawEnqueuedArray.Array, multipleTraces, multipleTraces.Length);

                            globalQueue.ClearStrict();
                        }
                    }

                    if (waiter != null)
                    {
                        await waiter.ConfigureAwait(false);
                    }
                    else if (singleTraces != null)
                    {
                        // does not pass cancellation token.
                        await client.Traces(singleTraces).ConfigureAwait(false);
                    }
                    else if (multipleTraces != null)
                    {
                        for (int i = 0; i < multipleTraces.Length;)
                        {
                            var tasks = new Task[Math.Min(ConcurrentRequestCountLimit, multipleTraces.Length - i)];
                            for (int j = 0; j < tasks.Length; j++)
                            {
                                var len = Math.Min(bufferingCount, multipleTraces.Length - i);
                                if (len <= 0)
                                {
                                    Array.Resize(ref tasks, j);
                                    break;
                                }

                                var segment = new ArraySegment<Span[]>(multipleTraces, i, len);
                                i += len;

                                tasks[j] = client.Traces(segment);
                            }

                            await Task.WhenAll(tasks).ConfigureAwait(false);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                }
                catch (OperationCanceledException)
                {
                    await Task.Delay(bufferingTime, cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    await Task.Delay(bufferingTime, cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logException(ex);
                    await Task.Delay(bufferingTime, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
        }

        public void Enqueue(Span[] value)
        {
            lock (queueLock)
            {
                globalQueue.Add(ref value);
            }
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
                    Span[][] lastTraces = null;
                    lock (queueLock)
                    {
                        var rawEnqueuedArray = globalQueue.GetBuffer();

                        if (rawEnqueuedArray.Count != 0)
                        {
                            lastTraces = new Span[rawEnqueuedArray.Count][];
                            Array.Copy(rawEnqueuedArray.Array, lastTraces, lastTraces.Length);
                        }
                    }

                    if (lastTraces != null)
                    {
                        client.Traces(lastTraces).Wait(waitTimeout);
                    }
                }
                catch (Exception ex)
                {
                    logException(ex);
                }
            }
        }
    }
}