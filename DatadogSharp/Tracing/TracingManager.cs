using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DatadogSharp.Tracing
{
    public class TracingManager
    {
        public static TracingManager Default = new TracingManager();
        internal AsyncQueueWorker worker;

        public TracingManager()
        {
            worker = new AsyncQueueWorker(new DatadogClient());
        }

        public TracingManager(DatadogClient client, int bufferingCount = 10, int bufferingTimeMilliseconds = 5000, Action<Exception> logException = null)
        {
            worker = new AsyncQueueWorker(client, bufferingCount, bufferingTimeMilliseconds, logException);
        }

        /// <summary>
        /// Begin the root tracing, when TracingScope.Dispose, finish measurement and enqueue to send worker.
        /// </summary>
        /// <param name="name">The span name.</param>
        /// <param name="resource">The resource you are tracing such as "/Home/Index", "/Article/Post".</param>
        /// <param name="service">The service name such as "webservice", "batch", "mysql", "redis".</param>
        /// <param name="type">The type of request such as "web", "db", "cache".</param>
        public TracingScope BeginTracing(string name, string resource, string service, string type)
        {
            return new TracingScope(name, resource, service, type, this);
        }

        /// <summary>
        /// Begin the root tracing, when TracingScope.Dispose, finish measurement and enqueue to send worker.
        /// When use distributed monitoring, set traceId and parentId from other machine.
        /// </summary>
        /// <param name="name">The span name.</param>
        /// <param name="resource">The resource you are tracing such as "/Home/Index", "/Article/Post".</param>
        /// <param name="service">The service name such as "webservice", "batch", "mysql", "redis".</param>
        /// <param name="type">The type of request such as "web", "db", "cache".</param>
        /// <param name="traceId">TraceId from other machine.</param>
        /// <param name="parentId">ParentId of tracing.</param>
        public TracingScope BeginTracing(string name, string resource, string service, string type, ulong traceId, ulong parentId)
        {
            return new TracingScope(name, resource, service, type, this, traceId, parentId);
        }

        public void EnqueueToWorker(Span[] tracing)
        {
            worker.Enqueue(tracing);
        }

        public void SetBufferingParameter(int bufferingCount, int bufferingTimeMilliseconds)
        {
            worker.SetBufferingParameter(bufferingCount, bufferingTimeMilliseconds);
        }

        public void SetExceptionLogger(Action<Exception> logger)
        {
            worker.SetExceptionLogger(logger);
        }

        /// <summary>
        /// Finish worker, flush all traces and wait complete.
        /// </summary>
        public void Complete(TimeSpan waitTimeout)
        {
            worker.Complete(waitTimeout);
        }
    }

    public class TracingScope : IDisposable
    {
        public ulong TraceId { get; private set; }
        public string Name { get; private set; }
        public string Resource { get; private set; }
        public string Service { get; private set; }
        public string Type { get; private set; }
        public ulong SpanId { get; private set; }

        readonly ulong start;
        readonly ulong? parentId;

        Stopwatch duration;
        int? error = null;
        Dictionary<string, string> meta = null;

        StructBuffer<Span> spans;
        TracingManager manager;

        readonly object gate = new object();

        // managing ambient global
        // Note:concurrent child count is small in most cases, List is almost fast.
        List<SpanScope> ambientManageQueue = null;

        public TracingScope(string name, string resource, string service, string type, TracingManager manager)
            : this(name, resource, service, type, manager, Span.BuildRandomId(), null)
        {
        }

        public TracingScope(string name, string resource, string service, string type, TracingManager manager, ulong traceId, ulong? parentId)
        {
            this.Name = name;
            this.Resource = resource;
            this.Service = service;
            this.Type = type;
            this.TraceId = traceId;
            this.SpanId = Span.BuildRandomId();
            this.start = Span.ToNanoseconds(DateTime.UtcNow);
            this.duration = ThreadSafeUtil.RentStopwatchStartNew();
            this.manager = manager;
            this.spans = new StructBuffer<Span>(4);
            this.parentId = parentId;
        }

        public void AddSpan(Span span)
        {
            // thread safe adding
            lock (gate)
            {
                this.spans.Add(ref span);
            }
        }

        internal void AddSpanAndRemoveAmbient(Span span, SpanScope scope)
        {
            lock (gate)
            {
                this.spans.Add(ref span);

                if (ambientManageQueue != null && ambientManageQueue.Count != 0)
                {
                    ambientManageQueue.Remove(scope);
                }
            }
        }

        /// <summary>
        /// Begin the child tracing, when SpanScope.Dispose, finish measurement and add span to root tracing.
        /// </summary>
        /// <param name="name">The span name.</param>
        /// <param name="resource">The resource you are tracing such as "/Home/Index", "/Article/Post".</param>
        /// <param name="service">The service name such as "webservice", "batch", "mysql", "redis".</param>
        /// <param name="type">The type of request such as "web", "db", "cache".</param>
        public SpanScope BeginSpan(string name, string resource, string service, string type)
        {
            lock (gate)
            {
                if (ambientManageQueue == null || ambientManageQueue.Count == 0)
                {
                    return new SpanScope(name, resource, service, type, SpanId, this, false);
                }
                else
                {
                    var lastChild = ambientManageQueue[ambientManageQueue.Count - 1];
                    return lastChild.BeginSpan(name, resource, service, type);
                }
            }
        }

        /// <summary>
        /// Replace returned child scope as ambient root.
        /// </summary>
        /// <param name="name">The span name.</param>
        /// <param name="resource">The resource you are tracing such as "/Home/Index", "/Article/Post".</param>
        /// <param name="service">The service name such as "webservice", "batch", "mysql", "redis".</param>
        /// <param name="type">The type of request such as "web", "db", "cache".</param>
        public SpanScope BeginSpanAndChangeAmbientScope(string name, string resource, string service, string type)
        {
            lock (gate)
            {
                if (ambientManageQueue == null) ambientManageQueue = new List<SpanScope>(4);

                SpanScope scope;
                if (ambientManageQueue.Count == 0)
                {
                    scope = new SpanScope(name, resource, service, type, SpanId, this, true);
                }
                else
                {
                    var lastChild = ambientManageQueue[ambientManageQueue.Count - 1];
                    scope = lastChild.BeginSpan(name, resource, service, type, true);
                }

                ambientManageQueue.Add(scope);
                return scope;
            }
        }

        public TracingScope WithError()
        {
            error = 1;
            return this;
        }

        public TracingScope WithMeta(Dictionary<string, string> meta)
        {
            this.meta = meta;
            return this;
        }

        public void Dispose()
        {
            if (duration == null) throw new ObjectDisposedException("already disposed");
            duration.Stop();

            var span = new Span
            {
                TraceId = TraceId,
                SpanId = SpanId,
                Name = Name,
                Resource = Resource,
                Service = Service,
                Type = Type,
                Start = start,
                Duration = Span.ToNanoseconds(duration.Elapsed),
                ParentId = null,
                Error = error,
                Meta = meta,
            };

            ThreadSafeUtil.ReturnStopwatch(duration);
            duration = null;

            Span[] array;
            lock (gate)
            {
                spans.Add(ref span);
                array = spans.ToArray();
            }
            manager.EnqueueToWorker(array);
        }
    }

    public class SpanScope : IDisposable
    {
        public ulong TraceId { get; }
        public string Name { get; }
        public string Resource { get; }
        public string Service { get; }
        public string Type { get; }
        public ulong SpanId { get; }

        readonly ulong parentId;
        readonly ulong start;
        readonly TracingScope rootScope;
        readonly bool isAmbientGlobal;

        Stopwatch duration;
        int? error = null;
        Dictionary<string, string> meta = null;

        internal SpanScope(string name, string resource, string service, string type, ulong parentId, TracingScope rootScope, bool isAmbientGlobal)
        {
            this.TraceId = rootScope.TraceId;
            this.Name = name;
            this.Resource = resource;
            this.Service = service;
            this.Type = type;
            this.parentId = parentId;
            this.start = Span.ToNanoseconds(DateTime.UtcNow);
            this.duration = ThreadSafeUtil.RentStopwatchStartNew();
            this.SpanId = Span.BuildRandomId();
            this.rootScope = rootScope;
            this.isAmbientGlobal = isAmbientGlobal;
        }

        /// <summary>
        /// Begin the child tracing, when SpanScope.Dispose, finish measurement and add span to root tracing.
        /// </summary>
        /// <param name="name">The span name.</param>
        /// <param name="resource">The resource you are tracing such as "/Home/Index", "/Article/Post".</param>
        /// <param name="service">The service name such as "webservice", "batch", "mysql", "redis".</param>
        /// <param name="type">The type of request such as "web", "db", "cache".</param>
        public SpanScope BeginSpan(string name, string resource, string service, string type)
        {
            return new SpanScope(name, resource, service, type, SpanId, rootScope, false);
        }

        internal SpanScope BeginSpan(string name, string resource, string service, string type, bool isAmbientGlobal)
        {
            return new SpanScope(name, resource, service, type, SpanId, rootScope, isAmbientGlobal);
        }

        public SpanScope WithError()
        {
            error = 1;
            return this;
        }

        public SpanScope WithMeta(Dictionary<string, string> meta)
        {
            this.meta = meta;
            return this;
        }

        public void Dispose()
        {
            if (duration == null) throw new ObjectDisposedException("already disposed");
            duration.Stop();

            var span = new Span
            {
                TraceId = TraceId,
                SpanId = SpanId,
                Name = Name,
                Resource = Resource,
                Service = Service,
                Type = Type,
                Start = start,
                Duration = Span.ToNanoseconds(duration.Elapsed),
                ParentId = parentId,
                Error = error,
                Meta = meta,
            };

            if (isAmbientGlobal)
            {
                rootScope.AddSpanAndRemoveAmbient(span, this);
            }
            else
            {
                rootScope.AddSpan(span);
            }

            ThreadSafeUtil.ReturnStopwatch(duration);
            duration = null;
        }
    }
}
