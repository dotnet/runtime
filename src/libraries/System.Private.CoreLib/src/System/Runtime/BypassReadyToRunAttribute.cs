// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime
{
    // Use this attribute to indicate that a function should never be compiled into a Ready2Run binary
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
#if MONO
    [Conditional("unnecessary")] // Mono doesn't use Ready2Run so we can remove this attribute to reduce size
#endif
    internal sealed class BypassReadyToRunAttribute : Attribute
    {
    }
}
