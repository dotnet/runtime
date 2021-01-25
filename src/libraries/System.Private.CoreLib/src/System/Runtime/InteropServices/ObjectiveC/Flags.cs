// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    [SupportedOSPlatform("macos")]
    [Flags]
    public enum RegisterInstanceFlags
    {
        None = 0,

        /// <summary>
        /// The type to register was defined in managed code.
        /// </summary>
        /// <remarks>
        /// Objective-C types defined are in managed code are allocated
        /// with additional bytes to carry a GC Handle used for life time
        /// management. This data structure can be used by the consuming
        /// Objective-C interop implementation. The data structure is an instance
        /// of <see cref="ManagedObjectWrapperLifetime"/>. It can be accessed
        /// on the allocation by through the object_getIndexedIvars Objective-C runtime API.
        /// </remarks>
        ManagedDefinition = 1,
    }

    [SupportedOSPlatform("macos")]
    [Flags]
    public enum CreateObjectFlags
    {
        None = 0,

        /// <summary>
        /// The supplied Objective-C instance should be check if it is a
        /// wrapped managed object and not a pure Objective-C instance.
        ///
        /// If the instance is wrapped return the underlying managed object
        /// instead of creating a new wrapper.
        /// </summary>
        Unwrap = 1,

        /// <summary>
        /// Let the .NET runtime participate in lifetime management.
        /// </summary>
        /// <remarks>
        /// Using this optional is always possible but required if the
        /// created object will contain managed state that must be kept
        /// alive even without a managed reference.
        /// </remarks>
        ManageLifetime = 2,
    }

    [SupportedOSPlatform("macos")]
    [Flags]
    public enum CreateBlockFlags
    {
        None = 0,
    }

    [SupportedOSPlatform("macos")]
    [Flags]
    public enum CreateDelegateFlags
    {
        None = 0,

        /// <summary>
        /// The supplied Objective-C block should be check if it is a
        /// wrapped Delegate and not a pure Objective-C Block.
        ///
        /// If the instance is wrapped return the underlying Delegate
        /// instead of creating a new wrapper.
        /// </summary>
        Unwrap = 1,
    }
}
