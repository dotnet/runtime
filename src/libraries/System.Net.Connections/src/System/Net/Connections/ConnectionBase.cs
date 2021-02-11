// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    /// <summary>
    /// Provides base functionality shared between singular (e.g. TCP) and multiplexed (e.g. QUIC) connections.
    /// </summary>
    public abstract class ConnectionBase : IDisposable, IAsyncDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Properties exposed by this connection.
        /// </summary>
        public abstract IConnectionProperties ConnectionProperties { get; }

        /// <summary>
        /// The local endpoint of this connection, if any.
        /// </summary>
        public abstract EndPoint? LocalEndPoint { get; }

        /// <summary>
        /// The remote endpoint of this connection, if any.
        /// </summary>
        public abstract EndPoint? RemoteEndPoint { get; }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="method">The method to use when closing the connection.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>A <see cref="ValueTask"/> for the asynchronous operation.</returns>
        public async ValueTask CloseAsync(ConnectionCloseMethod method = ConnectionCloseMethod.GracefulShutdown, CancellationToken cancellationToken = default)
        {
            if (!_disposed)
            {
                await CloseAsyncCore(method, cancellationToken).ConfigureAwait(false);
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="method">The method to use when closing the connection.</param>
        /// <param name="cancellationToken">A cancellation token for the asynchronous operation.</param>
        /// <returns>A <see cref="ValueTask"/> for the asynchronous operation.</returns>
        protected abstract ValueTask CloseAsyncCore(ConnectionCloseMethod method, CancellationToken cancellationToken);

        /// <summary>
        /// Disposes of the connection.
        /// </summary>
        /// <remarks>
        /// This is equivalent to calling <see cref="CloseAsync(ConnectionCloseMethod, CancellationToken)"/> with the method <see cref="ConnectionCloseMethod.GracefulShutdown"/>, and calling GetAwaiter().GetResult() on the resulting task.
        /// To increase likelihood of synchronous completion, call <see cref="CloseAsync(ConnectionCloseMethod, CancellationToken)"/> directly with the method <see cref="ConnectionCloseMethod.Immediate"/>.
        /// </remarks>
        public void Dispose()
        {
            ValueTask t = CloseAsync(ConnectionCloseMethod.GracefulShutdown, CancellationToken.None);

            if (t.IsCompleted) t.GetAwaiter().GetResult();
            else t.AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Disposes of the connection.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> for the asynchronous operation.</returns>
        /// <remarks>This is equivalent to calling <see cref="CloseAsync(ConnectionCloseMethod, CancellationToken)"/> with the method <see cref="ConnectionCloseMethod.GracefulShutdown"/>.</remarks>
        public ValueTask DisposeAsync()
        {
            return CloseAsync(ConnectionCloseMethod.GracefulShutdown, CancellationToken.None);
        }
    }
}
