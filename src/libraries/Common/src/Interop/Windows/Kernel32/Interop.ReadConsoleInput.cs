// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal const short KEY_EVENT = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct KEY_EVENT_RECORD
    {
        internal BOOL bKeyDown;
        internal short wRepeatCount;
        internal short wVirtualKeyCode;
        internal short wVirtualScanCode;
        internal char uChar; // Union between WCHAR and ASCII char
        internal int dwControlKeyState;
    }

    // Really, this is a union of KeyEventRecords and other types.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct InputRecord
    {
        internal short eventType;
        internal KEY_EVENT_RECORD keyEvent;
        // This struct is a union!  Word alignment should take care of padding!
    }


    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "ReadConsoleInputW",  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ReadConsoleInput(IntPtr hConsoleInput, out InputRecord buffer, int numInputRecords_UseOne, out int numEventsRead);
    }
}
