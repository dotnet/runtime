// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        // This struct is a union!  Word alighment should take care of padding!
    }


    internal partial class mincore
    {
        
        [DllImport(Libraries.Console_L1, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "ReadConsoleInputW")]
        internal static extern bool ReadConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);

    }
}
