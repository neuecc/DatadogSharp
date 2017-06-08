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

        readonly ulong spanId;
        readonly ulong start;
        Stopwatch duration;
        int? error = null;
        Dictionary<string, string> meta = null;

        StructBuffer<Span> spans;
        TracingManager manager;

        public TracingScope(string name, string resource, string service, string type, TracingManager manager)
        {
            this.Name = name;
            this.Resource = resource;
            this.Service = service;
            this.Type = type;
            this.TraceId = Span.BuildRandomId();
            this.spanId = Span.BuildRandomId();
            this.start = Span.ToNanoseconds(DateTime.UtcNow);
            this.duration = ThreadSafeUtil.RentStopwatchStartNew();
            this.manager = manager;
            this.spans = new StructBuffer<Span>(4);
        }

        public void AddSpan(Span span)
        {
            this.spans.Add(ref span);
        }

        public SpanScope BeginSpan(string name, string resource, string service, string type)
        {
            return new SpanScope(name, resource, service, type, spanId, this);
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
            spans.Add(ref span);

            ThreadSafeUtil.ReturnStopwatch(duration);
            duration = null;

            var array = spans.ToArray();
            manager.EnqueueToWorker(array);
        }
    }

    public class SpanScope : IDisposable
    {
        readonly string name;
        readonly string resource;
        readonly string service;
        readonly string type;
        readonly ulong parentId;
        readonly ulong spanId;
        readonly ulong start;
        readonly TracingScope rootScope;

        Stopwatch duration;
        int? error = null;
        Dictionary<string, string> meta = null;

        internal SpanScope(string name, string resource, string service, string type, ulong parentId, TracingScope rootScope)
        {
            this.name = name;
            this.resource = resource;
            this.service = service;
            this.type = type;
            this.parentId = parentId;
            this.start = Span.ToNanoseconds(DateTime.UtcNow);
            this.duration = ThreadSafeUtil.RentStopwatchStartNew();
            this.spanId = Span.BuildRandomId();
            this.rootScope = rootScope;
        }

        public SpanScope BeginSpan(string name, string resource, string service, string type)
        {
            return new SpanScope(name, resource, service, type, spanId, rootScope);
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
                TraceId = rootScope.TraceId,
                SpanId = spanId,
                Name = name,
                Resource = resource,
                Service = service,
                Type = type,
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
