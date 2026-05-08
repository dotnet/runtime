// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Enumeration of flags for <see cref="ComWrappers.GetOrCreateObjectForComInstance(IntPtr, CreateObjectFlags)"/>.
    /// </summary>
    [Flags]
    public enum CreateObjectFlags
    {
        None = 0,

        /// <summary>
        /// Indicate if the supplied external COM object implements the <see href="https://learn.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/nn-windows-ui-xaml-hosting-referencetracker-ireferencetracker">IReferenceTracker</see>.
        /// </summary>
        TrackerObject = 1,

        /// <summary>
        /// Ignore any internal caching and always create a unique instance.
        /// </summary>
        UniqueInstance = 2,

        /// <summary>
        /// Defined when COM aggregation is involved (that is an inner instance supplied).
        /// </summary>
        Aggregation = 4,

        /// <summary>
        /// Check if the supplied instance is actually a wrapper and if so return the underlying
        /// managed object rather than creating a new wrapper.
        /// </summary>
        /// <remarks>
        /// This matches the built-in RCW semantics for COM interop.
        /// </remarks>
        Unwrap = 8,
    }
}
