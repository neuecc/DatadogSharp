using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using DatadogSharp.DogStatsd;
using StatsdClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    [Config(typeof(Config))]
    public class MerticsCheck
    {
        public class Config : ManualConfig
        {
            public Config()
            {
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(new Job().WithLaunchCount(2).WithWarmupCount(2).WithTargetCount(10));
            }
        }

        [Setup]
        public void Setup()
        {
            global::DatadogSharp.DogStatsd.DatadogStats.ConfigureDefault("127.0.0.1", 8125, "myApp");

            var dogstatsdConfig = new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 8125, // Optional; default is 8125
                Prefix = "myApp" // Optional; by default no prefix will be prepended
            };
            StatsdClient.DogStatsd.Configure(dogstatsdConfig);
        }

        [Benchmark()]
        public void OfficialDogStatsD()
        {
            StatsdClient.DogStatsd.Gauge("testgauge", 10);
        }

        [Benchmark()]
        public void DatadogSharp()
        {
            global::DatadogSharp.DogStatsd.DatadogStats.Default.Gauge("testgauge", 10);
        }
    }


    [Config(typeof(Config))]
    public class EventCheck
    {
        public class Config : ManualConfig
        {
            public Config()
            {
                Add(new BenchmarkDotNet.Diagnosers.MemoryDiagnoser());
                Add(new Job().WithLaunchCount(2).WithWarmupCount(2).WithTargetCount(10));
            }
        }

        [Setup]
        public void Setup()
        {
            global::DatadogSharp.DogStatsd.DatadogStats.ConfigureDefault("127.0.0.1", 8125, "myApp");

            var dogstatsdConfig = new StatsdConfig
            {
                StatsdServerName = "127.0.0.1",
                StatsdPort = 8125, // Optional; default is 8125
                Prefix = "myApp" // Optional; by default no prefix will be prepended
            };
            StatsdClient.DogStatsd.Configure(dogstatsdConfig);
        }

        [Benchmark()]
        public void OfficialDogStatsD()
        {
            StatsdClient.DogStatsd.Event("testev", "aiueokakikukekonanonano");
        }

        [Benchmark()]
        public void DatadogSharp()
        {
            global::DatadogSharp.DogStatsd.DatadogStats.Default.Event("testev", "aiueokakikukekonanonano");
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<EventCheck>();


        }
    }
}
