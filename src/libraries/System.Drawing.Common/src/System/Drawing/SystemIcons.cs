// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Diagnostics.Contracts;

    /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons"]/*' />
    /// <devdoc>
    ///     Icon objects for Windows system-wide icons.
    /// </devdoc>
    public sealed class SystemIcons
    {
        private static Icon s_application;
        private static Icon s_asterisk;
        private static Icon s_error;
        private static Icon s_exclamation;
        private static Icon s_hand;
        private static Icon s_information;
        private static Icon s_question;
        private static Icon s_warning;
        private static Icon s_winlogo;
        private static Icon s_shield;

        // not creatable...
        //
        private SystemIcons()
        {
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.Application"]/*' />
        /// <devdoc>
        ///     Icon is the default Application icon.  (WIN32:  IDI_APPLICATION)
        /// </devdoc>
        public static Icon Application
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_application == null)
                    s_application = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_APPLICATION));
                return s_application;
            }
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.Asterisk"]/*' />
        /// <devdoc>
        ///     Icon is the system Asterisk icon.  (WIN32:  IDI_ASTERISK)
        /// </devdoc>
        public static Icon Asterisk
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_asterisk == null)
                    s_asterisk = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_ASTERISK));
                return s_asterisk;
            }
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.Error"]/*' />
        /// <devdoc>
        ///     Icon is the system Error icon.  (WIN32:  IDI_ERROR)
        /// </devdoc>
        public static Icon Error
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_error == null)
                    s_error = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_ERROR));
                return s_error;
            }
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.Exclamation"]/*' />
        /// <devdoc>
        ///     Icon is the system Exclamation icon.  (WIN32:  IDI_EXCLAMATION)
        /// </devdoc>
        public static Icon Exclamation
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_exclamation == null)
                    s_exclamation = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_EXCLAMATION));
                return s_exclamation;
            }
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.Hand"]/*' />
        /// <devdoc>
        ///     Icon is the system Hand icon.  (WIN32:  IDI_HAND)
        /// </devdoc>
        public static Icon Hand
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_hand == null)
                    s_hand = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_HAND));
                return s_hand;
            }
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.Information"]/*' />
        /// <devdoc>
        ///     Icon is the system Information icon.  (WIN32:  IDI_INFORMATION)
        /// </devdoc>
        public static Icon Information
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_information == null)
                    s_information = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_INFORMATION));
                return s_information;
            }
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.Question"]/*' />
        /// <devdoc>
        ///     Icon is the system Question icon.  (WIN32:  IDI_QUESTION)
        /// </devdoc>
        public static Icon Question
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_question == null)
                    s_question = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_QUESTION));
                return s_question;
            }
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.Warning"]/*' />
        /// <devdoc>
        ///     Icon is the system Warning icon.  (WIN32:  IDI_WARNING)
        /// </devdoc>
        public static Icon Warning
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_warning == null)
                    s_warning = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_WARNING));
                return s_warning;
            }
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.WinLogo"]/*' />
        /// <devdoc>
        ///     Icon is the Windows Logo icon.  (WIN32:  IDI_WINLOGO)
        /// </devdoc>
        public static Icon WinLogo
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_winlogo == null)
                    s_winlogo = new Icon(SafeNativeMethods.LoadIcon(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_WINLOGO));
                return s_winlogo;
            }
        }

        /// <include file='doc\SystemIcons.uex' path='docs/doc[@for="SystemIcons.Shield"]/*' />
        /// <devdoc>
        ///     Icon is the Windows Shield Icon.
        /// </devdoc>
        public static Icon Shield
        {
            get
            {
                Contract.Ensures(Contract.Result<Icon>() != null);

                if (s_shield == null)
                {
                    try
                    {
                        // IDI_SHIELD is defined in OS Vista and above
                        if (Environment.OSVersion.Version.Major >= 6)
                        {
                            // we hard-code size here, to prevent breaking change
                            // the size of _shield before this change is always 32 * 32  
                            IntPtr hIcon = IntPtr.Zero;
                            int result = SafeNativeMethods.LoadIconWithScaleDown(NativeMethods.NullHandleRef, SafeNativeMethods.IDI_SHIELD, 32, 32, ref hIcon);

                            if (result == 0)
                                s_shield = new Icon(hIcon);
                        }
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
    }
}
