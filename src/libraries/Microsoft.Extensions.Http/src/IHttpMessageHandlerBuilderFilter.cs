// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Http
{
    /// <summary>
    /// Used by the <see cref="DefaultHttpClientFactory"/> to apply additional initialization to the configure the
    /// <see cref="HttpMessageHandlerBuilder"/> immediately before <see cref="HttpMessageHandlerBuilder.Build()"/>
    /// is called.
    /// </summary>
    public interface IHttpMessageHandlerBuilderFilter
    {
        /// <summary>
        /// Applies additional initialization to the <see cref="HttpMessageHandlerBuilder"/>
        /// </summary>
        /// <param name="next">A delegate which will run the next <see cref="IHttpMessageHandlerBuilderFilter"/>.</param>
        /// <returns>The configured <see cref="HttpMessageHandlerBuilder"/>.</returns>
        Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next);
    }
}
