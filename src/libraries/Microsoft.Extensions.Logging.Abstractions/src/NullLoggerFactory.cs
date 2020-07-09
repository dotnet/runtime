// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// An <see cref="ILoggerFactory"/> used to create instance of
    /// <see cref="NullLogger"/> that logs nothing.
    /// </summary>
    public class NullLoggerFactory : ILoggerFactory
    {
        /// <summary>
        /// Creates a new <see cref="NullLoggerFactory"/> instance.
        /// </summary>
        public NullLoggerFactory() { }

        /// <summary>
        /// Returns the shared instance of <see cref="NullLoggerFactory"/>.
        /// </summary>
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

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
