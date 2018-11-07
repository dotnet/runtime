// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// Provider for the <see cref="NullLogger"/>.
    /// </summary>
    public class NullLoggerProvider : ILoggerProvider
    {
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
