// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Xml;
    using System.Collections;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    internal class NamespaceUtils
    {
        private NamespaceUtils() { }

        private static XmlNamespaceManager s_xmlNamespace = new XmlNamespaceManager(new NameTable());

        static NamespaceUtils()
        {
            s_xmlNamespace.AddNamespace("se", DsmlConstants.SoapUri);
            s_xmlNamespace.AddNamespace("dsml", DsmlConstants.DsmlUri);
            s_xmlNamespace.AddNamespace("ad", DsmlConstants.ADSessionUri);
            s_xmlNamespace.AddNamespace("xsd", DsmlConstants.XsdUri);
            s_xmlNamespace.AddNamespace("xsi", DsmlConstants.XsiUri);
        }

        /// <summary>
        /// This is the common namespace manager we use everywhere we need a XmlNamespaceManager.
        /// For example, every XPath selection should use this to provide its XmlNamespaceManager,
        /// when defining a XPath query string.  This ensures that XPath query strings can be
        /// passed between different classes in the API, without worrying that they may
        /// use different prefixes to represent the same namespace.
        /// </summary>
        static public XmlNamespaceManager GetDsmlNamespaceManager()
        {
            return s_xmlNamespace;
        }
    }

    internal class Utility
    {
        private static bool s_platformSupported = false;
        private static bool s_isWin2kOS = false;
        private static bool s_isWin2k3Above = false;

        static Utility()
        {
            // check the platform first
            // S.DS.Protocols only supported on W2K above
            //    
            OperatingSystem osVersion = Environment.OSVersion;
            if ((osVersion.Platform == PlatformID.Win32NT) && (osVersion.Version.Major >= 5))
            {
                s_platformSupported = true;
                if (osVersion.Version.Major == 5 && osVersion.Version.Minor == 0)
                    s_isWin2kOS = true;

                // win2k3's major version is 5, minor version is 2
                if (osVersion.Version.Major > 5 || osVersion.Version.Minor >= 2)
                    s_isWin2k3Above = true;
            }
        }

        internal static void CheckOSVersion()
        {
            if (!s_platformSupported)
                throw new PlatformNotSupportedException(Res.GetString(Res.SupportedPlatforms));
        }

        internal static bool IsWin2kOS
        {
            get
            {
                return s_isWin2kOS;
            }
        }

        internal static bool IsWin2k3AboveOS
        {
            get
            {
                return s_isWin2k3Above;
            }
        }

        internal static bool IsLdapError(LdapError error)
        {
            if (error == LdapError.IsLeaf || error == LdapError.InvalidCredentials || error == LdapError.SendTimeOut)
                return true;

            if (error >= LdapError.ServerDown && error <= LdapError.ReferralLimitExceeded)
                return true;

            return false;
        }

        internal static bool IsResultCode(ResultCode code)
        {
            if (code >= ResultCode.Success && code <= ResultCode.SaslBindInProgress)
                return true;

            if (code >= ResultCode.NoSuchAttribute && code <= ResultCode.InvalidAttributeSyntax)
                return true;

            if (code >= ResultCode.NoSuchObject && code <= ResultCode.InvalidDNSyntax)
                return true;

            if (code >= ResultCode.InsufficientAccessRights && code <= ResultCode.LoopDetect)
                return true;

            if (code >= ResultCode.NamingViolation && code <= ResultCode.AffectsMultipleDsas)
                return true;

            if (code == ResultCode.AliasDereferencingProblem || code == ResultCode.InappropriateAuthentication || code == ResultCode.SortControlMissing || code == ResultCode.OffsetRangeError || code == ResultCode.VirtualListViewError || code == ResultCode.Other)
                return true;

            return false;
        }

        internal static IntPtr AllocHGlobalIntPtrArray(int size)
        {
            IntPtr intPtrArray = (IntPtr)0;
            checked
            {
                intPtrArray = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)) * size);
                for (int i = 0; i < size; i++)
                {
                    IntPtr tempPtr = (IntPtr)((long)intPtrArray + Marshal.SizeOf(typeof(IntPtr)) * i);
                    Marshal.WriteIntPtr(tempPtr, IntPtr.Zero);
                }
            }
            return intPtrArray;
        }
    }
}
