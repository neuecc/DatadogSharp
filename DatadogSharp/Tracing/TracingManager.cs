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

        public TracingScope BeginTracing(string name, string resource, string service, string type)
        {
            return new TracingScope(name, resource, service, type, this);
        }

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

    public interface ITracingScope : IDisposable
    {
        ulong TraceId { get; }
        string Name { get; }
        string Resource { get; }
        string Service { get; }
        string Type { get; }

        ITracingScope BeginSpan(string name, string resource, string service, string type);
        ITracingScope WithError();
        ITracingScope WithMeta(Dictionary<string, string> meta);
    }

    public class TracingScope : ITracingScope
    {
        public ulong TraceId { get; private set; }
        public string Name { get; private set; }
        public string Resource { get; private set; }
        public string Service { get; private set; }
        public string Type { get; private set; }

        readonly ulong spanId;
        readonly ulong start;
        readonly ulong? parentId;

        Stopwatch duration;
        int? error = null;
        Dictionary<string, string> meta = null;

        StructBuffer<Span> spans;
        TracingManager manager;

        readonly object gate = new object();

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
            this.spanId = Span.BuildRandomId();
            this.start = Span.ToNanoseconds(DateTime.UtcNow);
            this.duration = ThreadSafeUtil.RentStopwatchStartNew();
            this.manager = manager;
            this.spans = new StructBuffer<Span>(4);
            this.parentId = parentId;
        }

        public void AddSpan(Span span)
        {
            // thread safe only adding
            lock (gate)
            {
                this.spans.Add(ref span);
            }
        }

        public ITracingScope BeginSpan(string name, string resource, string service, string type)
        {
            return new SpanScope(name, resource, service, type, spanId, this);
        }

        public ITracingScope WithError()
        {
            error = 1;
            return this;
        }

        public ITracingScope WithMeta(Dictionary<string, string> meta)
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
                SpanId = spanId,
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

    public class SpanScope : ITracingScope
    {
        public ulong TraceId { get; }
        public string Name { get; }
        public string Resource { get; }
        public string Service { get; }
        public string Type { get; }
        readonly ulong parentId;
        readonly ulong spanId;
        readonly ulong start;
        readonly TracingScope rootScope;

        Stopwatch duration;
        int? error = null;
        Dictionary<string, string> meta = null;

        internal SpanScope(string name, string resource, string service, string type, ulong parentId, TracingScope rootScope)
        {
            this.TraceId = rootScope.TraceId;
            this.Name = name;
            this.Resource = resource;
            this.Service = service;
            this.Type = type;
            this.parentId = parentId;
            this.start = Span.ToNanoseconds(DateTime.UtcNow);
            this.duration = ThreadSafeUtil.RentStopwatchStartNew();
            this.spanId = Span.BuildRandomId();
            this.rootScope = rootScope;
        }

        public ITracingScope BeginSpan(string name, string resource, string service, string type)
        {
            return new SpanScope(name, resource, service, type, spanId, rootScope);
        }

        public ITracingScope WithError()
        {
            error = 1;
            return this;
        }

        public ITracingScope WithMeta(Dictionary<string, string> meta)
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
                SpanId = spanId,
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

            rootScope.AddSpan(span);

            ThreadSafeUtil.ReturnStopwatch(duration);
            duration = null;
        }
    }
}
