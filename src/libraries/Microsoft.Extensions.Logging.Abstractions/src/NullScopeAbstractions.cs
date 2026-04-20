// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging.Abstractions
{
    /// <summary>
    /// An empty scope without any logic.
    /// </summary>
    public sealed class NullScope : IDisposable
    {
        /// <summary>
        /// Returns the shared instance of <see cref="NullScope"/>.
        /// </summary>
        public static NullScope Instance { get; } = new NullScope();

        private NullScope()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
