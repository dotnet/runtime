// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    public class CallConvCdecl
    {
        public CallConvCdecl() { }
    }
    public class CallConvFastcall
    {
        public CallConvFastcall() { }
    }
    public class CallConvStdcall
    {
        public CallConvStdcall() { }
    }

    /// <summary>
    /// Indicates that a method should suppress the GC transition as part of the calling convention.
    /// </summary>
    /// <remarks>
    /// The <see cref="System.Runtime.InteropServices.SuppressGCTransitionAttribute" /> describes the effects
    /// of suppressing the GC transition on a native call.
    /// </remarks>
    public class CallConvSuppressGCTransition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallConvSuppressGCTransition" /> class.
        /// </summary>
        public CallConvSuppressGCTransition() { }
    }
    public class CallConvThiscall
    {
        public CallConvThiscall() { }
    }
}
