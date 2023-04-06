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
#if TESTING_UNITY_CORECLR
    private static readonly ConcurrentBag<GCHandle> handles = new ();
#endif

    public static object ToManagedRepresentation(this nint intPtr)
        => Unsafe.As<nint, object>(ref intPtr);

    public static nint ToNativeRepresentation(this object obj)
    {
#if TESTING_UNITY_CORECLR
        WorkaroundToGetGCSafety(obj);
#endif
        return Unsafe.As<object, nint>(ref obj);
    }

#if TESTING_UNITY_CORECLR
    static void WorkaroundToGetGCSafety(object obj)
    {
        var valueToPin = 1;
        var handle = GCHandle.Alloc(valueToPin, GCHandleType.Pinned);

        // Now do the same thing as
        // https://github.com/Unity-Technologies/runtime/blob/unity-main/src/libraries/System[…].Private.CoreLib/src/System/Runtime/InteropServices/GCHandle.cs
        // Except by pass the pinnable checks so that we can use the GCHandle to hang onto our object
        var getHandleValueMethod =
            typeof(GCHandle).GetMethod("GetHandleValue", BindingFlags.Static | BindingFlags.NonPublic);
        var internalSetMethod =
            typeof(GCHandle).GetMethod("InternalSet", BindingFlags.Static | BindingFlags.NonPublic);

        var handleValue = getHandleValueMethod.Invoke(handle, new[] {(object)GCHandle.ToIntPtr(handle)});
        internalSetMethod.Invoke(handle, new []
        {
            handleValue,
            obj
        });

        handles.Add(handle);
    }
#endif
}
