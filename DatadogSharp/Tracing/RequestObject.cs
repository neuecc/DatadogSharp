using DatadogSharp.DogStatsd;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatadogSharp.Tracing
{
    // Tracing API Requests
    // http://docs.datadoghq.com/tracing/api/

    [MessagePackObject]
    [MessagePackFormatter(typeof(SpanFormatter))]
    public class Span
    {
        // Required.

        /// <summary>Required. The unique integer (64-bit unsigned) ID of the trace containing this span.</summary>
        [Key("trace_id")]
        public ulong TraceId { get; set; }
        /// <summary>Required. The unique integer (64-bit unsigned) ID of the trace containing this span.</summary>
        [Key("span_id")]
        public ulong SpanId { get; set; }
        /// <summary>Required.The span name.</summary>
        [Key("name")]
        public string Name { get; set; }
        /// <summary>Required.The resource you are tracing.</summary>
        [Key("resource")]
        public string Resource { get; set; }
        /// <summary>Required.The service name.</summary>
        [Key("service")]
        public string Service { get; set; }
        /// <summary>Required.The type of request.</summary>
        [Key("type")]
        public string Type { get; set; }
        /// <summary>Required.The start time of the request in nanoseconds from the unix epoch.</summary>
        [Key("start")]
        public ulong Start { get; set; }
        /// <summary>Required.The duration of the request in nanoseconds.</summary>
        [Key("duration")]
        public ulong Duration { get; set; }

        // Optional.

        /// <summary>Optional.The span integer ID of the parent span.</summary>
        [Key("parent_id")]
        public ulong? ParentId { get; set; }
        /// <summary>Optional.Set this value to 1 to indicate if an error occured.If an error occurs, you should pass additional information, such as the error message, type and stack information in the meta property.</summary>
        [Key("error")]
        public int? Error { get; set; }
        /// <summary>Optional.A dictionary of Key-value metadata. e.g.tags.</summary>
        [Key("meta")]
        public Dictionary<string, string> Meta { get; set; }

        public static ulong BuildRandomId()
        {
            // Note:should return true ulong random.
            return (ulong)ThreadSafeUtil.ThreadStaticRandom.Next(0, int.MaxValue);
        }

        public static ulong ToNanoseconds(DateTime dateTime)
        {
            var seconds = (dateTime.Ticks / TimeSpan.TicksPerSecond) - 62135596800;
            var nanoseconds = (dateTime.Ticks % TimeSpan.TicksPerSecond) * 100;
            checked
            {
                return (ulong)(seconds * 1000000000 + nanoseconds);
            }
        }

        public static ulong ToNanoseconds(TimeSpan timeSpan)
        {
            var seconds = (timeSpan.Ticks / TimeSpan.TicksPerSecond);
            var nanoseconds = (timeSpan.Ticks % TimeSpan.TicksPerSecond) * 100;
            checked
            {
                return (ulong)(seconds * 1000000000 + nanoseconds);
            }
        }

        public override string ToString()
        {
            return $"TraceId:{TraceId} SpanId:{SpanId}";
        }
    }

    [MessagePackObject]
    [MessagePackFormatter(typeof(ServiceFormatter))]
    public class Service
    {
        [IgnoreMember]
        public string ServiceName { get; set; }
        [Key("app")]
        public string App { get; set; }
        [Key("app_type")]
        public string AppType { get; set; }
    }
}
