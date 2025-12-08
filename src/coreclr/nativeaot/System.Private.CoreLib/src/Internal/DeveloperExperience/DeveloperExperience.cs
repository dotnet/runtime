// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;

using Internal.Runtime.Augments;

namespace Internal.DeveloperExperience
{
    internal static class DeveloperExperience
    {
        internal static string GetMethodName(IntPtr ip, out IntPtr methodStart, out bool isStackTraceHidden)
        {
            methodStart = IntPtr.Zero;
            StackTraceMetadataCallbacks stackTraceCallbacks = RuntimeAugments.StackTraceCallbacksIfAvailable;
            if (stackTraceCallbacks != null)
            {
                methodStart = RuntimeImports.RhFindMethodStartAddress(ip);
                if (methodStart != IntPtr.Zero)
                {
                    return stackTraceCallbacks.TryGetMethodNameFromStartAddress(methodStart, out isStackTraceHidden);
                }
            }
            isStackTraceHidden = false;
            return null;
        }
    }
}
