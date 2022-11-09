// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Security;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    public partial class SslStream
    {
        internal sealed class JavaProxy : IDisposable
        {
            private static object s_initializationLock = new();
            private static bool s_initialized;

            private readonly SslStream _sslStream;
            private GCHandle? _handle;

            public unsafe JavaProxy(SslStream sslStream)
            {
                RegisterTrustManagerCallback();

                _sslStream = sslStream;
                _handle = GCHandle.Alloc(this);
            }

            public IntPtr Handle
                => _handle is GCHandle handle
                    ? GCHandle.ToIntPtr(handle)
                    : throw new ObjectDisposedException(nameof(JavaProxy));

            public Exception? CaughtException { get; private set; }

            private static unsafe void RegisterTrustManagerCallback()
            {
                lock (s_initializationLock)
                {
                    if (!s_initialized)
                    {
                        Interop.AndroidCrypto.RegisterTrustManagerCallback(&VerifyRemoteCertificate);
                        s_initialized = true;
                    }
                }
            }

            public void Dispose()
            {
                _handle?.Free();
                _handle = null;
            }

            [UnmanagedCallersOnly]
            private static unsafe bool VerifyRemoteCertificate(IntPtr sslStreamProxyHandle)
            {
                var proxy = (JavaProxy?)GCHandle.FromIntPtr(sslStreamProxyHandle).Target;
                Debug.Assert(proxy is not null);

                try
                {
                    SslStream sslStream = proxy._sslStream;
                    SslAuthenticationOptions options = proxy._sslStream._sslAuthenticationOptions;
                    ProtocolToken? alertToken = null;
                    return sslStream.VerifyRemoteCertificate(options.CertValidationDelegate, options.CertificateContext?.Trust, ref alertToken, out _, out _);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"Remote certificate verification has thrown an exception: {exception}");
                    Debug.WriteLine(exception.StackTrace);

                    proxy.CaughtException = exception;
                    return false;
                }
            }
        }
    }
}
