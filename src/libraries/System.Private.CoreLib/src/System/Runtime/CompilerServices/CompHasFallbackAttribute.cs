// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.CompilerServices
{
    // Use this attribute alongside CompExactlyDependsOnAttribute to indicate that a method has
    // a functional fallback path and should still be compiled into the Ready2Run image even when
    // none of the CompExactlyDependsOn instruction sets are supported.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
#if MONO
    [Conditional("unnecessary")] // Mono doesn't use Ready2Run so we can remove this attribute to reduce size
#endif
    internal sealed class CompHasFallbackAttribute : Attribute
    {
    }
}
