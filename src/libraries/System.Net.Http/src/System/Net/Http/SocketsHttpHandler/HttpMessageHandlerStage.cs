using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    internal abstract class HttpMessageHandlerStage : HttpMessageHandler
    {
        protected internal sealed override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ValueTask<HttpResponseMessage> sendTask = SendAsync(request, async:false, cancellationToken);
            Debug.Assert(sendTask.IsCompleted);
            return sendTask.GetAwaiter().GetResult();
        }

        protected internal sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            SendAsync(request, async: true, cancellationToken).AsTask();

        internal abstract ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken);
    }
}
