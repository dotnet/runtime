// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    internal static class Marshal
    {
        public static void Copy(IntPtr source, byte[] destination, int startIndex, int length)
        {
            InteropExtensions.CopyToManaged(source, destination, startIndex, length);
        }

        public static void Copy(byte[] array, int startIndex, IntPtr destination, int length)
        {
            InteropExtensions.CopyToNative(array, startIndex, destination, length);
        }
        
        public static IntPtr AllocHGlobal(int cb)
        {
            return InteropExtensions.MemAlloc(new UIntPtr((uint)cb));
        }

        public static void FreeHGlobal(IntPtr hglobal)
        {
            InteropExtensions.MemFree(hglobal);
        }
    }
}