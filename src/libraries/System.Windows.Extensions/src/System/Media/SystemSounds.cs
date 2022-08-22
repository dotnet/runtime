// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Media
{
    public static class SystemSounds
    {
        private static volatile SystemSound? s_asterisk;
        private static volatile SystemSound? s_beep;
        private static volatile SystemSound? s_exclamation;
        private static volatile SystemSound? s_hand;
        private static volatile SystemSound? s_question;

        public static SystemSound Asterisk
        {
            get => s_asterisk ??= new SystemSound(Interop.User32.MB_ICONASTERISK);
        }

        public static SystemSound Beep
        {
            get => s_beep ??= new SystemSound(Interop.User32.MB_OK);
        }

        public static SystemSound Exclamation
        {
            get => s_exclamation ??= new SystemSound(Interop.User32.MB_ICONEXCLAMATION);
        }

        public static SystemSound Hand
        {
            get => s_hand ??= new SystemSound(Interop.User32.MB_ICONHAND);
        }

        public static SystemSound Question
        {
            get => s_question ??= new SystemSound(Interop.User32.MB_ICONQUESTION);
        }
    }
}
