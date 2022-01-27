// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxCreate")]
        internal static partial SafeSslContextHandle SslCtxCreate(IntPtr method);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxDestroy")]
        internal static partial void SslCtxDestroy(IntPtr ctx);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxGetData")]
        internal static partial IntPtr SslCtxGetData(IntPtr ctx);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetData")]
        internal static partial int SslCtxSetData(SafeSslContextHandle ctx, IntPtr data);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetData")]
        internal static partial int SslCtxSetData(IntPtr ctx, IntPtr data);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetAlpnSelectCb")]
        internal static unsafe partial void SslCtxSetAlpnSelectCb(SafeSslContextHandle ctx, delegate* unmanaged<IntPtr, byte**, byte*, byte*, uint, IntPtr, int> callback, IntPtr arg);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetCaching")]
        internal static unsafe partial void SslCtxSetCaching(SafeSslContextHandle ctx, int mode, delegate* unmanaged<IntPtr, IntPtr, int> neewSessionCallback, delegate* unmanaged<IntPtr, IntPtr, void> removeSessionCallback);

        internal static bool AddExtraChainCertificates(SafeSslContextHandle ctx, X509Certificate2[] chain)
        {
            // send pre-computed list of intermediates.
            for (int i = 0; i < chain.Length; i++)
            {
                SafeX509Handle dupCertHandle = Crypto.X509UpRef(chain[i].Handle);
                Crypto.CheckValidOpenSslHandle(dupCertHandle);
                if (!SslCtxAddExtraChainCert(ctx, dupCertHandle))
                {
                    Crypto.ErrClearError();
                    dupCertHandle.Dispose(); // we still own the safe handle; clean it up
                    return false;
                }
                dupCertHandle.SetHandleAsInvalid(); // ownership has been transferred to sslHandle; do not free via this safe handle
            }

            return true;
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeSslContextHandle : SafeHandle
    {
        private ConcurrentDictionary<string, IntPtr>? _sslSessions;
        private GCHandle _gch;

        public SafeSslContextHandle()
            : base(IntPtr.Zero, true)
        {
        }

        internal SafeSslContextHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            Interop.Ssl.SslCtxDestroy(handle);
            SetHandle(IntPtr.Zero);
            if (_gch.IsAllocated)
            {
                //Interop.Ssl.SslCtxSetData(this, (IntPtr)_gch);
                _gch.Free();
            }

            if (_sslSessions != null)
            {
                lock (_sslSessions)
                {
                    foreach (string name in _sslSessions.Keys)
                    {
                        _sslSessions.Remove(name, out IntPtr session);
                        Interop.Ssl.SessionFree(session);
                    }
                }
            }

            return true;
        }

        public void EnableSessionCache()
        {
            _sslSessions = new ConcurrentDictionary<string, IntPtr>();
            _gch = GCHandle.Alloc(this);
            // This is needed so we can find the handle from session remove callback.
            Interop.Ssl.SslCtxSetData(this, (IntPtr)_gch);
        }

        public bool TryAddSession(IntPtr serverName, IntPtr session)
        {
            Debug.Assert(_sslSessions != null && session != IntPtr.Zero);

            if (_sslSessions == null || serverName == IntPtr.Zero)
            {
                return false;
            }

            string? name = Marshal.PtrToStringAnsi(serverName);
            if (!string.IsNullOrEmpty(name))
            {
                Interop.Ssl.SessionSetHostname(session, serverName);

                lock (_sslSessions)
                {
                    IntPtr oldSession = _sslSessions.GetOrAdd(name, session);
                    if (oldSession != session)
                    {
                        _sslSessions.Remove(name, out oldSession);
                         Interop.Ssl.SessionFree(oldSession);
                        oldSession = _sslSessions.GetOrAdd(name, session);
                        Debug.Assert(oldSession == session);
                    }
                }

                return true;
            }

            return false;
        }

        public void Remove(string name, IntPtr session)
        {
            if (_sslSessions != null)
            {
                lock (_sslSessions)
                {
                    if (!_sslSessions.Remove(name, out IntPtr oldSession))
                    {
                        Interop.Ssl.SessionFree(oldSession);
                    }
                }
            }
        }

        public bool TrySetSession(SafeSslHandle sslHandle, string name)
        {
            Debug.Assert(_sslSessions != null);

            if (_sslSessions == null || string.IsNullOrEmpty(name))
            {
                return false;
            }

            // even if we don't have matching session, we can get new one and we need
            // way how to link SSL back to `this`.
            Interop.Ssl.SslSetData(sslHandle, (IntPtr)_gch);

            lock (_sslSessions)
            {
                if (_sslSessions.TryGetValue(name, out IntPtr session))
                {
                    // This will increase reference count on the session as needed.
                    // We need to hold lock here to prevent session being deleted before the call is done.
                    Interop.Ssl.SslSetSession(sslHandle, session);

                    return true;
                }
            }

            return false;
        }
    }
}
