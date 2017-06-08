using DatadogSharp.Tracing;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SandboxNetCore
{
    class Program
    {
        static Span GetTestSpan()
        {
            return new Span
            {
                TraceId = 42,
                SpanId = 52,
                ParentId = 42,
                Type = "web",
                Service = "high.throughput",
                Name = "sending.events",
                Resource = "SEND /data",
                Start = 1481215590883401105,
                Duration = 1000000000,
                Meta = new Dictionary<string, string> { { "http.host", "" } },
            };
        }

        static void Main(string[] args)
        {
            //using (var scope = TracingManager.Default.BeginTracing("my_test_trace_root", "/home/index", "Service2", "Web"))
            //{
            //    using (var parent = scope.BeginSpan("my_span", "Redis"))
            //    {
            //        using (parent.BeginSpan("my_span2", "/huga/tako", "BattleEngine", "Redis"))
            //        {
            //            Thread.Sleep(TimeSpan.FromSeconds(1));
            //        }
            //        Thread.Sleep(TimeSpan.FromSeconds(2));
            //    }

            //    Thread.Sleep(TimeSpan.FromSeconds(4.5));
            //}

            //Thread.Sleep(TimeSpan.FromSeconds(10));
            // TracingManager.Default.Complete();
        }
    }
}