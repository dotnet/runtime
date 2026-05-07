// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace Microsoft.Extensions.Logging.Abstractions.Internal
{
    /// <summary>
    /// An empty scope without any logic.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is retained only for compatibility.", error: true)]
    public class NullScope : IDisposable
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
