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
        /// <remarks>
        /// <para>
        /// When this flag is the only one specified on a given COM interface, no implementation methods will be generated
        /// to support <see cref="System.Runtime.InteropServices.IDynamicInterfaceCastable"/> casts. In such a scenario,
        /// attempting to cast to that interface type will still succeed, but any calls to interface methods will fail.
        /// </para>
        /// <para>
        /// Using only this flag is purely a binary size optimization. If calling methods via this interface on native
        /// object is required, the <see cref="ComObjectWrapper"/> flag should also be used instead.
        /// </para>
        /// </remarks>
        ManagedObjectWrapper = 0x1,
        /// <summary>
        /// Generate a wrapper for COM objects to enable exposing them through the managed interface.
        /// </summary>
        ComObjectWrapper = 0x2,
    }
}
