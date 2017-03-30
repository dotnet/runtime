// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Net
{
    // _SecPkgInfoW in sspi.h.
    internal class SecurityPackageInfoClass
    {
        internal int Capabilities = 0;
        internal short Version = 0;
        internal short RPCID = 0;
        internal int MaxToken = 0;
        internal string Name = null;
        internal string Comment = null;

        /*
            This is to support SSL with no client cert.
            Important: safeHandle should not be Disposed during construction of this object.
           
            _SecPkgInfoW in sspi.h
         */
        internal SecurityPackageInfoClass(SafeHandle safeHandle, int index)
        {
            if (safeHandle.IsInvalid)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Invalid handle: {safeHandle}");
                return;
            }

            IntPtr unmanagedAddress = IntPtrHelper.Add(safeHandle.DangerousGetHandle(), SecurityPackageInfo.Size * index);
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"unmanagedAddress: {unmanagedAddress}");

            // TODO (Issue #3114): replace with Marshal.PtrToStructure.
            Capabilities = Marshal.ReadInt32(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("Capabilities"));
            Version = Marshal.ReadInt16(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("Version"));
            RPCID = Marshal.ReadInt16(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("RPCID"));
            MaxToken = Marshal.ReadInt32(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("MaxToken"));

            IntPtr unmanagedString;

            unmanagedString = Marshal.ReadIntPtr(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("Name"));
            if (unmanagedString != IntPtr.Zero)
            {
                Name = Marshal.PtrToStringUni(unmanagedString);
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Name: {Name}");
            }

            unmanagedString = Marshal.ReadIntPtr(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("Comment"));
            if (unmanagedString != IntPtr.Zero)
            {
                Comment = Marshal.PtrToStringUni(unmanagedString);
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Comment: {Comment}");
            }

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, this.ToString());
        }

        public override string ToString()
        {
            return "Capabilities:" + String.Format(CultureInfo.InvariantCulture, "0x{0:x}", Capabilities)
                + " Version:" + Version.ToString(NumberFormatInfo.InvariantInfo)
                + " RPCID:" + RPCID.ToString(NumberFormatInfo.InvariantInfo)
                + " MaxToken:" + MaxToken.ToString(NumberFormatInfo.InvariantInfo)
                + " Name:" + ((Name == null) ? "(null)" : Name)
                + " Comment:" + ((Comment == null) ? "(null)" : Comment);
        }
    }
}
