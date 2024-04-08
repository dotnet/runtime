// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.ComponentModel;

#if MICROSOFT_INTEROP_COMINTERFACEGENERATOR
namespace Microsoft.Interop
#else
namespace System.Runtime.InteropServices.Marshalling
#endif
{
    /// <summary>
    /// Options for how to generate COM interface interop with the COM interop source generator.
    /// </summary>
    [Flags]
    public enum ComInterfaceOptions
    {
        /// <summary>
        /// No options specified.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        None = 0,
        /// <summary>
        /// Generate a wrapper for managed objects to enable exposing them through the COM interface.
        /// </summary>
        ManagedObjectWrapper = 0x1,
        /// <summary>
        /// Generate a wrapper for COM objects to enable exposing them through the managed interface.
        /// </summary>
        ComObjectWrapper = 0x2,
    }
}
