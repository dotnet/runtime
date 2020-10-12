// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal.Tls.OpenSsl;
using System.Runtime.InteropServices;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal class OpenSslQuicMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeCallbacks
        {
            internal IntPtr setEncryptionSecrets;
            internal IntPtr addHandshakeData;
            internal IntPtr flushFlight;
            internal IntPtr sendAlert;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int SetEncryptionSecretsFunc(IntPtr ssl, OpenSslEncryptionLevel level, byte* readSecret,
            byte* writeSecret, UIntPtr secretLen);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int AddHandshakeDataFunc(IntPtr ssl, OpenSslEncryptionLevel level, byte* data, UIntPtr len);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int FlushFlightFunc(IntPtr ssl);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int SendAlertFunc(IntPtr ssl, OpenSslEncryptionLevel level, byte alert);
    }
}
