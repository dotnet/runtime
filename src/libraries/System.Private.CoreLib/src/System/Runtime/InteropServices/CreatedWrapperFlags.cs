// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Enumeration of flags for <see cref="ComWrappers.CreateObject(IntPtr, CreateObjectFlags, object?, out CreatedWrapperFlags)"/>.
    /// </summary>
    [Flags]
    public enum CreatedWrapperFlags
    {
        None = 0,

        /// <summary>
        /// Indicate if the supplied external COM object implements the <see href="https://learn.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/nn-windows-ui-xaml-hosting-referencetracker-ireferencetracker">IReferenceTracker</see>.
        /// </summary>
        TrackerObject = 1,

        /// <summary>
        /// The managed object doesn't keep the native object alive. It represents an equivalent value.
        /// </summary>
        /// <remarks>
        /// Using this flag results in the following changes:
        /// <see cref="ComWrappers.TryGetComInstance" /> will return <c>false</c> for the returned object.
        /// The features provided by the <see cref="CreateObjectFlags.TrackerObject" /> flag will be disabled.
        /// Integration between <see cref="WeakReference" /> and the returned object via the native <c>IWeakReferenceSource</c> interface will not work.
        /// <see cref="CreateObjectFlags.UniqueInstance" /> behavior is implied.
        /// Diagnostics tooling support to unwrap objects returned by `CreateObject` will not see this object as a wrapper.
        /// The same object can be returned from `CreateObject` wrapping different COM objects.
        /// </remarks>
        NonWrapping = 0x2
    }
}
