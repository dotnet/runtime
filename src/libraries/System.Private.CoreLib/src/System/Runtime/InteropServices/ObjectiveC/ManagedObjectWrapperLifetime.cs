// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// Data structure for managing object wrapper lifetime.
    /// </summary>
    [SupportedOSPlatform("macos")]
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct ManagedObjectWrapperLifetime
    {
        /// <summary>
        /// Allocated space for Objective-C interop implementation
        /// to use as needed. Will be initialized to nuint.MaxValue.
        /// </summary>
        public nuint Scratch;

        // Internal fields
        internal IntPtr GCHandle;
    }
}
