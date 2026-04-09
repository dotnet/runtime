// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        internal static unsafe int CoGetObjectContext(in Guid riid, out IntPtr ppv)
        {
            fixed (Guid* riidPtr = &riid)
            fixed (IntPtr* ppvPtr = &ppv)
            {
                return CoGetObjectContext(riidPtr, ppvPtr);
            }
        }

        [LibraryImport(Libraries.Ole32)]
        internal static unsafe partial int CoGetObjectContext(Guid* riid, IntPtr* ppv);
    }
}
