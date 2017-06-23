// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;

namespace System.Drawing
{
    public static class SystemIcons
    {
        private static Icon s_application = null;
        private static Icon s_asterisk = null;
        private static Icon s_error = null;
        private static Icon s_exclamation = null;
        private static Icon s_hand = null;
        private static Icon s_information = null;
        private static Icon s_question = null;
        private static Icon s_warning = null;
        private static Icon s_winlogo = null;
        private static Icon s_shield = null;

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
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_shield == null)
                {
                    try
                    {
                        // we hard-code size here, to prevent breaking change
                        // the size of _shield before this change is always 32 * 32  
                        IntPtr hIcon = IntPtr.Zero;
                        int result = SafeNativeMethods.LoadIconWithScaleDown(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_SHIELD, 32, 32, ref hIcon);

                        if (result == 0)
                            s_shield = new Icon(hIcon);
                    }
                    catch
                    {
                        // we don't want to throw exception here.
                        // If there is an exception, we will load an icon from file ShieldIcon.ico
                    }
                }
                if (s_shield == null)
                {
                    s_shield = new Icon(typeof(SystemIcons), "ShieldIcon.ico");
                }
                return s_shield;
            }
        }

        private static Icon GetIcon(ref Icon icon, int iconId)
        {
            return icon ?? (icon = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, iconId)));
        }
    }
}
