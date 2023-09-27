// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unity.CoreCLRHelpers;

/// <summary>
/// These extensions are specifically for transitioning to/from our current native representation
/// These are not GC safe and will change in the future
/// </summary>
static class UnsafeExtensions
{
    private static readonly bool s_ReturnHandlesFromAPI = CoreCLRHost.UseRealGC() || CoreCLRHost.ReturnHandlesFromAPI();
    public static object ToManagedRepresentation(this nint intPtr)
    {
        var value = intPtr;
        if (IsHandle(value))
        {
            var handle = (nuint)value & ~kBitMask;
            return ((GCHandle)(nint)handle).Target;
        }
        // legacy pointer code path until we are fully GC safe
        return Unsafe.As<nint, object>(ref intPtr);
    }

    public static nint ToNativeRepresentation(this object obj)
    {
        if (s_ReturnHandlesFromAPI)
        {
            var handle = (nuint)(nint)(IntPtr)GCHandle.Alloc(obj);
            handle |= kHandleMask;

            return (nint)handle;
        }

        return Unsafe.As<object, nint>(ref obj);
    }

    static bool IsHandle(IntPtr value) { return   (((nuint)value) & kHandleMask) == kHandleMask; }

    const nuint kBitMask = 0b11;
    const nuint kHandleMask = 0b10; // the low bit may or may not be set depending on the runtime
}
