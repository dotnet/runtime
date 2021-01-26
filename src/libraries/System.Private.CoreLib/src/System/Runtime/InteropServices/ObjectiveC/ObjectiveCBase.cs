// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// Base type for all types participating in Objective-C interop.
    /// </summary>
    [SupportedOSPlatform("macos")]
    public abstract class ObjectiveCBase : IDisposable
    {
        /// <summary>
        /// Create a <see cref="ObjectiveCBase"/> instance.
        /// </summary>
        protected ObjectiveCBase()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method called during Dispose().
        /// </summary>
        /// <param name="disposing">If called from <see cref="Dispose"/></param>
        protected virtual void Dispose(bool disposing)
        {
            throw new NotImplementedException();
        }
    }
}
