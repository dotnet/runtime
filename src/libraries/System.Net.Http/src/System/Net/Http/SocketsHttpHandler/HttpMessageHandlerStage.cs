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
            ValueTask<HttpResponseMessage> sendTask = SendAsync(request, false, cancellationToken);
            // At least for HTTP 1.1 we must support fully sync Send
            Debug.Assert(request.Version.Major >= 2 || sendTask.IsCompleted);
            return sendTask.IsCompleted ? sendTask.GetAwaiter().GetResult() : sendTask.AsTask().GetAwaiter().GetResult();
        }

        protected internal sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            SendAsync(request, true, cancellationToken).AsTask();

        internal abstract ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async,
            CancellationToken cancellationToken);
    }
}
