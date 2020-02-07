using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal abstract class HttpMessageHandlerStage : HttpMessageHandler
    {
        protected internal sealed override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            SendAsync(request, false, cancellationToken).GetAwaiter().GetResult();

        protected internal sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            SendAsync(request, true, cancellationToken).AsTask();

        internal abstract ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async,
            CancellationToken cancellationToken);
    }
}
