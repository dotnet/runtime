// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// An <see cref="ILoggerFactory"/> used to create instance of
    /// <see cref="NullLogger"/> that logs nothing.
    /// </summary>
    public class NullLoggerFactory : ILoggerFactory
    {
        public static readonly NullLoggerFactory Instance = new NullLoggerFactory();

        /// <inheritdoc />
        /// <remarks>
        /// This returns a <see cref="NullLogger"/> instance which logs nothing.
        /// </remarks>
        public ILogger CreateLogger(string name)
        {
            return NullLogger.Instance;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method ignores the parameter and does nothing.
        /// </remarks>
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}