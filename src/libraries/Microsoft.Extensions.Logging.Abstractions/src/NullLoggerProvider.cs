// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// Provider for the <see cref="NullLogger"/>.
    /// </summary>
    public class NullLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Returns an instance of <see cref="NullLoggerProvider"/>.
        /// </summary>
        public static NullLoggerProvider Instance { get; } = new NullLoggerProvider();

        private NullLoggerProvider()
        {
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return NullLogger.Instance;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
