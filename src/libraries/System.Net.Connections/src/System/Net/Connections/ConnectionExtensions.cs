// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections
{
    /// <summary>
    /// Extension methods for working with the System.Net.Connections types.
    /// </summary>
    public static class ConnectionExtensions
    {
        /// <summary>
        /// Retrieves a Type-based property from an <see cref="IConnectionProperties"/>, if it exists.
        /// </summary>
        /// <typeparam name="T">The type of the property to retrieve.</typeparam>
        /// <param name="properties">The connection properties to retrieve a property from.</param>
        /// <param name="property">If <paramref name="properties"/> contains a property of type <typeparamref name="T"/>, receives the property. Otherwise, default.</param>
        /// <returns>If <paramref name="properties"/> contains a property of type <typeparamref name="T"/>, true. Otherwise, false.</returns>
        public static bool TryGet<T>(this IConnectionProperties properties, [MaybeNullWhen(false)] out T property)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            if (properties.TryGet(typeof(T), out object? obj) && obj is T propertyValue)
            {
                property = propertyValue;
                return true;
            }
            else
            {
                property = default;
                return false;
            }
        }

        /// <summary>
        /// Creates a connection-level filter on top of a <see cref="ConnectionFactory"/>.
        /// </summary>
        /// <param name="factory">The factory to be filtered.</param>
        /// <param name="filter">The connection-level filter to apply on top of <paramref name="factory"/>.</param>
        /// <returns>A new filtered <see cref="ConnectionFactory"/>.</returns>
        public static ConnectionFactory Filter(this ConnectionFactory factory, Func<Connection, IConnectionProperties?, CancellationToken, ValueTask<Connection>> filter)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            return new ConnectionFilteringFactory(factory, filter);
        }

        private sealed class ConnectionFilteringFactory : ConnectionFactory
        {
            private readonly ConnectionFactory _baseFactory;
            private readonly Func<Connection, IConnectionProperties?, CancellationToken, ValueTask<Connection>> _filter;

            public ConnectionFilteringFactory(ConnectionFactory baseFactory, Func<Connection, IConnectionProperties?, CancellationToken, ValueTask<Connection>> filter)
            {
                _baseFactory = baseFactory;
                _filter = filter;
            }

            public override async ValueTask<Connection> ConnectAsync(EndPoint? endPoint, IConnectionProperties? options = null, CancellationToken cancellationToken = default)
            {
                Connection con = await _baseFactory.ConnectAsync(endPoint, options, cancellationToken).ConfigureAwait(false);
                try
                {
                    return await _filter(con, options, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    await con.CloseAsync(ConnectionCloseMethod.Abort, cancellationToken).ConfigureAwait(false);
                    throw;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) _baseFactory.Dispose();
            }

            protected override ValueTask DisposeAsyncCore()
            {
                return _baseFactory.DisposeAsync();
            }
        }
    }
}
