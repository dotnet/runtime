// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetAllMountPoints")]
        private static unsafe partial int GetAllMountPoints(delegate* unmanaged<void*, byte*, void> onFound, void* context);

        private struct AllMountPointsContext
        {
            public List<string> _results;
            public ExceptionDispatchInfo? _exception;
        }

        [UnmanagedCallersOnly]
        private static unsafe void AddMountPoint(void* context, byte* name)
        {
            ref AllMountPointsContext callbackContext = ref Unsafe.As<byte, AllMountPointsContext>(ref *(byte*)context);
            callbackContext._results = new List<string>();

            try
            {
                callbackContext._results.Add(Marshal.PtrToStringUTF8((IntPtr)name)!);
            }
            catch (Exception e)
            {
                callbackContext._exception = ExceptionDispatchInfo.Capture(e);
            }
        }

        internal static string[] GetAllMountPoints()
        {
            AllMountPointsContext context = default;
            context._results = new List<string>();

            unsafe
            {
                GetAllMountPoints(&AddMountPoint, Unsafe.AsPointer(ref context));
            }

            if (context._exception != null)
                context._exception.Throw();

            return context._results.ToArray();
        }
    }
}
