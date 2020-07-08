using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security.Tests
{
    internal static class SslStreamExtensions
    {
        public static Task AuthenticateAsClientAsync(this SslStream stream,
            bool async, SslClientAuthenticationOptions clientOptions,
            CancellationToken cancellationToken = default)
        {
            return async
                ? stream.AuthenticateAsClientAsync(clientOptions, cancellationToken)
                : Task.Run(() => stream.AuthenticateAsClient(clientOptions));
        }
        public static Task AuthenticateAsServerAsync(this SslStream stream,
            bool async, SslServerAuthenticationOptions serverOptions,
            CancellationToken cancellationToken = default)
        {
            return async
                ? stream.AuthenticateAsServerAsync(serverOptions, cancellationToken)
                : Task.Run(() => stream.AuthenticateAsServer(serverOptions));
        }
    }
}
