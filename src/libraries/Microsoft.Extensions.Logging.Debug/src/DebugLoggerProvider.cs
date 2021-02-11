// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
