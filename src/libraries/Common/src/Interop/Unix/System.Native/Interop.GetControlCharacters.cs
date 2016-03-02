// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetControlCharacters")]
        internal static extern void GetControlCharacters(
            ControlCharacterNames[] controlCharacterNames, byte[] controlCharacterValues, int controlCharacterLength,
            out byte posixDisableValue);

        internal enum ControlCharacterNames : int
        {
            VINTR = 0,
            VQUIT = 1,
            VERASE = 2,
            VKILL = 3,
            VEOF = 4,
            VTIME = 5,
            VMIN = 6,
            VSWTC = 7,
            VSTART = 8,
            VSTOP = 9,
            VSUSP = 10,
            VEOL = 11,
            VREPRINT = 12,
            VDISCARD = 13,
            VWERASE = 14,
            VLNEXT = 15,
            VEOL2 = 16
        };
    }
}
