// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Drawing
{
    public static class SystemIcons
    {
        private static Icon? s_application;
        private static Icon? s_asterisk;
        private static Icon? s_error;
        private static Icon? s_exclamation;
        private static Icon? s_hand;
        private static Icon? s_information;
        private static Icon? s_question;
        private static Icon? s_warning;
        private static Icon? s_winlogo;
        private static Icon? s_shield;

        public static Icon Application => GetIcon(ref s_application, SafeNativeMethods.IDI_APPLICATION);

        public static Icon Asterisk => GetIcon(ref s_asterisk, SafeNativeMethods.IDI_ASTERISK);

        public static Icon Error => GetIcon(ref s_error, SafeNativeMethods.IDI_ERROR);

        public static Icon Exclamation => GetIcon(ref s_exclamation, SafeNativeMethods.IDI_EXCLAMATION);

        public static Icon Hand => GetIcon(ref s_hand, SafeNativeMethods.IDI_HAND);

        public static Icon Information => GetIcon(ref s_information, SafeNativeMethods.IDI_INFORMATION);

        public static Icon Question => GetIcon(ref s_question, SafeNativeMethods.IDI_QUESTION);

        public static Icon Warning => GetIcon(ref s_warning, SafeNativeMethods.IDI_WARNING);

        public static Icon WinLogo => GetIcon(ref s_winlogo, SafeNativeMethods.IDI_WINLOGO);

        public static Icon Shield
        {
            get
            {
                if (s_shield == null)
                {
                    s_shield = new Icon(typeof(SystemIcons), "ShieldIcon.ico");
                    Debug.Assert(s_shield != null, "ShieldIcon.ico must be present as an embedded resource in System.Drawing.Common.");
                }

                return s_shield;
            }
        }

        private static Icon GetIcon(ref Icon? icon, int iconId)
        {
            return icon ??= new Icon(Interop.User32.LoadIcon(NativeMethods.NullHandleRef, (IntPtr)iconId));
        }
    }
}
