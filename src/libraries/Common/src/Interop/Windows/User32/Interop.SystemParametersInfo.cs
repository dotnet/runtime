// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class User32
    {
        public enum SystemParametersAction : uint
        {
            SPI_GETICONTITLELOGFONT = 0x1F,
            SPI_GETNONCLIENTMETRICS = 0x29
        }

        [DllImport(Libraries.User32, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern unsafe bool SystemParametersInfoW(SystemParametersAction uiAction, uint uiParam, void* pvParam, uint fWinIni);
    }
}
