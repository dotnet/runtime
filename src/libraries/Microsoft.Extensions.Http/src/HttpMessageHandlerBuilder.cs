// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;

namespace Microsoft.Extensions.Http
{
    /// <summary>
    /// A builder abstraction for configuring <see cref="HttpMessageHandler"/> instances.
    /// </summary>
    /// <remarks>
    /// The <see cref="HttpMessageHandlerBuilder"/> is registered in the service collection as
    /// a transient service. Callers should retrieve a new instance for each <see cref="HttpMessageHandler"/> to
    /// be created. Implementors should expect each instance to be used a single time.
    /// </remarks>
    public abstract class HttpMessageHandlerBuilder
    {
        /// <summary>
        /// Gets or sets the name of the <see cref="HttpClient"/> being created.
        /// </summary>
        /// <remarks>
        /// The <see cref="Name"/> is set by the <see cref="IHttpClientFactory"/> infrastructure
        /// and is public for unit testing purposes only. Setting the <see cref="Name"/> outside of
        /// testing scenarios may have unpredictable results.
        /// </remarks>
        [DisallowNull]
        public abstract string? Name { get; set; }

        /// <summary>
        /// Gets or sets the primary <see cref="HttpMessageHandler"/>.
        /// </summary>
        public abstract HttpMessageHandler PrimaryHandler { get; set; }

        /// <summary>
        /// Gets a list of additional <see cref="DelegatingHandler"/> instances used to configure an
        /// <see cref="HttpClient"/> pipeline.
        /// </summary>
        public abstract IList<DelegatingHandler> AdditionalHandlers { get; }

        /// <summary>
        /// Gets an <see cref="IServiceProvider"/> which can be used to resolve services
        /// from the dependency injection container.
        /// </summary>
        /// <remarks>
        /// This property is sensitive to the value of
        /// <see cref="HttpClientFactoryOptions.SuppressHandlerScope"/>. If <c>true</c> this
        /// property will be a reference to the application's root service provider. If <c>false</c>
        /// (default) this will be a reference to a scoped service provider that has the same
        /// lifetime as the handler being created.
        /// </remarks>
        public virtual IServiceProvider Services { get; } = null!;

        /// <summary>
        /// Creates an <see cref="HttpMessageHandler"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="HttpMessageHandler"/> built from the <see cref="PrimaryHandler"/> and
        /// <see cref="AdditionalHandlers"/>.
        /// </returns>
        public abstract HttpMessageHandler Build();

        /// <summary>
        /// Constructs an instance of <see cref="HttpMessageHandler"/> by chaining <paramref name="additionalHandlers"/> one after another with <paramref name="primaryHandler"/> in the
        /// end of the chain. The resulting pipeline is used by <see cref="IHttpClientFactory"/> infrastructure to create <see cref="HttpClient"/> instances with customized message
        /// handlers. The resulting pipeline can also be accessed by using <see cref="IHttpMessageHandlerFactory"/> instead of <see cref="IHttpClientFactory"/>.
        /// </summary>
        /// <param name="primaryHandler">An instance of <see cref="HttpMessageHandler"/> to operate at the bottom of the handler chain and actually handle the HTTP transport operations.</param>
        /// <param name="additionalHandlers">An ordered list of <see cref="DelegatingHandler"/> instances to be invoked as part
        /// of sending an <see cref="HttpRequestMessage"/> and receiving an <see cref="HttpResponseMessage"/>.
        /// The handlers are invoked in a top-down fashion. That is, the first entry is invoked first for
        /// an outbound request message but last for an inbound response message.</param>
        /// <returns>The HTTP message handler chain.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="primaryHandler "/> or <paramref name="additionalHandlers "/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="additionalHandlers "/> contains a <see langword="null"/> entry.
        /// -or-
        /// The <c>DelegatingHandler.InnerHandler</c> property must be <see langword="null"/>. <c>DelegatingHandler</c> instances provided to <c>HttpMessageHandlerBuilder</c> must not be reused or cached.</exception>
        protected internal static HttpMessageHandler CreateHandlerPipeline(HttpMessageHandler primaryHandler, IEnumerable<DelegatingHandler> additionalHandlers)
        {
            ThrowHelper.ThrowIfNull(primaryHandler);
            ThrowHelper.ThrowIfNull(additionalHandlers);

            // This is similar to https://github.com/aspnet/AspNetWebStack/blob/master/src/System.Net.Http.Formatting/HttpClientFactory.cs#L58
            // but we don't want to take that package as a dependency.

            IReadOnlyList<DelegatingHandler> additionalHandlersList = additionalHandlers as IReadOnlyList<DelegatingHandler> ?? additionalHandlers.ToArray();

            HttpMessageHandler next = primaryHandler;
            for (int i = additionalHandlersList.Count - 1; i >= 0; i--)
            {
                DelegatingHandler handler = additionalHandlersList[i];
                if (handler == null)
                {
                    string message = SR.Format(SR.HttpMessageHandlerBuilder_AdditionalHandlerIsNull, nameof(additionalHandlers));
                    throw new InvalidOperationException(message);
                }

                // Checking for this allows us to catch cases where someone has tried to re-use a handler. That really won't
                // work the way you want and it can be tricky for callers to figure out.
                if (handler.InnerHandler != null)
                {
                    string message = SR.Format(SR.HttpMessageHandlerBuilder_AdditionHandlerIsInvalid,
                        nameof(DelegatingHandler.InnerHandler),
                        nameof(DelegatingHandler),
                        nameof(HttpMessageHandlerBuilder),
                        Environment.NewLine,
                        handler);
                    throw new InvalidOperationException(message);
                }

                handler.InnerHandler = next;
                next = handler;
            }

            return next;
        }
    }
}
