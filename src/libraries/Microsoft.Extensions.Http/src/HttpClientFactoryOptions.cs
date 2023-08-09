// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http
{
    /// <summary>
    /// An options class for configuring the default <see cref="IHttpClientFactory"/>.
    /// </summary>
    public class HttpClientFactoryOptions
    {
        // Establishing a minimum lifetime helps us avoid some possible destructive cases.
        //
        // IMPORTANT: MinimumHandlerLifetime is used in a resource string. Update the resource if this changes.
        internal static readonly TimeSpan MinimumHandlerLifetime = TimeSpan.FromSeconds(1);

        private TimeSpan _handlerLifetime = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets a list of operations used to configure an <see cref="HttpMessageHandlerBuilder"/>.
        /// </summary>
        /// <remarks>
        /// Accessing this property will have a performance impact, as it will disable some optimizations.
        /// Consider using configuration APIs like ConfigurePrimaryHttpMessageHandler or
        /// ConfigureAdditionalHttpMessageHandlers instead of accessing this property directly.
        /// </remarks>
        public IList<Action<HttpMessageHandlerBuilder>> HttpMessageHandlerBuilderActions => _httpMessageHandlerBuilderActions ??= MergeToHandlerBuilderActions();

        /// <summary>
        /// Gets a list of operations used to configure an <see cref="HttpClient"/>.
        /// </summary>
        public IList<Action<HttpClient>> HttpClientActions { get; } = new List<Action<HttpClient>>();

        /// <summary>
        /// Gets or sets the length of time that a <see cref="HttpMessageHandler"/> instance can be reused. Each named
        /// client can have its own configured handler lifetime value. The default value of this property is two minutes.
        /// Set the lifetime to <see cref="Timeout.InfiniteTimeSpan"/> to disable handler expiry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The default implementation of <see cref="IHttpClientFactory"/> will pool the <see cref="HttpMessageHandler"/>
        /// instances created by the factory to reduce resource consumption. This setting configures the amount of time
        /// a handler can be pooled before it is scheduled for removal from the pool and disposal.
        /// </para>
        /// <para>
        /// Pooling of handlers is desirable as each handler typically manages its own underlying HTTP connections; creating
        /// more handlers than necessary can result in connection delays. Some handlers also keep connections open indefinitely
        /// which can prevent the handler from reacting to DNS changes. The value of <see cref="HandlerLifetime"/> should be
        /// chosen with an understanding of the application's requirement to respond to changes in the network environment.
        /// </para>
        /// <para>
        /// Expiry of a handler will not immediately dispose the handler. An expired handler is placed in a separate pool
        /// which is processed at intervals to dispose handlers only when they become unreachable. Using long-lived
        /// <see cref="HttpClient"/> instances will prevent the underlying <see cref="HttpMessageHandler"/> from being
        /// disposed until all references are garbage-collected.
        /// </para>
        /// </remarks>
        public TimeSpan HandlerLifetime
        {
            get => _handlerLifetime;
            set
            {
                if (value != Timeout.InfiniteTimeSpan && value < MinimumHandlerLifetime)
                {
                    throw new ArgumentException(SR.HandlerLifetime_InvalidValue, nameof(value));
                }

                _handlerLifetime = value;
            }
        }

        /// <summary>
        /// The <see cref="Func{T, R}"/> which determines whether to redact the HTTP header value before logging.
        /// </summary>
        public Func<string, bool> ShouldRedactHeaderValue { get; set; } = (header) => false;

        /// <summary>
        /// <para>
        /// Gets or sets a value that determines whether the <see cref="IHttpClientFactory"/> will
        /// create a dependency injection scope when building an <see cref="HttpMessageHandler"/>.
        /// If <c>false</c> (default), a scope will be created, otherwise a scope will not be created.
        /// </para>
        /// <para>
        /// This option is provided for compatibility with existing applications. It is recommended
        /// to use the default setting for new applications.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="IHttpClientFactory"/> will (by default) create a dependency injection scope
        /// each time it creates an <see cref="HttpMessageHandler"/>. The created scope has the same
        /// lifetime as the message handler, and will be disposed when the message handler is disposed.
        /// </para>
        /// <para>
        /// When operations that are part of <see cref="HttpMessageHandlerBuilderActions"/> are executed
        /// they will be provided with the scoped <see cref="IServiceProvider"/> via
        /// <see cref="HttpMessageHandlerBuilder.Services"/>. This includes retrieving a message handler
        /// from dependency injection, such as one registered using
        /// <see cref="HttpClientBuilderExtensions.AddHttpMessageHandler{THandler}(IHttpClientBuilder)"/>.
        /// </para>
        /// </remarks>
        public bool SuppressHandlerScope { get; set; }

        internal bool SuppressDefaultLogging { get; set; }
        internal List<Action<HttpMessageHandlerBuilder>> LoggingBuilderActions { get; } = new List<Action<HttpMessageHandlerBuilder>>();

        private List<Action<HttpMessageHandlerBuilder>>? _httpMessageHandlerBuilderActions;
        private List<Action<IPrimaryHandlerBuilder>>? _primaryHandlerActions = new List<Action<IPrimaryHandlerBuilder>>();
        private List<Action<IAdditionalHandlersBuilder>>? _additionalHandlersActions = new List<Action<IAdditionalHandlersBuilder>>();

        internal IReadOnlyList<Action<IPrimaryHandlerBuilder>> PrimaryHandlerActions => _primaryHandlerActions!;
        internal IReadOnlyList<Action<IAdditionalHandlersBuilder>> AdditionalHandlersActions => _additionalHandlersActions!;
        internal bool MergedToHandlerBuilderActions => _primaryHandlerActions is null;

        // fallback for backwards compatibility
        private List<Action<HttpMessageHandlerBuilder>> MergeToHandlerBuilderActions()
        {
            Debug.Assert(_httpMessageHandlerBuilderActions is null);
            Debug.Assert(_primaryHandlerActions is not null);
            Debug.Assert(_additionalHandlersActions is not null);

            List<Action<HttpMessageHandlerBuilder>> handlerBuilderActions = new List<Action<HttpMessageHandlerBuilder>>();
            handlerBuilderActions.AddRange(_primaryHandlerActions);
            handlerBuilderActions.AddRange(_additionalHandlersActions);

            _primaryHandlerActions = null;
            _additionalHandlersActions = null;

            return handlerBuilderActions;
        }

        internal void AddPrimaryHandlerAction(Action<IPrimaryHandlerBuilder> action, bool updatesExistingHandler)
        {
            if (!MergedToHandlerBuilderActions)
            {
                if (!updatesExistingHandler)
                {
                    _primaryHandlerActions!.Clear();
                }
                _primaryHandlerActions!.Add(action);
            }
            else
            {
                _httpMessageHandlerBuilderActions!.Add(action);
            }
        }

        internal void AddAdditionalHandlersAction(Action<IAdditionalHandlersBuilder> action)
        {
            if (!MergedToHandlerBuilderActions)
            {
                _additionalHandlersActions!.Add(action);
            }
            else
            {
                _httpMessageHandlerBuilderActions!.Add(action);
            }
        }
    }
}
