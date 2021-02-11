// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Represents a <see cref="ILoggerProvider"/> that is able to consume external scope information.
    /// </summary>
    public interface ISupportExternalScope
    {
        /// <summary>
        /// Sets external scope information source for logger provider.
        /// </summary>
        /// <param name="scopeProvider">The provider of scope data.</param>
        void SetScopeProvider(IExternalScopeProvider scopeProvider);
    }
}
