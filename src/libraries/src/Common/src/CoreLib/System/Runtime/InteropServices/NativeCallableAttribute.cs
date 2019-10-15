// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Any method marked with NativeCallableAttribute can be directly called from
    /// native code. The function token can be loaded to a local variable using LDFTN
    /// and passed as a callback to native method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NativeCallableAttribute : Attribute
    {
        public NativeCallableAttribute()
        {
        }

        /// <summary>
        /// Optional. If omitted, compiler will choose one for you.
        /// </summary>
        public CallingConvention CallingConvention;

        /// <summary>
        /// Optional. If omitted, then the method is native callable, but no EAT is emitted.
        /// </summary>
        public string? EntryPoint;
    }
}
