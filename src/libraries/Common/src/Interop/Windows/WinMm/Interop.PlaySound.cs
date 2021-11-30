// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class WinMM
    {
        internal const int SND_SYNC = 0x0;
        internal const int SND_ASYNC = 0x1;
        internal const int SND_NODEFAULT = 0x2;
        internal const int SND_MEMORY = 0x4;
        internal const int SND_LOOP = 0x8;
        internal const int SND_PURGE = 0x40;
        internal const int SND_FILENAME = 0x20000;
        internal const int SND_NOSTOP = 0x10;

        [GeneratedDllImport(Libraries.WinMM, EntryPoint = "PlaySoundW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static partial bool PlaySound(string soundName, IntPtr hmod, int soundFlags);

        [GeneratedDllImport(Libraries.WinMM, EntryPoint = "PlaySoundW", ExactSpelling = true)]
        internal static partial bool PlaySound(byte[]? soundName, IntPtr hmod, int soundFlags);
    }
}
