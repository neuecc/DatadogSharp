using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Text;

namespace DatadogSharp.Tracing
{
    public class DatadogSharpResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance = new DatadogSharpResolver();

        DatadogSharpResolver()
        {

        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(Span))
                {
                    formatter = (IMessagePackFormatter<T>)(object)SpanFormatter.Instance;
                }
                else if (typeof(T) == typeof(Service))
                {
                    formatter = (IMessagePackFormatter<T>)(object)ServiceFormatter.Instance;
                }
                else
                {
                    formatter = StandardResolver.Instance.GetFormatter<T>();
                }
            }
        }
    }

    public sealed class SpanFormatter : global::MessagePack.Formatters.IMessagePackFormatter<Span>
    {
        public static readonly IMessagePackFormatter<Span> Instance = new SpanFormatter();

        SpanFormatter()
        {

        }

        static byte[][] keyNameBytes = new[]
        {
            Encoding.UTF8.GetBytes("trace_id"),
            Encoding.UTF8.GetBytes("span_id"),
            Encoding.UTF8.GetBytes("name"),
            Encoding.UTF8.GetBytes("resource"),
            Encoding.UTF8.GetBytes("service"),
            Encoding.UTF8.GetBytes("type"),
            Encoding.UTF8.GetBytes("start"),
            Encoding.UTF8.GetBytes("duration"),
            Encoding.UTF8.GetBytes("parent_id"),
            Encoding.UTF8.GetBytes("error"),
            Encoding.UTF8.GetBytes("meta"),
            Encoding.UTF8.GetBytes("metrics"),
        };

        public int Serialize(ref byte[] bytes, int offset, Span value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var startOffset = offset;

            // Optimized headersize.
            var headerSize = 8;
            if (value.ParentId != null) headerSize++;
            if (value.Error != null) headerSize++;
            if (value.Meta != null) headerSize++;
            if (value.Metrics != null) headerSize++;
            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, headerSize);

            // Required.
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[0]);
            offset += MessagePackBinary.WriteUInt64(ref bytes, offset, value.TraceId);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[1]);
            offset += MessagePackBinary.WriteUInt64(ref bytes, offset, value.SpanId);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[2]);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.Name);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[3]);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.Resource);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[4]);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.Service);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[5]);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.Type);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[6]);
            offset += MessagePackBinary.WriteUInt64(ref bytes, offset, value.Start);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[7]);
            offset += MessagePackBinary.WriteUInt64(ref bytes, offset, value.Duration);

            // Optional.
            if (value.ParentId != null)
            {
                offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[8]);
                offset += MessagePackBinary.WriteUInt64(ref bytes, offset, value.ParentId.Value);
            }
            if (value.Error != null)
            {
                offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[9]);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, value.Error.Value);
            }
            if (value.Meta != null)
            {
                offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[10]);
                offset += formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.Dictionary<string, string>>().Serialize(ref bytes, offset, value.Meta, formatterResolver);
            }
            if (value.Metrics != null)
            {
                offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[11]);
                offset += formatterResolver.GetFormatterWithVerify<global::System.Collections.Generic.Dictionary<string, double>>().Serialize(ref bytes, offset, value.Metrics, formatterResolver);
            }
            return offset - startOffset;
        }

        public Span Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            throw new NotSupportedException("Supports serialize only.");
        }
    }

    public sealed class ServiceFormatter : global::MessagePack.Formatters.IMessagePackFormatter<Service>
    {
        public static readonly IMessagePackFormatter<Service> Instance = new ServiceFormatter();

        ServiceFormatter()
        {

        }

        static byte[][] keyNameBytes = new[]
        {
            Encoding.UTF8.GetBytes("app"),
            Encoding.UTF8.GetBytes("app_type"),
        };

        public int Serialize(ref byte[] bytes, int offset, Service value, global::MessagePack.IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return global::MessagePack.MessagePackBinary.WriteNil(ref bytes, offset);
            }

            var startOffset = offset;
            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 1);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.ServiceName);

            offset += global::MessagePack.MessagePackBinary.WriteFixedMapHeaderUnsafe(ref bytes, offset, 2);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[0]);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.App);
            offset += global::MessagePack.MessagePackBinary.WriteStringBytes(ref bytes, offset, keyNameBytes[1]);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.AppType);

            return offset - startOffset;
        }

        public Service Deserialize(byte[] bytes, int offset, global::MessagePack.IFormatterResolver formatterResolver, out int readSize)
        {
            throw new NotSupportedException("Supports serialize only.");
        }
    }
}
