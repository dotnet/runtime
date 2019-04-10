// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Debug
{
    /// <summary>
    /// The provider for the <see cref="DebugLogger"/>.
    /// </summary>
    [ProviderAlias("Debug")]
    public class DebugLoggerProvider : ILoggerProvider
    {
        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            return new DebugLogger(name);
        }

        public void Dispose()
        {
        }
    }
}
