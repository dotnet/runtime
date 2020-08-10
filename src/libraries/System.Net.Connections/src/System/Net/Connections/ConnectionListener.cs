// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    /// <summary>
    /// A listener to accept incoming connections.
    /// </summary>
    public abstract class ConnectionListener : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Properties exposed by this listener.
        /// </summary>
        public abstract IConnectionProperties ListenerProperties { get; }

        /// <summary>
        /// The local endpoint of this connection, if any.
        /// </summary>
        public abstract EndPoint? LocalEndPoint { get; }

        /// <summary>
        /// Accepts an incoming connection.
        /// </summary>
        /// <param name="options">Options used to create the connection, if any.</param>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> for the <see cref="Connection"/>.</returns>
        public abstract ValueTask<Connection> AcceptAsync(IConnectionProperties? options = null, CancellationToken cancellationToken = default);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the <see cref="ConnectionFactory"/>.
        /// </summary>
        /// <param name="disposing">If true, the <see cref="ConnectionFactory"/> is being disposed. If false, the <see cref="ConnectionFactory"/> is being finalized.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Asynchronously disposes the <see cref="ConnectionFactory"/>.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
        protected virtual ValueTask DisposeAsyncCore()
        {
            Dispose(true);
            return default;
        }
    }
}
