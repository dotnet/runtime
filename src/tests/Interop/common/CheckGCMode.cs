// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

using TestLibrary;

unsafe internal class CheckGCMode
{
    internal static bool Enabled = false;
    internal static void Initialize(delegate* <delegate* unmanaged<int>, void> setIsInCooperativeModeFunction)
    {
        // GetIsInCooperativeGCModeFunctionPointer is conditionally included based on the runtime build configuration,
        // so we check for its existence and only do the explicit mode validation if it is available.
        Type marshalType = typeof(object).Assembly.GetType(typeof(System.Runtime.InteropServices.Marshal).FullName);
        MethodInfo getFunctionPtr = marshalType.GetMethod("GetIsInCooperativeGCModeFunctionPointer", BindingFlags.NonPublic | BindingFlags.Static);
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

        Assert.AreEqual(transitionSuppressed, inCooperativeMode, $"GC transition should{(transitionSuppressed ? "" : " not")} have been suppressed");
    }

    internal static void Validate(bool transitionSuppressed, int inCooperativeMode)
        => Validate(transitionSuppressed, inCooperativeMode != 0);
}
