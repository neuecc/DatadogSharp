using System;
using System.Globalization;

namespace DatadogSharp.DogStatsd
{
    /// <summary>
    /// http://docs.datadoghq.com/guides/dogstatsd/#datagram-format
    /// </summary>
    public static class DogStatsDFormatter
    {
        static readonly IFormatProvider InvaliantCultrue = CultureInfo.InvariantCulture;

        // metric.name:value|type|@sample_rate|#tag1:value,tag2

        // metric.name should be a String with no colons, bars or @ characters and fit our naming policy.
        // value should be a number
        // type should be c for Counter, g for Gauge, h for Histogram, ms for Timer or s for Set.
        // sample rate is optional and should be a float between 0 and 1 inclusive.
        // tags are optional, and should be a comma seperated list of tags.Colons are used for key value tags.Note that the key device is reserved, tags like “device:xyc” will be dropped by Datadog.

        static string Build(string metricName, string value, string type, double sampleRate, string tags)
        {
            if (sampleRate == 1.0)
            {
                if (tags == null || tags.Length == 0)
                {
                    return metricName + ":" + value + "|" + type;
                }
                else
                {
                    return metricName + ":" + value + "|" + type + "|#" + tags;
                }
            }
            else
            {
                if (tags == null || tags.Length == 0)
                {
                    return metricName + ":" + value + "|" + type + "|@" + sampleRate.ToString(InvaliantCultrue);
                }
                else
                {
                    return metricName + ":" + value + "|" + type + "|@" + sampleRate.ToString(InvaliantCultrue) + "|#" + tags;
                }
            }
        }

        public static string Gauge(string metricName, long value, double sampleRate, string tags)
        {
            return Build(metricName, value.ToString(InvaliantCultrue), "g", sampleRate, tags);
        }

        public static string Gauge(string metricName, double value, double sampleRate, string tags)
        {
            return Build(metricName, value.ToString(InvaliantCultrue), "g", sampleRate, tags);
        }


        //public static string Counters(string metricName, string value)
        //{
        //    return metricName + ":" + value + "|" + "g";
        //}
        //public static string Gauge(string metricName, string value)
        //{
        //    return metricName + ":" + value + "|" + "g";
        //}
        //public static string Timers(string metricName, string value)
        //{
        //    return metricName + ":" + value + "|" + "g";
        //}
        //public static string Gauge(string metricName, string value)
        //{
        //    return metricName + ":" + value + "|" + "g";
        //}
        //public static string Gauge(string metricName, string value)
        //{
        //    return metricName + ":" + value + "|" + "g";
        //}
    }
}