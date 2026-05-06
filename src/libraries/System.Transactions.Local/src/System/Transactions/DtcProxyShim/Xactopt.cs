// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace System.Transactions.DtcProxyShim;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms679195(v=vs.85)
[NativeMarshalling(typeof(Marshaller))]
[StructLayout(LayoutKind.Sequential)]
internal struct Xactopt
{
    internal Xactopt(uint ulTimeout, string szDescription)
        => (UlTimeout, SzDescription) = (ulTimeout, szDescription);

    public uint UlTimeout;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
    public string SzDescription;

    [CustomMarshaller(typeof(Xactopt), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
    [CustomMarshaller(typeof(Xactopt), MarshalMode.UnmanagedToManagedIn, typeof(Marshaller))]
    internal static class Marshaller
    {
        internal struct XactoptNative
        {
            public uint UlTimeout;

            public SzDescription SzDescription;
        }

        [InlineArray(40)]
        internal struct SzDescription
        {
            private byte _element0;
        }

        public static XactoptNative ConvertToUnmanaged(Xactopt managed)
        {
            XactoptNative native = new()
            {
                UlTimeout = managed.UlTimeout,
            };

            // Usage of Xactopt never passes non-ASCII chars, so we can ignore them.
            Encoding.ASCII.TryGetBytes(managed.SzDescription, native.SzDescription, out _);

            return native;
        }

        public static Xactopt ConvertToManaged(XactoptNative unmanaged)
        => new(unmanaged.UlTimeout, Encoding.ASCII.GetString(unmanaged.SzDescription));
    }
}
