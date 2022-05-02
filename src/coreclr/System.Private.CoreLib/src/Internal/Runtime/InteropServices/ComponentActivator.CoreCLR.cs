// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Internal.Runtime.InteropServices
{
    public static partial class ComponentActivator
    {
        // This hook for when GetFunctionPointer is called when the feature is disabled allows us to
        // provide error messages for known hosting scenarios such as C++/CLI.
        private static void OnDisabledGetFunctionPointerCall(IntPtr typeNameNative, IntPtr methodNameNative)
        {
            if (!OperatingSystem.IsWindows())
                return;

            // Check for the exact type and method name used by ijwhost - see src/native/corehost/ijwhost/ijwhost.cpp
            if (Marshal.PtrToStringUni(methodNameNative) == "LoadInMemoryAssemblyInContext"
                && Marshal.PtrToStringUni(typeNameNative) == $"Internal.Runtime.InteropServices.{nameof(InMemoryAssemblyLoader)}, {CoreLib.Name}")
            {
                throw new NotSupportedException(SR.NotSupported_CppCli);
            }
        }
    }
}
