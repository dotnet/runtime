// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Gdi32
    {
        public enum StockObject : int
        {
            DEFAULT_GUI_FONT = 17
        }

        [LibraryImport(Libraries.Gdi32)]
        public static partial IntPtr GetStockObject(StockObject i);
    }
}
