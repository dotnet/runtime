// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal const short KEY_EVENT = 1;

    // Windows's KEY_EVENT_RECORD
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct KeyEventRecord
    {
        internal bool keyDown;
        internal short repeatCount;
        internal short virtualKeyCode;
        internal short virtualScanCode;
        internal char uChar; // Union between WCHAR and ASCII char
        internal int controlKeyState;
    }

    // Really, this is a union of KeyEventRecords and other types.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct InputRecord
    {
        internal short eventType;
        internal KeyEventRecord keyEvent;
        // This struct is a union!  Word alignment should take care of padding!
    }


    internal partial class Kernel32
    {
        
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "ReadConsoleInputW")]
        internal static extern bool ReadConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

    }
}
