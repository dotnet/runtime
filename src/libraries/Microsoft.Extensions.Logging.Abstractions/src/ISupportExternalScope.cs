// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
