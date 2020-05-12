using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security.Tests
{
    internal static class SslStreamExtensions
    {
        public static Task AuthenticateAsClientAsync(this SslStream stream, SslClientAuthenticationOptions clientOptions,
            bool async, CancellationToken cancellationToken = default)
        {
            return async
                ? stream.AuthenticateAsClientAsync(clientOptions, cancellationToken)
                : Task.Run(() => stream.AuthenticateAsClient(clientOptions));
        }
        public static Task AuthenticateAsServerAsync(this SslStream stream, SslServerAuthenticationOptions serverOptions,
            bool async, CancellationToken cancellationToken = default)
        {
            return async
                ? stream.AuthenticateAsServerAsync(serverOptions, cancellationToken)
                : Task.Run(() => stream.AuthenticateAsServer(serverOptions));
        }
    }
}
