// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

using Xunit;

unsafe internal class CheckGCMode
{
    internal static bool Enabled = false;
    internal static void Initialize(delegate* <delegate* unmanaged<int>, void> setIsInCooperativeModeFunction)
    {
        // GetIsInCooperativeGCModeFunctionPointer is CoreCLR-specific, so only validate runtimes that provide it.
        MethodInfo getFunctionPtr = typeof(Marshal).GetMethod("GetIsInCooperativeGCModeFunctionPointer", BindingFlags.NonPublic | BindingFlags.Static);
        if (getFunctionPtr != null)
        {
            var isInCooperativeModeFunc = (delegate* unmanaged<int>)(IntPtr)getFunctionPtr.Invoke(null, null);
            setIsInCooperativeModeFunction(isInCooperativeModeFunc);
            Enabled = true;
            Console.WriteLine("Explicit GC mode check is enabled");
        }
    }

    internal static void Validate(bool transitionSuppressed, bool inCooperativeMode)
    {
        if (!Enabled)
            return;

        Assert.Equal(transitionSuppressed, inCooperativeMode);
    }

    internal static void Validate(bool transitionSuppressed, int inCooperativeMode)
        => Validate(transitionSuppressed, inCooperativeMode != 0);
}
