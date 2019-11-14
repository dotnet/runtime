// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        [DllImport(Libraries.Gdi32, ExactSpelling = true)]
        public static extern bool DeleteObject(IntPtr ho);

        public static bool DeleteObject(HandleRef ho)
        {
            bool result = DeleteObject(ho.Handle);
            GC.KeepAlive(ho.Wrapper);
            return result;
        }
    }
}
