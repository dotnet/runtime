// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// An empty scope with no logic.
    /// </summary>
    public sealed class NullScope : IDisposable
    {
        /// <summary>
        /// Provides a Null Scope Singleton.
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
