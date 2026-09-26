// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxCreate")]
        internal static partial SafeSslContextHandle SslCtxCreate(IntPtr method);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxDestroy")]
        internal static partial void SslCtxDestroy(IntPtr ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxGetData")]
        internal static partial IntPtr SslCtxGetData(IntPtr ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetData")]
        internal static partial int SslCtxSetData(SafeSslContextHandle ctx, IntPtr data);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetData")]
        internal static partial int SslCtxSetData(IntPtr ctx, IntPtr data);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetAlpnSelectCb")]
        internal static unsafe partial void SslCtxSetAlpnSelectCb(SafeSslContextHandle ctx, delegate* unmanaged<IntPtr, byte**, byte*, byte*, uint, IntPtr, int> callback, IntPtr arg);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetKeylogCallback")]
        internal static unsafe partial void SslCtxSetKeylogCallback(SafeSslContextHandle ctx, delegate* unmanaged<IntPtr, char*, void> callback);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetCaching")]
        internal static unsafe partial int SslCtxSetCaching(SafeSslContextHandle ctx, int mode, int cacheSize, int contextIdLength, Span<byte> contextId, delegate* unmanaged<IntPtr, IntPtr, int> neewSessionCallback, delegate* unmanaged<IntPtr, IntPtr, void> removeSessionCallback);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxRemoveSession")]
        internal static unsafe partial void SslCtxRemoveSession(SafeSslContextHandle ctx, IntPtr session);

        internal static bool AddExtraChainCertificates(SafeSslContextHandle ctx, ReadOnlyCollection<X509Certificate2> chain)
        {
            // send pre-computed list of intermediates.
            for (int i = 0; i < chain.Count; i++)
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

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetCertVerifyCallback")]
        internal static unsafe partial void SslCtxSetCertVerifyCallback(SafeSslContextHandle ctx, delegate* unmanaged<IntPtr, IntPtr, int> callback);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetSslPtr")]
        internal static partial IntPtr X509StoreCtxGetSslPtr(IntPtr storeCtx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxSetError")]
        internal static partial void X509StoreCtxSetError(IntPtr storeCtx, int error);
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeSslContextHandle : SafeHandle, ISafeHandleCachable
    {
        // OpenSSL retires a TLS 1.3 session when the handshake using it finishes
        // (tls_finish_handshake calls SSL_CTX_remove_session, which sets not_resumable on
        // the shared object), so offering one session to several concurrent handshakes
        // silently downgrades all but the first to a full handshake. Pooling several
        // tickets per host lets concurrent connections each take a distinct one.
        private const int TlsResumePoolSize = 8;

        private readonly struct CachedSession(IntPtr session, bool isTls13)
        {
            public IntPtr Session { get; } = session;
            public bool IsTls13 { get; } = isTls13;
        }

        // This is session cache keyed by SNI e.g. TargetHost
        private Dictionary<string, List<CachedSession>>? _sslSessions;
        private GCHandle _gch;

        // SSL_CTX handles are cached, so we need to keep track of the
        // number of times a handle is being used. Once we decide to dispose the handle,
        // we set the _rentCount to -1.
        private volatile int _rentCount;

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

        public bool TryAddRentCount()
        {
            int oldCount;

            do
            {
                oldCount = _rentCount;
                if (oldCount < 0)
                {
                    // The handle is already disposed.
                    return false;
                }
            } while (Interlocked.CompareExchange(ref _rentCount, oldCount + 1, oldCount) != oldCount);

            return true;
        }

        public bool TryMarkForDispose()
        {
            return Interlocked.CompareExchange(ref _rentCount, -1, 0) == 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Decrement(ref _rentCount) < 0)
            {
                // _rentCount is 0 if the handle was never rented (e.g. failure during creation),
                // and is -1 when evicted from cache.
                base.Dispose(disposing);
            }
        }

        protected override bool ReleaseHandle()
        {
            if (_sslSessions != null)
            {
                // The SSL_CTX is ref counted and may not immediately die when we call SslCtxDestroy()
                // Since there is no relation between SafeSslContextHandle and SafeSslHandle `this`
                // can be released while we still have SSL session using it.
                Interop.Ssl.SslCtxSetData(handle, IntPtr.Zero);

                lock (_sslSessions)
                {
                    foreach (List<CachedSession> sessions in _sslSessions.Values)
                    {
                        foreach (CachedSession cached in sessions)
                        {
                            Interop.Ssl.SessionFree(cached.Session);
                        }
                    }

                    _sslSessions.Clear();
                }

                Debug.Assert(_gch.IsAllocated);
                _gch.Free();
            }

            Interop.Ssl.SslCtxDestroy(handle);
            SetHandle(IntPtr.Zero);

            return true;
        }

        internal void EnableSessionCache()
        {
            Debug.Assert(_sslSessions == null);

            _sslSessions = new Dictionary<string, List<CachedSession>>();
            _gch = GCHandle.Alloc(this);
            Debug.Assert(_gch.IsAllocated);
            // This is needed so we can find the handle from session in SessionRemove callback.
            Interop.Ssl.SslCtxSetData(this, (IntPtr)_gch);
        }

        internal unsafe bool TryAddSession(byte* namePtr, IntPtr session, bool isTls13)
        {
            Debug.Assert(_sslSessions != null && session != IntPtr.Zero);

            if (_sslSessions == null || namePtr == null)
            {
                return false;
            }

            string? targetName = Utf8StringMarshaller.ConvertToManaged(namePtr);
            Debug.Assert(targetName != null);

            if (string.IsNullOrEmpty(targetName))
            {
                return false;
            }

            // We do this only for lookup in RemoveSession.
            // Since this is part of cache manipulation and no function impact it is done here.
            // This will use strdup() so it is safe to pass in raw pointer.
            Interop.Ssl.SessionSetHostname(session, namePtr);

            // A TLS 1.2 session stays usable after a resumption and is never replaced by a
            // new one (OpenSSL skips new_session_cb on resumed TLS 1.2 handshakes), so a
            // single entry is both sufficient and all we will ever be given.
            int limit = isTls13 ? TlsResumePoolSize : 1;

            IntPtr[]? evicted = null;
            int evictedCount = 0;

            lock (_sslSessions)
            {
                if (!_sslSessions.TryGetValue(targetName, out List<CachedSession>? sessions))
                {
                    sessions = new List<CachedSession>();
                    _sslSessions[targetName] = sessions;
                }

                // Pooled tickets are only usable by the protocol version that produced them,
                // so a change of negotiated version drops the pool rather than leaving a
                // stale entry at the head masking everything behind it.
                int toEvict = sessions.Count > 0 && sessions[0].IsTls13 != isTls13
                    ? sessions.Count
                    : Math.Max(0, sessions.Count - limit + 1);

                if (toEvict > 0)
                {
                    evicted = new IntPtr[toEvict];
                    for (; evictedCount < toEvict; evictedCount++)
                    {
                        evicted[evictedCount] = sessions[evictedCount].Session;
                    }

                    sessions.RemoveRange(0, toEvict);
                }

                sessions.Add(new CachedSession(session, isTls13));
            }

            for (int i = 0; i < evictedCount; i++)
            {
                // Remove the evicted session also from the internal OpenSSL cache and drop
                // the reference count. Since SSL_CTX_remove_session will call
                // session_remove_cb, we need to do this outside of the _sslSessions lock to
                // avoid deadlock with another thread which could be holding the SSL_CTX lock
                // and trying to acquire _sslSessions.
                Interop.Ssl.SslCtxRemoveSession(this, evicted![i]);
                Interop.Ssl.SessionFree(evicted[i]);
            }

            return true;
        }

        internal unsafe void RemoveSession(byte* namePtr, IntPtr session)
        {
            Debug.Assert(_sslSessions != null);

            if (_sslSessions == null || namePtr == null)
            {
                return;
            }

            string? targetName = Utf8StringMarshaller.ConvertToManaged(namePtr);
            Debug.Assert(targetName != null);

            if (targetName == null)
            {
                return;
            }

            bool removed = false;

            lock (_sslSessions)
            {
                if (_sslSessions.TryGetValue(targetName, out List<CachedSession>? sessions))
                {
                    for (int i = 0; i < sessions.Count; i++)
                    {
                        if (sessions[i].Session == session)
                        {
                            sessions.RemoveAt(i);
                            removed = true;
                            break;
                        }
                    }

                    if (sessions.Count == 0)
                    {
                        _sslSessions.Remove(targetName);
                    }
                }
            }

            if (removed)
            {
                // It seems like we may be called more than once. Since we grabbed only one
                // reference when added to the cache, we will also drop exactly one when removed.
                Interop.Ssl.SessionFree(session);
            }
        }

        internal bool TrySetSession(SafeSslHandle sslHandle, string name)
        {
            Debug.Assert(_sslSessions != null);

            if (_sslSessions == null || string.IsNullOrEmpty(name))
            {
                return false;
            }

            IntPtr owned;

            lock (_sslSessions)
            {
                if (!_sslSessions.TryGetValue(name, out List<CachedSession>? sessions) || sessions.Count == 0)
                {
                    return false;
                }

                CachedSession cached = sessions[0];

                // While the pool holds more than one ticket each concurrent handshake can
                // take its own. The last one is still shared rather than withheld, since a
                // shared ticket only costs a fallback to a full handshake, while withholding
                // it guarantees one.
                bool singleUse = cached.IsTls13 && sessions.Count > 1;

                if (singleUse)
                {
                    // Taking the entry out of the cache transfers the cache's reference to us.
                    // The pool holds more than one entry here, so it cannot become empty.
                    sessions.RemoveAt(0);
                }
                else if (Interop.Ssl.SessionUpRef(cached.Session) != 1)
                {
                    return false;
                }

                owned = cached.Session;
            }

            // Held outside the lock: RemoveSession frees on a callback OpenSSL raises while
            // holding the SSL_CTX lock, so the reference taken above, not the lock, is what
            // keeps the session alive across this call.
            bool set = Interop.Ssl.SslSetSession(sslHandle, owned) == 1;
            Interop.Ssl.SessionFree(owned);

            return set;
        }
    }
}
