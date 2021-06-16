// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Net
{
    // _SecPkgInfoW in sspi.h.
    internal sealed class SecurityPackageInfoClass
    {
        internal int Capabilities;
        internal short Version;
        internal short RPCID;
        internal int MaxToken;
        internal string? Name;
        internal string? Comment;

        /*
            This is to support SSL with no client cert.
            Important: safeHandle should not be Disposed during construction of this object.

            _SecPkgInfoW in sspi.h
         */
        internal unsafe SecurityPackageInfoClass(SafeHandle safeHandle, int index)
        {
            if (safeHandle.IsInvalid)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Invalid handle: {safeHandle}");
                return;
            }

            IntPtr unmanagedAddress = safeHandle.DangerousGetHandle() + (sizeof(SecurityPackageInfo) * index);
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"unmanagedAddress: {unmanagedAddress}");

            SecurityPackageInfo* pSecurityPackageInfo = (SecurityPackageInfo*)unmanagedAddress;

            Capabilities = pSecurityPackageInfo->Capabilities;
            Version = pSecurityPackageInfo->Version;
            RPCID = pSecurityPackageInfo->RPCID;
            MaxToken = pSecurityPackageInfo->MaxToken;

            IntPtr unmanagedString;

            unmanagedString = pSecurityPackageInfo->Name;
            if (unmanagedString != IntPtr.Zero)
            {
                Name = Marshal.PtrToStringUni(unmanagedString);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Name: {Name}");
            }

            unmanagedString = pSecurityPackageInfo->Comment;
            if (unmanagedString != IntPtr.Zero)
            {
                Comment = Marshal.PtrToStringUni(unmanagedString);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Comment: {Comment}");
            }

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, this.ToString());
        }

        public override string ToString()
        {
            return "Capabilities:" + string.Format(CultureInfo.InvariantCulture, "0x{0:x}", Capabilities)
                + " Version:" + Version.ToString(NumberFormatInfo.InvariantInfo)
                + " RPCID:" + RPCID.ToString(NumberFormatInfo.InvariantInfo)
                + " MaxToken:" + MaxToken.ToString(NumberFormatInfo.InvariantInfo)
                + " Name:" + ((Name == null) ? "(null)" : Name)
                + " Comment:" + ((Comment == null) ? "(null)" : Comment);
        }
    }
}
