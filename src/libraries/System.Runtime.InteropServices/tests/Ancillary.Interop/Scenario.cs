﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if MICROSOFT_INTEROP_SOURCEGENERATION
namespace Microsoft.Interop
#else
namespace System.Runtime.InteropServices
#endif
{
    /// <summary>
    /// An enumeration representing the different marshalling scenarios in our marshalling model.
    /// </summary>
#if LIBRARYIMPORT_GENERATOR_TEST || MICROSOFT_INTEROP_SOURCEGENERATION
    public
#else
    internal
#endif
    enum Scenario
    {
        /// <summary>
        /// All scenarios. A marshaller specified with this scenario will be used if there is not a specific
        /// marshaller specified for a given usage scenario.
        /// </summary>
        Default,
        /// <summary>
        /// By-value and <c>in</c> parameters in managed-to-unmanaged scenarios, like P/Invoke.
        /// </summary>
        ManagedToUnmanagedIn,
        /// <summary>
        /// <c>ref</c> parameters in managed-to-unmanaged scenarios, like P/Invoke.
        /// </summary>
        ManagedToUnmanagedRef,
        /// <summary>
        /// <c>out</c> parameters in managed-to-unmanaged scenarios, like P/Invoke.
        /// </summary>
        ManagedToUnmanagedOut,
        /// <summary>
        /// By-value and <c>in</c> parameters in unmanaged-to-managed scenarios, like Reverse P/Invoke.
        /// </summary>
        UnmanagedToManagedIn,
        /// <summary>
        /// <c>ref</c> parameters in unmanaged-to-managed scenarios, like Reverse P/Invoke.
        /// </summary>
        UnmanagedToManagedRef,
        /// <summary>
        /// <c>out</c> parameters in unmanaged-to-managed scenarios, like Reverse P/Invoke.
        /// </summary>
        UnmanagedToManagedOut,
        /// <summary>
        /// Elements of arrays passed with <c>in</c> or by-value in interop scenarios.
        /// </summary>
        ElementIn,
        /// <summary>
        /// Elements of arrays passed with <c>ref</c> or passed by-value with both <see cref="InAttribute"/> and <see cref="OutAttribute" /> in interop scenarios.
        /// </summary>
        ElementRef,
        /// <summary>
        /// Elements of arrays passed with <c>out</c> or passed by-value with only <see cref="OutAttribute" /> in interop scenarios.
        /// </summary>
        ElementOut
    }
}
