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
                Metrics = new Dictionary<string, double> { { "http.monitor", 41.99 } },
            };
        }

        static void Main(string[] args)
        {
            var client = new DatadogSharp.Tracing.DatadogClient();
            var rand = new Random();
            var traces = new List<Span>();

            //client.Services(new Service
            //{
            //    App = "myapp",
            //    AppType = "web",
            //    ServiceName = "high.throughput"
            //}).Wait();

            var tid = Span.BuildRandomId();
            Console.WriteLine(tid);
            for (int i = 0; i < 4; i++)
            {
                var test = new DatadogSharp.Tracing.Span
                {
                    TraceId = tid,
                    SpanId = Span.BuildRandomId(),
                    Name = "my_test_span" + i,
                    Resource = "/home",
                    Service = "testservice",
                    Start = Span.ToNanoseconds(DateTime.UtcNow.AddMilliseconds(rand.Next(0, 3000))),
                    Type = "web",
                    Duration = Span.ToNanoseconds(TimeSpan.FromMilliseconds(rand.Next(100, 300))),
                };

                traces.Add(test);
            }
            var bytes = MessagePackSerializer.Serialize(traces, DatadogSharpResolver.Instance);

            Console.WriteLine(MessagePackSerializer.ToJson(bytes));


            var v = client.Traces(new[] { traces.ToArray() }).Result;
            Console.WriteLine(v);
        }
    }
}