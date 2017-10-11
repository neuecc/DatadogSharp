using DatadogSharp.Tracing;
using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SandboxNetCore
{
    class Program
    {
        static Span GetTestSpan(ulong i)
        {
            return new Span
            {
                TraceId = i,
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
            Console.WriteLine("hoge");

            DatadogSharp.DogStatsd.DatadogStats.ConfigureDefault("127.0.0.1");

            var sendStr = File.ReadAllText(@"C:\Users\y.kawai\Documents\Visual Studio 2017\Projects\ConsoleApp116\bin\Debug\hoge.txt");


            DatadogSharp.DogStatsd.DatadogStats.Default.Event("hogehogehugahuga", sendStr);


        }

        //static Task RequestRedisAsync()
        //{



        //}
    }
}