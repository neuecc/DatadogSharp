using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace DatadogSharp.Tracing
{
    public class DatadogClient : IDisposable
    {
        protected readonly HttpClient client;
        static readonly MediaTypeHeaderValue msgPackHeader = new MediaTypeHeaderValue("application/msgpack");

        public Uri BaseAddress
        {
            get { return client.BaseAddress; }
            set { client.BaseAddress = value; }
        }

        public HttpRequestHeaders DefaultRequestHeaders
        {
            get { return client.DefaultRequestHeaders; }
        }

        public long MaxResponseContentBufferSize
        {
            get { return client.MaxResponseContentBufferSize; }
            set { client.MaxResponseContentBufferSize = value; }
        }

        public TimeSpan Timeout
        {
            get { return client.Timeout; }
            set { client.Timeout = value; }
        }

        public DatadogClient()
            : this(new HttpClientHandler())
        {

        }

        public DatadogClient(HttpMessageHandler handler)
            : this(handler, true)
        {

        }

        public DatadogClient(HttpMessageHandler handler, bool disposeHandler)
        {
            this.client = new HttpClient(handler, disposeHandler);
            this.client.BaseAddress = new Uri("http://localhost:8126/");
        }

        public DatadogClient(HttpClient client)
        {
            this.client = client;
        }

        public Task<string> Traces(Span[][] traces, CancellationToken cancellationToken = default(CancellationToken))
        {
            var content = new ByteArrayContent(MessagePack.MessagePackSerializer.Serialize(traces, DatadogSharpResolver.Instance));
            content.Headers.ContentType = msgPackHeader;

            return Post("v0.3/traces", content, cancellationToken);
        }

        public Task<string> Services(Service service, CancellationToken cancellationToken = default(CancellationToken))
        {
            var content = new ByteArrayContent(MessagePack.MessagePackSerializer.Serialize(service, DatadogSharpResolver.Instance));
            content.Headers.ContentType = msgPackHeader;
            return Post("v0.3/services", content, cancellationToken);
        }

        async Task<string> Post(string api, ByteArrayContent content, CancellationToken cancellationToken)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, BaseAddress + api)
            {
                Content = content,
            };

            var response = await SendAsync(message, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError || response.StatusCode == HttpStatusCode.BadRequest)
            {
                var failMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new DatadogClientException(response.StatusCode, failMessage);
            }
            else
            {
                throw new DatadogClientException(response.StatusCode, "");
            }
        }

        public virtual Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return client.SendAsync(request, cancellationToken);
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }

    public class DatadogClientException : Exception
    {
        readonly HttpStatusCode statusCode;
        readonly string message;

        public DatadogClientException(HttpStatusCode statusCode, string message)
        {
            this.statusCode = statusCode;
            this.message = message;
        }

        public override string Message
        {
            get
            {
                return (int)statusCode + " " + message;
            }
        }
    }
}
