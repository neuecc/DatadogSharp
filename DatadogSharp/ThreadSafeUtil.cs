using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace DatadogSharp
{
    internal static class ThreadSafeUtil
    {
        [ThreadStatic]
        static Random random;

        public static Random ThreadStaticRandom
        {
            get
            {
                if (random == null)
                {
                    using (var rng = RandomNumberGenerator.Create())
                    {
                        var buffer = new byte[sizeof(int)];
                        rng.GetBytes(buffer);
                        var seed = BitConverter.ToInt32(buffer, 0);
                        random = new Random(seed);
                    }
                }

                return random;
            }
        }

        [ThreadStatic]
        static StringBuilder stringBuilder;

        public static StringBuilder RentThreadStaticStringBuilder()
        {
            if (stringBuilder == null)
            {
                stringBuilder = new StringBuilder();
            }
            else
            {
                stringBuilder.Clear();
            }

            return stringBuilder;
        }

        static ConcurrentQueue<Stopwatch> stopwatchPool = new ConcurrentQueue<Stopwatch>();

        internal static Stopwatch RentStopwatchStartNew()
        {
            if (stopwatchPool.TryDequeue(out var sw))
            {
                sw.Restart();
                return sw;
            }
            else
            {
                return Stopwatch.StartNew();
            }
        }

        internal static void ReturnStopwatch(Stopwatch sw)
        {
            stopwatchPool.Enqueue(sw);
        }
    }
}