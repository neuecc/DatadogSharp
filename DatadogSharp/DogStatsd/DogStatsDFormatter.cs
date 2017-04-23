using System;
using System.Globalization;
using System.Text;

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

        static string BuildMetrics(string metricName, string value, string type, double sampleRate, string[] tags)
        {
            if (sampleRate == 1.0)
            {
                if (tags == null || tags.Length == 0)
                {
                    return metricName + ":" + value + "|" + type;
                }
                else
                {
                    var sb = ThreadSafeUtil.RentThreadStaticStringBuilder();
                    sb.Append(metricName);
                    sb.Append(":");
                    sb.Append(value);
                    sb.Append("|");
                    sb.Append(type);
                    sb.Append("|#");
                    for (int i = 0; i < tags.Length; i++)
                    {
                        if (i != 0) sb.Append(",");
                        sb.Append(tags[i]);
                    }

                    return sb.ToString();
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
                    var sb = ThreadSafeUtil.RentThreadStaticStringBuilder();
                    sb.Append(metricName);
                    sb.Append(":");
                    sb.Append(value);
                    sb.Append("|");
                    sb.Append(type);
                    sb.Append("|@");
                    sb.Append(sampleRate.ToString(InvaliantCultrue));
                    sb.Append("|#");
                    for (int i = 0; i < tags.Length; i++)
                    {
                        if (i != 0) sb.Append(",");
                        sb.Append(tags[i]);
                    }

                    return sb.ToString();
                }
            }
        }

        public static string Counter(string metricName, long value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, value.ToString(InvaliantCultrue), "c", sampleRate, tags);
        }

        public static string Counter(string metricName, double value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, Math.Round(value, 3).ToString(InvaliantCultrue), "c", sampleRate, tags);
        }

        public static string Gauge(string metricName, long value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, value.ToString(InvaliantCultrue), "g", sampleRate, tags);
        }

        public static string Gauge(string metricName, double value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, Math.Round(value, 3).ToString(InvaliantCultrue), "g", sampleRate, tags);
        }

        public static string Histogram(string metricName, long value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, value.ToString(InvaliantCultrue), "h", sampleRate, tags);
        }

        public static string Histogram(string metricName, double value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, Math.Round(value, 3).ToString(InvaliantCultrue), "h", sampleRate, tags);
        }

        public static string Timer(string metricName, long value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, value.ToString(InvaliantCultrue), "ms", sampleRate, tags);
        }

        public static string Timer(string metricName, double value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, Math.Round(value, 3).ToString(InvaliantCultrue), "ms", sampleRate, tags);
        }

        public static string Set(string metricName, long value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, value.ToString(InvaliantCultrue), "s", sampleRate, tags);
        }

        public static string Set(string metricName, double value, double sampleRate, string[] tags)
        {
            return BuildMetrics(metricName, Math.Round(value, 3).ToString(InvaliantCultrue), "s", sampleRate, tags);
        }

        // _e{title.length,text.length}:title|text|d:date_happened|h:hostname|p:priority|t:alert_type|#tag1,tag2

        public static string Event(string title, string text, int? dateHappened = null, string hostName = null, string aggregationKey = null, Priority priority = Priority.Normal, string sourceTypeName = null, AlertType alertType = AlertType.Info, string[] tags = null, bool truncateText = true)
        {
            var sb = ThreadSafeUtil.RentThreadStaticStringBuilder();

            sb.Append("_e{");
            sb.Append(title.Length.ToString(InvaliantCultrue));
            sb.Append(",");
            sb.Append(text.Length.ToString(InvaliantCultrue));
            sb.Append("}:");

            sb.Append(title);
            sb.Append("|");
            if (truncateText && text.Length > 4096)
            {
                sb.Append(text, 0, 4096); // optimize truncate.
            }
            else
            {
                sb.Append(text);
            }

            // Optional
            if (dateHappened != null)
            {
                sb.Append("|d:");
                sb.Append(dateHappened.Value.ToString(InvaliantCultrue));
            }

            if (hostName != null)
            {
                sb.Append("|h:");
                sb.Append(hostName);
            }

            if (aggregationKey != null)
            {
                sb.Append("|k:");
                sb.Append(aggregationKey);
            }

            if (priority != Priority.Normal)
            {
                sb.Append("|p:");
                sb.Append(priority.ToFormatName());
            }

            if (sourceTypeName != null)
            {
                sb.Append("|s:");
                sb.Append(sourceTypeName);
            }

            if (alertType != AlertType.Info)
            {
                sb.Append("|t:");
                sb.Append(alertType.ToFormatName());
            }

            if (tags != null && tags.Length != 0)
            {
                sb.Append("|#");
                for (int i = 0; i < tags.Length; i++)
                {
                    if (i != 0) sb.Append(",");
                    sb.Append(tags[i]);
                }
            }

            return sb.ToString();
        }

        // _sc|name|status|metadata

        public static string ServiceCheck(string name, string status, int? timestamp = null, string hostName = null, string[] tags = null, string serviceCheckMessage = null, bool truncateText = true)
        {
            var sb = ThreadSafeUtil.RentThreadStaticStringBuilder();

            sb.Append("_sc|");
            sb.Append(name);
            sb.Append("|");
            sb.Append(status);

            // Optional
            if (timestamp != null)
            {
                sb.Append("|d:");
                sb.Append(timestamp.Value.ToString(InvaliantCultrue));
            }

            if (hostName != null)
            {
                sb.Append("|h:");
                sb.Append(hostName);
            }

            if (tags != null && tags.Length != 0)
            {
                sb.Append("|#");
                for (int i = 0; i < tags.Length; i++)
                {
                    if (i != 0) sb.Append(",");
                    sb.Append(tags[i]);
                }
            }

            if (serviceCheckMessage != null)
            {
                sb.Append("|m");
                if (serviceCheckMessage.Length > 4096)
                {
                    sb.Append(serviceCheckMessage, 0, 4096);
                }
                else
                {
                    sb.Append(serviceCheckMessage);
                }
            }

            return sb.ToString();
        }
    }

    public enum AlertType
    {
        Info = 0,
        Error = 1,
        Warning = 2,
        Success = 3
    }

    public enum Priority
    {
        Normal = 0,
        Low = 1,
    }

    internal static class DogStatsDEnumExtensions
    {
        public static string ToFormatName(this AlertType type)
        {
            switch (type)
            {
                case AlertType.Info:
                    return "info";
                case AlertType.Error:
                    return "error";
                case AlertType.Warning:
                    return "warning";
                case AlertType.Success:
                    return "success";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public static string ToFormatName(this Priority p)
        {
            switch (p)
            {
                case Priority.Normal:
                    return "normal";
                case Priority.Low:
                    return "low";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }
    }
}