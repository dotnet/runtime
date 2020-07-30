using System.Threading;
using System.Threading.Tasks;
using System.Net.Connections;
using System.Net.Sockets;

namespace System.Net.Http
{
    public class SocketsHttpConnectionFactory : ConnectionFactory
    {
        public sealed override ValueTask<Connection> ConnectAsync(EndPoint? endPoint, IConnectionProperties? options = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public virtual Socket CreateSocket(HttpRequestMessage message, EndPoint? endPoint, IConnectionProperties options)
            => throw new NotImplementedException();

        public virtual ValueTask<Connection> EstablishConnectionAsync(HttpRequestMessage message, EndPoint? endPoint, IConnectionProperties options, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
