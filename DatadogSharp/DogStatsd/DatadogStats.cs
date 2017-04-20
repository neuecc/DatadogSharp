using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DatadogSharp.DogStatsd
{
    public class DatadogStats : IDisposable
    {
        public static DatadogStats Default { get; private set; } = new DatadogStats();

        public static void ConfigureDefault(string name, int port, string metricNamePrefix = null, string[] defaultTags = null)
        {
            Default.Dispose();
            Default = new DatadogStats(name, port, metricNamePrefix, defaultTags);
        }

        bool isNull;
        string metricNamePrefix = null;
        string defaultTags = null;
        IPEndPoint endPoint;
        Socket udpSocket;
        Encoding utf8 = Encoding.UTF8;

        DatadogStats()
        {
            isNull = true;
        }

        public DatadogStats(string name, int port, string metricNamePrefix = null, string[] defaultTags = null)
        {
            this.metricNamePrefix = metricNamePrefix;
            if (defaultTags != null)
            {
                this.defaultTags = string.Join(",", defaultTags);
            }

            this.udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.endPoint = new IPEndPoint(IPAddress.Parse(name), port);
        }

        public string WithPrefix(string name)
        {
            if (string.IsNullOrEmpty(metricNamePrefix)) return name;

            return metricNamePrefix + "." + name;
        }

        public string WithDefaultTag(string tag)
        {
            if (defaultTags == null)
            {
                return tag;
            }
            else
            {
                if (tag == null || tag.Length == 0) return defaultTags;

                return tag + "," + defaultTags;
            }
        }

        public string WithDefaultTag(string[] tag)
        {
            if (defaultTags == null)
            {
                return string.Join(",", tag);
            }
            else
            {
                if (tag == null || tag.Length == 0) return defaultTags;

                return string.Join(",", tag) + "," + defaultTags;
            }
        }

        public void Send(string command)
        {
            Send(utf8.GetBytes(command));
        }

        public void Send(byte[] command)
        {
            udpSocket.SendTo(command, 0, command.Length, SocketFlags.None, endPoint);
        }

        public void Increment()
        {

        }

        public void Decrement() { }
        public void Counter() { }

        public void Gauge(string metricName, long value, double sampleRate = 1.0, string[] tags = null)
        {
            if (isNull) return;
            var command = DogStatsDFormatter.Gauge(metricName, value, sampleRate, WithDefaultTag(tags));
            Send(command);
        }

        public void Gauge(string metricName, long value, double sampleRate = 1.0, string tags = null)
        {
            if (isNull) return;
            var command = DogStatsDFormatter.Gauge(metricName, value, sampleRate, WithDefaultTag(tags));
            Send(command);
        }

        public void Histogram() { }
        public void Set() { }
        public void Time() { }

        public void Dispose()
        {
            if (isNull) return;
            this.udpSocket.Dispose();
            isNull = true;
        }
    }
}