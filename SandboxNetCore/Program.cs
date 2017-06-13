using DatadogSharp.Tracing;
using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
            //var scope = TracingManager.Default.BeginTracing("hoge", "huga", "tako", "nano");


            //var scopes = new ConcurrentQueue<SpanScope>();

            //Parallel.For(0, 100, i =>
            //{
            //    var hoge = scope.BeginSpanAndChangeAmbientScope("hoge", "huga", "tako", "nano");
            //    scopes.Enqueue(hoge);
            //});


            //Console.WriteLine("tako");


            //var huga = scope.BeginSpan("hoge", "huga", "tako", "nano");

            //Parallel.ForEach(scopes, x => x.Dispose());

            //huga.Dispose();

            //Console.WriteLine("nano");

            //var aahuga = scope.BeginSpanAndChangeAmbientScope("hoge", "huga", "tako", "nano");
            // TracingManager.Default.Complete

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