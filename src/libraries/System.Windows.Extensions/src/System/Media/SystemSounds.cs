// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Media
{
    public static class SystemSounds
    {
        public static SystemSound Asterisk => field ??= new SystemSound(Interop.User32.MB_ICONASTERISK);
        public static SystemSound Beep => field ??= new SystemSound(Interop.User32.MB_OK);
        public static SystemSound Exclamation => field ??= new SystemSound(Interop.User32.MB_ICONEXCLAMATION);
        public static SystemSound Hand => field ??= new SystemSound(Interop.User32.MB_ICONHAND);
        public static SystemSound Question => field ??= new SystemSound(Interop.User32.MB_ICONQUESTION);
    }
}
