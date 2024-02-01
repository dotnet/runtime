// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    //
    // Used when working with SSPI APIs, like SafeSspiAuthDataHandle(). Holds the pointer to the auth data blob.
    //
#if DEBUG
    internal sealed class SafeSspiAuthDataHandle : DebugSafeHandle
    {
#else
    internal sealed class SafeSspiAuthDataHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
#endif
        public SafeSspiAuthDataHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Interop.SspiCli.SspiFreeAuthIdentity(handle) == Interop.SECURITY_STATUS.OK;
        }
    }

    //
    //  A set of Safe Handles that depend on native FreeContextBuffer finalizer.
    //
#if DEBUG
    internal abstract class SafeFreeContextBuffer : DebugSafeHandle
    {
#else
    internal abstract class SafeFreeContextBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
#endif
        protected SafeFreeContextBuffer() : base(true) { }

        // This must be ONLY called from this file.
        internal void Set(IntPtr value)
        {
            this.handle = value;
        }

        internal static int EnumeratePackages(out int pkgnum, out SafeFreeContextBuffer pkgArray)
        {
            int res = Interop.SspiCli.EnumerateSecurityPackagesW(out pkgnum, out SafeFreeContextBuffer_SECURITY? pkgArray_SECURITY);
            pkgArray = pkgArray_SECURITY;

            if (res != 0)
            {
                pkgArray?.SetHandleAsInvalid();
            }

            return res;
        }

        internal static SafeFreeContextBuffer CreateEmptyHandle()
        {
            return new SafeFreeContextBuffer_SECURITY();
        }

        //
        // After PInvoke call the method will fix the refHandle.handle with the returned value.
        // The caller is responsible for creating a correct SafeHandle template or null can be passed if no handle is returned.
        //
        // This method switches between three non-interruptible helper methods.  (This method can't be both non-interruptible and
        // reference imports from all three DLLs - doing so would cause all three DLLs to try to be bound to.)
        //
        public static unsafe int QueryContextAttributes(SafeDeleteContext phContext, Interop.SspiCli.ContextAttribute contextAttribute, byte* buffer, SafeHandle? refHandle)
        {
            int status = (int)Interop.SECURITY_STATUS.InvalidHandle;

            bool mustRelease = false;
            try
            {
                phContext.DangerousAddRef(ref mustRelease);
                status = Interop.SspiCli.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
            }
            finally
            {
                if (mustRelease)
                {
                    phContext.DangerousRelease();
                }
            }

            if (status == 0 && refHandle != null)
            {
                if (refHandle is SafeFreeContextBuffer)
                {
                    ((SafeFreeContextBuffer)refHandle).Set(*(IntPtr*)buffer);
                }
                else
                {
                    ((SafeFreeCertContext)refHandle).Set(*(IntPtr*)buffer);
                }
            }

            if (status != 0)
            {
                refHandle?.SetHandleAsInvalid();
            }

            return status;
        }

        public static int SetContextAttributes(
            SafeDeleteContext phContext,
            Interop.SspiCli.ContextAttribute contextAttribute, byte[] buffer)
        {
            bool mustRelease = false;
            try
            {
                phContext.DangerousAddRef(ref mustRelease);
                return Interop.SspiCli.SetContextAttributesW(ref phContext._handle, contextAttribute, buffer, buffer.Length);
            }
            finally
            {
                if (mustRelease)
                {
                    phContext.DangerousRelease();
                }
            }
        }
    }

    internal sealed class SafeFreeContextBuffer_SECURITY : SafeFreeContextBuffer
    {
        public SafeFreeContextBuffer_SECURITY() : base() { }

        protected override bool ReleaseHandle()
        {
            return Interop.SspiCli.FreeContextBuffer(handle) == 0;
        }
    }

    //
    // Implementation of handles required CertFreeCertificateContext
    //
#if DEBUG
    internal sealed class SafeFreeCertContext : DebugSafeHandle
    {
#else
    internal sealed class SafeFreeCertContext : SafeHandleZeroOrMinusOneIsInvalid
    {
#endif

        public SafeFreeCertContext() : base(true) { }

        // This must be ONLY called from this file.
        internal void Set(IntPtr value)
        {
            this.handle = value;
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypt32.CertFreeCertificateContext(handle);
            return true;
        }
    }

    //
    // Implementation of handles dependable on FreeCredentialsHandle
    //
#if DEBUG
    internal abstract class SafeFreeCredentials : DebugSafeHandle
    {
#else
    internal abstract class SafeFreeCredentials : SafeHandle
    {
#endif

        internal DateTime _expiry;
        internal Interop.SspiCli.CredHandle _handle;    //should be always used as by ref in PInvokes parameters

        protected SafeFreeCredentials() : base(IntPtr.Zero, true)
        {
            _handle = default;
            _expiry = DateTime.MaxValue;
        }

        public override bool IsInvalid
        {
            get { return IsClosed || _handle.IsZero; }
        }

        public DateTime Expiry => _expiry;

        public static unsafe int AcquireDefaultCredential(
            string package,
            Interop.SspiCli.CredentialUse intent,
            out SafeFreeCredentials outCredential)
        {
            int errorCode = -1;
            long timeStamp;

            outCredential = new SafeFreeCredential_SECURITY();

            errorCode = Interop.SspiCli.AcquireCredentialsHandleW(
                            null,
                            package,
                            (int)intent,
                            null,
                            IntPtr.Zero,
                            null,
                            null,
                            ref outCredential._handle,
                            out timeStamp);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"{nameof(Interop.SspiCli.AcquireCredentialsHandleW)} returns 0x{errorCode:x}, handle = {outCredential}");

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }

            return errorCode;
        }

        public static unsafe int AcquireCredentialsHandle(
            string package,
            Interop.SspiCli.CredentialUse intent,
            ref SafeSspiAuthDataHandle authdata,
            out SafeFreeCredentials outCredential)
        {
            outCredential = new SafeFreeCredential_SECURITY();
            int errorCode = Interop.SspiCli.AcquireCredentialsHandleW(
                            null,
                            package,
                            (int)intent,
                            null,
                            authdata,
                            null,
                            null,
                            ref outCredential._handle,
                            out _);

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }

            return errorCode;
        }

        public static unsafe int AcquireCredentialsHandle(
            string package,
            Interop.SspiCli.CredentialUse intent,
            Interop.SspiCli.SCHANNEL_CRED* authdata,
            out SafeFreeCredentials outCredential)
        {
            int errorCode = -1;

            outCredential = new SafeFreeCredential_SECURITY();

            errorCode = Interop.SspiCli.AcquireCredentialsHandleW(
                                null,
                                package,
                                (int)intent,
                                null,
                                authdata,
                                null,
                                null,
                                ref outCredential._handle,
                                out _);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"{nameof(Interop.SspiCli.AcquireCredentialsHandleW)} returns 0x{errorCode:x}, handle = {outCredential}");

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }

            return errorCode;
        }

        public static unsafe int AcquireCredentialsHandle(
            string package,
            Interop.SspiCli.CredentialUse intent,
            Interop.SspiCli.SCH_CREDENTIALS* authdata,
            out SafeFreeCredentials outCredential)
        {
            long timeStamp;

            outCredential = new SafeFreeCredential_SECURITY();

            int errorCode = Interop.SspiCli.AcquireCredentialsHandleW(
                                null,
                                package,
                                (int)intent,
                                null,
                                authdata,
                                null,
                                null,
                                ref outCredential._handle,
                                out timeStamp);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"{nameof(Interop.SspiCli.AcquireCredentialsHandleW)} returns 0x{errorCode:x}, handle = {outCredential}");

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }

            return errorCode;
        }

    }

    internal sealed class SafeFreeCredential_SECURITY : SafeFreeCredentials
    {
#pragma warning disable 0649
        // This is used only by SslStream but it is included elsewhere
        public X509Certificate? LocalCertificate;
#pragma warning restore 0649
        public SafeFreeCredential_SECURITY() : base() { }

        protected override bool ReleaseHandle()
        {
            LocalCertificate?.Dispose();
            return Interop.SspiCli.FreeCredentialsHandle(ref _handle) == 0;
        }
    }

    //
    // Implementation of handles that are dependent on DeleteSecurityContext
    //
#if DEBUG
    internal abstract partial class SafeDeleteContext : DebugSafeHandle
    {
#else
    internal abstract partial class SafeDeleteContext : SafeHandle
    {
#endif
        protected SafeFreeCredentials? _EffectiveCredential;

        //-------------------------------------------------------------------
        internal static unsafe int InitializeSecurityContext(
            ref SafeFreeCredentials? inCredentials,
            ref SafeDeleteSslContext? refContext,
            string? targetName,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            InputSecurityBuffers inSecBuffers,
            ref ProtocolToken outToken,
            ref Interop.SspiCli.ContextFlags outFlags)
        {
            ArgumentNullException.ThrowIfNull(inCredentials);

            Debug.Assert(inSecBuffers.Count <= 3);
            Interop.SspiCli.SecBufferDesc inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(inSecBuffers.Count);
            Interop.SspiCli.SecBufferDesc outSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(1);

            // Actually, this is returned in outFlags.
            bool isSspiAllocated = (inFlags & Interop.SspiCli.ContextFlags.AllocateMemory) != 0 ? true : false;

            int errorCode = -1;

            bool isContextAbsent = true;
            if (refContext != null)
            {
                isContextAbsent = refContext._handle.IsZero;
            }

            // Optional output buffer that may need to be freed.
            IntPtr outoutBuffer = IntPtr.Zero;
            try
            {
                Span<Interop.SspiCli.SecBuffer> inUnmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[3];

                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                fixed (void* pinnedToken0 = inSecBuffers._item0.Token)
                fixed (void* pinnedToken1 = inSecBuffers._item1.Token)
                fixed (void* pinnedToken2 = inSecBuffers._item2.Token)
                {
                    // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                    inSecurityBufferDescriptor.pBuffers = inUnmanagedBufferPtr;
                    // Updated pvBuffer with pinned address. UnmanagedToken takes precedence.
                    if (inSecBuffers.Count > 2)
                    {
                        inUnmanagedBuffer[2].BufferType = inSecBuffers._item2.Type;
                        if (inSecBuffers._item2.UnmanagedToken != null)
                        {
                            Debug.Assert(inSecBuffers._item2.Type == SecurityBufferType.SECBUFFER_CHANNEL_BINDINGS);
                            inUnmanagedBuffer[2].pvBuffer = (IntPtr)inSecBuffers._item2.UnmanagedToken.DangerousGetHandle();
                            inUnmanagedBuffer[2].cbBuffer = ((ChannelBinding)inSecBuffers._item2.UnmanagedToken).Size;
                        }
                        else
                        {
                            inUnmanagedBuffer[2].cbBuffer = inSecBuffers._item2.Token.Length;
                            inUnmanagedBuffer[2].pvBuffer = (IntPtr)pinnedToken2;
                        }

                    }

                    if (inSecBuffers.Count > 1)
                    {
                        inUnmanagedBuffer[1].BufferType = inSecBuffers._item1.Type;
                        if (inSecBuffers._item1.UnmanagedToken != null)
                        {
                            Debug.Assert(inSecBuffers._item1.Type == SecurityBufferType.SECBUFFER_CHANNEL_BINDINGS);
                            inUnmanagedBuffer[1].pvBuffer = (IntPtr)inSecBuffers._item1.UnmanagedToken.DangerousGetHandle();
                            inUnmanagedBuffer[1].cbBuffer = ((ChannelBinding)inSecBuffers._item1.UnmanagedToken).Size;
                        }
                        else
                        {
                            inUnmanagedBuffer[1].cbBuffer = inSecBuffers._item1.Token.Length;
                            inUnmanagedBuffer[1].pvBuffer = (IntPtr)pinnedToken1;
                        }
                    }

                    if (inSecBuffers.Count > 0)
                    {
                        inUnmanagedBuffer[0].BufferType = inSecBuffers._item0.Type;
                        if (inSecBuffers._item0.UnmanagedToken != null)
                        {
                            Debug.Assert(inSecBuffers._item0.Type == SecurityBufferType.SECBUFFER_CHANNEL_BINDINGS);
                            inUnmanagedBuffer[0].pvBuffer = (IntPtr)inSecBuffers._item0.UnmanagedToken.DangerousGetHandle();
                            inUnmanagedBuffer[0].cbBuffer = ((ChannelBinding)inSecBuffers._item0.UnmanagedToken).Size;
                        }
                        else
                        {
                            inUnmanagedBuffer[0].cbBuffer = inSecBuffers._item0.Token.Length;
                            inUnmanagedBuffer[0].pvBuffer = (IntPtr)pinnedToken0;
                        }
                    }

                    fixed (byte* pinnedOutBytes = outToken.Payload)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        Interop.SspiCli.SecBuffer outUnmanagedBuffer = default;
                        outSecurityBufferDescriptor.pBuffers = &outUnmanagedBuffer;
                        outUnmanagedBuffer.cbBuffer = outToken.Size;
                        outUnmanagedBuffer.BufferType = SecurityBufferType.SECBUFFER_TOKEN;
                        outUnmanagedBuffer.pvBuffer = outToken.Payload == null || outToken.Size == 0 ?
                            IntPtr.Zero :
                            (IntPtr)(pinnedOutBytes);

                        if (refContext == null || refContext.IsInvalid)
                        {
                            // Previous versions unconditionally built a new "refContext" here, but would pass
                            // incorrect arguments to InitializeSecurityContextW in cases where an "contextHandle" was
                            // already present and non-zero.
                            if (isContextAbsent)
                            {
                                refContext?.Dispose();
                                refContext = new SafeDeleteSslContext();
                            }
                        }

                        fixed (char* namePtr = targetName)
                        {
                            errorCode = MustRunInitializeSecurityContext(
                                            ref inCredentials,
                                            isContextAbsent,
                                            (byte*)namePtr,
                                            inFlags,
                                            endianness,
                                            &inSecurityBufferDescriptor,
                                            refContext!,
                                            ref outSecurityBufferDescriptor,
                                            ref outFlags,
                                            null);

                            if (isSspiAllocated)
                            {
                                outoutBuffer = outUnmanagedBuffer.pvBuffer;
                            }

                            // Get unmanaged buffer with index 0 as the only one passed into PInvoke.
                            if (isSspiAllocated)
                            {
                                if (outUnmanagedBuffer.cbBuffer > 0)
                                {
                                    outToken.EnsureAvailableSpace(outUnmanagedBuffer.cbBuffer);
                                    new Span<byte>((byte*)outUnmanagedBuffer.pvBuffer, outUnmanagedBuffer.cbBuffer).CopyTo(outToken.AvailableSpan);
                                }
                            }
                            outToken.Size = outUnmanagedBuffer.cbBuffer;

                            // In some cases schannel may not process all the given data.
                            // and it will return them back as SECBUFFER_EXTRA, expecting caller to
                            // feed them in again. Since we don't have good way how to flow the input back,
                            // we will try it again as separate call and we will return combined output from first and second try.
                            // That makes processing of outBuffer somewhat complicated.
                            if (inSecBuffers.Count > 1 && inUnmanagedBuffer[1].BufferType == SecurityBufferType.SECBUFFER_EXTRA && inSecBuffers._item1.Type == SecurityBufferType.SECBUFFER_EMPTY)
                            {
                                // OS function did not use all provided data and turned EMPTY to EXTRA
                                // https://docs.microsoft.com/en-us/windows/win32/secauthn/extra-buffers-returned-by-schannel

                                int leftover = inUnmanagedBuffer[1].cbBuffer;
                                int processed = inSecBuffers._item0.Token.Length - inUnmanagedBuffer[1].cbBuffer;

                                /* skip over processed data and try it again. */
                                inUnmanagedBuffer[0].cbBuffer = leftover;
                                inUnmanagedBuffer[0].pvBuffer = inUnmanagedBuffer[0].pvBuffer + processed;
                                inUnmanagedBuffer[1].BufferType = SecurityBufferType.SECBUFFER_EMPTY;
                                inUnmanagedBuffer[1].cbBuffer = 0;

                                outUnmanagedBuffer.cbBuffer = 0;

                                if (outoutBuffer != IntPtr.Zero)
                                {
                                    Interop.SspiCli.FreeContextBuffer(outoutBuffer);
                                    outoutBuffer = IntPtr.Zero;
                                }

                                errorCode = MustRunInitializeSecurityContext(
                                             ref inCredentials,
                                             isContextAbsent,
                                             (byte*)namePtr,
                                             inFlags,
                                             endianness,
                                             &inSecurityBufferDescriptor,
                                             refContext!,
                                             ref outSecurityBufferDescriptor,
                                             ref outFlags,
                                             null);

                                if (isSspiAllocated)
                                {
                                    outoutBuffer = outUnmanagedBuffer.pvBuffer;

                                    if (outUnmanagedBuffer.cbBuffer > 0)
                                    {
                                        outToken.EnsureAvailableSpace(outUnmanagedBuffer.cbBuffer);
                                        new Span<byte>((byte*)outUnmanagedBuffer.pvBuffer, outUnmanagedBuffer.cbBuffer).CopyTo(outToken.AvailableSpan);
                                        outToken.Size += outUnmanagedBuffer.cbBuffer;
                                    }
                                }

                                if (inUnmanagedBuffer[1].BufferType == SecurityBufferType.SECBUFFER_EXTRA)
                                {
                                    // we are left with unprocessed data again. fail with SEC_E_INCOMPLETE_MESSAGE hResult.
                                    errorCode = unchecked((int)0x80090318);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (outoutBuffer != IntPtr.Zero)
                {
                    Interop.SspiCli.FreeContextBuffer(outoutBuffer);
                }
            }

            return errorCode;
        }

        //
        // After PInvoke call the method will fix the handleTemplate.handle with the returned value.
        // The caller is responsible for creating a correct SafeFreeContextBuffer_XXX flavor or null can be passed if no handle is returned.
        //
        private static unsafe int MustRunInitializeSecurityContext(
            ref SafeFreeCredentials inCredentials,
            bool isContextAbsent,
            byte* targetName,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            Interop.SspiCli.SecBufferDesc* inputBuffer,
            SafeDeleteContext outContext,
            ref Interop.SspiCli.SecBufferDesc outputBuffer,
            ref Interop.SspiCli.ContextFlags attributes,
            SafeFreeContextBuffer? handleTemplate)
        {
            int errorCode = (int)Interop.SECURITY_STATUS.InvalidHandle;

            bool mustReleaseCredentials = false;
            bool mustReleaseOutContext = false;
            try
            {
                inCredentials.DangerousAddRef(ref mustReleaseCredentials);
                outContext.DangerousAddRef(ref mustReleaseOutContext);

                Interop.SspiCli.CredHandle credentialHandle = inCredentials._handle;

                long timeStamp;

                // Now that "outContext" (or "refContext" by the caller) references an actual handle (and cannot
                // be closed until it is released below), point "inContextPtr" to its embedded handle (or
                // null if the embedded handle has not yet been initialized).
                Interop.SspiCli.CredHandle contextHandle = outContext._handle;
                void* inContextPtr = contextHandle.IsZero ? null : &contextHandle;

                // The "isContextAbsent" supplied by the caller is generally correct but was computed without proper
                // synchronization. Rewrite the indicator now that the final "inContext" is known, update if necessary.
                isContextAbsent = (inContextPtr == null);

                errorCode = Interop.SspiCli.InitializeSecurityContextW(
                                ref credentialHandle,
                                inContextPtr,
                                targetName,
                                inFlags,
                                0,
                                endianness,
                                inputBuffer,
                                0,
                                ref outContext._handle,
                                ref outputBuffer,
                                ref attributes,
                                out timeStamp);
            }
            finally
            {
                //
                // When a credential handle is first associated with the context we keep credential
                // ref count bumped up to ensure ordered finalization.
                // If the credential handle has been changed we de-ref the old one and associate the
                //  context with the new cred handle but only if the call was successful.
                if (outContext._EffectiveCredential != inCredentials && (errorCode & 0x80000000) == 0)
                {
                    // Disassociate the previous credential handle
                    outContext._EffectiveCredential?.DangerousRelease();
                    outContext._EffectiveCredential = inCredentials;
                }
                else if (mustReleaseCredentials)
                {
                    inCredentials.DangerousRelease();
                }

                if (mustReleaseOutContext)
                {
                    outContext.DangerousRelease();
                }
            }

            // The idea is that SSPI has allocated a block and filled up outUnmanagedBuffer+8 slot with the pointer.
            if (handleTemplate != null)
            {
                //ATTN: on 64 BIT that is still +8 cause of 2* c++ unsigned long == 8 bytes
                handleTemplate.Set(((Interop.SspiCli.SecBuffer*)outputBuffer.pBuffers)->pvBuffer);
                if (handleTemplate.IsInvalid)
                {
                    handleTemplate.SetHandleAsInvalid();
                }
            }

            if (isContextAbsent && (errorCode & 0x80000000) != 0)
            {
                // an error on the first call, need to set the out handle to invalid value
                outContext._handle.SetToInvalid();
            }

            return errorCode;
        }

        //-------------------------------------------------------------------
        internal static unsafe int AcceptSecurityContext(
            ref SafeFreeCredentials? inCredentials,
            ref SafeDeleteSslContext? refContext,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            InputSecurityBuffers inSecBuffers,
            ref ProtocolToken outToken,
            ref Interop.SspiCli.ContextFlags outFlags)
        {
            ArgumentNullException.ThrowIfNull(inCredentials);

            Debug.Assert(inSecBuffers.Count <= 3);
            Interop.SspiCli.SecBufferDesc inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(inSecBuffers.Count);
            Interop.SspiCli.SecBufferDesc outSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(count: 2);

            // Actually, this is returned in outFlags.
            bool isSspiAllocated = (inFlags & Interop.SspiCli.ContextFlags.AllocateMemory) != 0 ? true : false;

            int errorCode = -1;

            bool isContextAbsent = true;
            if (refContext != null)
            {
                isContextAbsent = refContext._handle.IsZero;
            }

            Span<Interop.SspiCli.SecBuffer> outUnmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[2];
            outUnmanagedBuffer[1].pvBuffer = IntPtr.Zero;
            try
            {
                // Allocate always maximum to allow better code optimization.
                Span<Interop.SspiCli.SecBuffer> inUnmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[3];

                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                fixed (void* outUnmanagedBufferPtr = outUnmanagedBuffer)
                fixed (void* pinnedToken0 = inSecBuffers._item0.Token)
                fixed (void* pinnedToken1 = inSecBuffers._item1.Token)
                fixed (void* pinnedToken2 = inSecBuffers._item2.Token)
                {
                    inSecurityBufferDescriptor.pBuffers = inUnmanagedBufferPtr;
                    // Updated pvBuffer with pinned address. UnmanagedToken takes precedence.
                    if (inSecBuffers.Count > 2)
                    {
                        inUnmanagedBuffer[2].BufferType = inSecBuffers._item2.Type;
                        if (inSecBuffers._item2.UnmanagedToken != null)
                        {
                            Debug.Assert(inSecBuffers._item2.Type == SecurityBufferType.SECBUFFER_CHANNEL_BINDINGS);
                            inUnmanagedBuffer[2].pvBuffer = (IntPtr)inSecBuffers._item2.UnmanagedToken.DangerousGetHandle();
                            inUnmanagedBuffer[2].cbBuffer = ((ChannelBinding)inSecBuffers._item2.UnmanagedToken).Size;
                        }
                        else
                        {
                            inUnmanagedBuffer[2].cbBuffer = inSecBuffers._item2.Token.Length;
                            inUnmanagedBuffer[2].pvBuffer = (IntPtr)pinnedToken2;
                        }

                    }

                    if (inSecBuffers.Count > 1)
                    {
                        inUnmanagedBuffer[1].BufferType = inSecBuffers._item1.Type;
                        if (inSecBuffers._item1.UnmanagedToken != null)
                        {
                            Debug.Assert(inSecBuffers._item1.Type == SecurityBufferType.SECBUFFER_CHANNEL_BINDINGS);
                            inUnmanagedBuffer[1].pvBuffer = (IntPtr)inSecBuffers._item1.UnmanagedToken.DangerousGetHandle();
                            inUnmanagedBuffer[1].cbBuffer = ((ChannelBinding)inSecBuffers._item1.UnmanagedToken).Size;
                        }
                        else
                        {
                            inUnmanagedBuffer[1].cbBuffer = inSecBuffers._item1.Token.Length;
                            inUnmanagedBuffer[1].pvBuffer = (IntPtr)pinnedToken1;
                        }
                    }

                    if (inSecBuffers.Count > 0)
                    {
                        inUnmanagedBuffer[0].BufferType = inSecBuffers._item0.Type;
                        if (inSecBuffers._item0.UnmanagedToken != null)
                        {
                            Debug.Assert(inSecBuffers._item0.Type == SecurityBufferType.SECBUFFER_CHANNEL_BINDINGS);
                            inUnmanagedBuffer[0].pvBuffer = (IntPtr)inSecBuffers._item0.UnmanagedToken.DangerousGetHandle();
                            inUnmanagedBuffer[0].cbBuffer = ((ChannelBinding)inSecBuffers._item0.UnmanagedToken).Size;
                        }
                        else
                        {
                            inUnmanagedBuffer[0].cbBuffer = inSecBuffers._item0.Token.Length;
                            inUnmanagedBuffer[0].pvBuffer = (IntPtr)pinnedToken0;
                        }
                    }

                    fixed (byte* pinnedOutBytes = outToken.Payload)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        outSecurityBufferDescriptor.pBuffers = outUnmanagedBufferPtr;

                        // Copy the SecurityBuffer content into unmanaged place holder.
                        outUnmanagedBuffer[0].cbBuffer = outToken.Size;
                        outUnmanagedBuffer[0].BufferType = SecurityBufferType.SECBUFFER_TOKEN;
                        outUnmanagedBuffer[0].pvBuffer = outToken.Payload == null || outToken.Payload.Length == 0 ?
                            IntPtr.Zero :
                            (IntPtr)(pinnedOutBytes);

                        outUnmanagedBuffer[1].cbBuffer = 0;
                        outUnmanagedBuffer[1].BufferType = SecurityBufferType.SECBUFFER_ALERT;

                        if (refContext == null || refContext.IsInvalid)
                        {
                            // Previous versions unconditionally built a new "refContext" here, but would pass
                            // incorrect arguments to AcceptSecurityContext in cases where an "contextHandle" was
                            // already present and non-zero.
                            if (isContextAbsent)
                                refContext = new SafeDeleteSslContext();
                        }

                        errorCode = MustRunAcceptSecurityContext_SECURITY(
                                        ref inCredentials,
                                        isContextAbsent,
                                        &inSecurityBufferDescriptor,
                                        inFlags,
                                        endianness,
                                        refContext!,
                                        ref outSecurityBufferDescriptor,
                                        ref outFlags,
                                        null);

                        // No data written out but there is Alert
                        int index = outUnmanagedBuffer[0].cbBuffer == 0 && outUnmanagedBuffer[1].cbBuffer > 0 ? 1 : 0;

                        int length = outUnmanagedBuffer[index].cbBuffer;
                        if (isSspiAllocated && length > 0)
                        {
                            outToken.EnsureAvailableSpace(length);
                            new Span<byte>((byte*)outUnmanagedBuffer[index].pvBuffer, length).CopyTo(outToken.AvailableSpan);
                        }
                        outToken.Size = length;

                        if (inSecBuffers.Count > 1 && inUnmanagedBuffer[1].BufferType == SecurityBufferType.SECBUFFER_EXTRA && inSecBuffers._item1.Type == SecurityBufferType.SECBUFFER_EMPTY)
                        {
                            // OS function did not use all provided data and turned EMPTY to EXTRA
                            // https://docs.microsoft.com/en-us/windows/win32/secauthn/extra-buffers-returned-by-schannel

                            int leftover = inUnmanagedBuffer[1].cbBuffer;
                            int processed = inSecBuffers._item0.Token.Length - inUnmanagedBuffer[1].cbBuffer;

                            /* skip over processed data and try it again. */
                            inUnmanagedBuffer[0].cbBuffer = leftover;
                            inUnmanagedBuffer[0].pvBuffer = inUnmanagedBuffer[0].pvBuffer + processed;
                            inUnmanagedBuffer[1].BufferType = SecurityBufferType.SECBUFFER_EMPTY;
                            inUnmanagedBuffer[1].cbBuffer = 0;

                            outUnmanagedBuffer[0].cbBuffer = 0;
                            if (isSspiAllocated && outUnmanagedBuffer[0].pvBuffer != IntPtr.Zero)
                            {
                                Interop.SspiCli.FreeContextBuffer(outUnmanagedBuffer[0].pvBuffer);
                                outUnmanagedBuffer[0].pvBuffer = IntPtr.Zero;
                            }

                            errorCode = MustRunAcceptSecurityContext_SECURITY(
                                        ref inCredentials,
                                        isContextAbsent,
                                        &inSecurityBufferDescriptor,
                                        inFlags,
                                        endianness,
                                        refContext!,
                                        ref outSecurityBufferDescriptor,
                                        ref outFlags,
                                        null);

                            index = outUnmanagedBuffer[0].cbBuffer == 0 && outUnmanagedBuffer[1].cbBuffer > 0 ? 1 : 0;
                            if (outUnmanagedBuffer[index].cbBuffer > 0)
                            {
                                outToken.EnsureAvailableSpace(outUnmanagedBuffer[index].cbBuffer);
                                new Span<byte>((byte*)outUnmanagedBuffer[index].pvBuffer, outUnmanagedBuffer[index].cbBuffer).CopyTo(outToken.AvailableSpan);
                                outToken.Size += outUnmanagedBuffer[index].cbBuffer;
                            }

                            if (inUnmanagedBuffer[1].BufferType == SecurityBufferType.SECBUFFER_EXTRA)
                            {
                                // we are left with unprocessed data again. fail with SEC_E_INCOMPLETE_MESSAGE hResult.
                                errorCode = unchecked((int)0x80090318);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (isSspiAllocated && outUnmanagedBuffer[0].pvBuffer != IntPtr.Zero)
                {
                    Interop.SspiCli.FreeContextBuffer(outUnmanagedBuffer[0].pvBuffer);
                }

                if (outUnmanagedBuffer[1].pvBuffer != IntPtr.Zero)
                {
                    Interop.SspiCli.FreeContextBuffer(outUnmanagedBuffer[1].pvBuffer);
                }
            }

            return errorCode;
        }

        //
        // After PInvoke call the method will fix the handleTemplate.handle with the returned value.
        // The caller is responsible for creating a correct SafeFreeContextBuffer_XXX flavor or null can be passed if no handle is returned.
        //
        private static unsafe int MustRunAcceptSecurityContext_SECURITY(
            ref SafeFreeCredentials inCredentials,
            bool isContextAbsent,
            Interop.SspiCli.SecBufferDesc* inputBuffer,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            SafeDeleteContext outContext,
            ref Interop.SspiCli.SecBufferDesc outputBuffer,
            ref Interop.SspiCli.ContextFlags outFlags,
            SafeFreeContextBuffer? handleTemplate)
        {
            int errorCode = (int)Interop.SECURITY_STATUS.InvalidHandle;

            bool mustReleaseCredentials = false;
            bool mustReleaseOutContext = false;
            // Run the body of this method as a non-interruptible block.
            try
            {
                inCredentials.DangerousAddRef(ref mustReleaseCredentials);
                outContext.DangerousAddRef(ref mustReleaseOutContext);

                Interop.SspiCli.CredHandle credentialHandle = inCredentials._handle;
                long timeStamp;

                // Now that "outContext" (or "refContext" by the caller) references an actual handle (and cannot
                // be closed until it is released below), point "inContextPtr" to its embedded handle (or
                // null if the embedded handle has not yet been initialized).
                Interop.SspiCli.CredHandle contextHandle = outContext._handle;
                void* inContextPtr = contextHandle.IsZero ? null : &contextHandle;

                // The "isContextAbsent" supplied by the caller is generally correct but was computed without proper
                // synchronization. Rewrite the indicator now that the final "inContext" is known, update if necessary.
                isContextAbsent = (inContextPtr == null);

                errorCode = Interop.SspiCli.AcceptSecurityContext(
                                ref credentialHandle,
                                inContextPtr,
                                inputBuffer,
                                inFlags,
                                endianness,
                                ref outContext._handle,
                                ref outputBuffer,
                                ref outFlags,
                                out timeStamp);
            }
            finally
            {
                //
                // When a credential handle is first associated with the context we keep credential
                // ref count bumped up to ensure ordered finalization.
                // If the credential handle has been changed we de-ref the old one and associate the
                //  context with the new cred handle but only if the call was successful.
                if (outContext._EffectiveCredential != inCredentials && (errorCode & 0x80000000) == 0)
                {
                    // Disassociate the previous credential handle.
                    outContext._EffectiveCredential?.DangerousRelease();
                    outContext._EffectiveCredential = inCredentials;
                }
                else if (mustReleaseCredentials)
                {
                    inCredentials.DangerousRelease();
                }

                if (mustReleaseOutContext)
                {
                    outContext.DangerousRelease();
                }
            }

            // The idea is that SSPI has allocated a block and filled up outUnmanagedBuffer+8 slot with the pointer.
            if (handleTemplate != null)
            {
                //ATTN: on 64 BIT that is still +8 cause of 2* c++ unsigned long == 8 bytes.
                handleTemplate.Set(((Interop.SspiCli.SecBuffer*)outputBuffer.pBuffers)->pvBuffer);
                if (handleTemplate.IsInvalid)
                {
                    handleTemplate.SetHandleAsInvalid();
                }
            }

            if (isContextAbsent && (errorCode & 0x80000000) != 0)
            {
                // An error on the first call, need to set the out handle to invalid value.
                outContext._handle.SetToInvalid();
            }

            return errorCode;
        }

        internal static unsafe int CompleteAuthToken(
            ref SafeDeleteSslContext? refContext,
            in InputSecurityBuffer inSecBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"refContext = {refContext}");

            var inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(1);
            int errorCode = (int)Interop.SECURITY_STATUS.InvalidHandle;

            Interop.SspiCli.SecBuffer inUnmanagedBuffer = default;
            inSecurityBufferDescriptor.pBuffers = &inUnmanagedBuffer;
            fixed (byte* pinnedToken = inSecBuffer.Token)
            {
                Debug.Assert(inSecBuffer.UnmanagedToken != null);
                inUnmanagedBuffer.cbBuffer = inSecBuffer.Token.Length;
                inUnmanagedBuffer.BufferType = inSecBuffer.Type;
                inUnmanagedBuffer.pvBuffer =
                    inSecBuffer.Token.IsEmpty ? IntPtr.Zero : (IntPtr)pinnedToken;

                Interop.SspiCli.CredHandle contextHandle = refContext != null ? refContext._handle : default;
                if (refContext == null || refContext.IsInvalid)
                {
                    // Previous versions unconditionally built a new "refContext" here, but would pass
                    // incorrect arguments to CompleteAuthToken in cases where a nonzero "contextHandle" was
                    // already present. In these cases, allow the "refContext" to flow through unmodified
                    // (which will generate an ObjectDisposedException below). In all other cases, continue to
                    // build a new "refContext" in an attempt to maximize compat.
                    if (contextHandle.IsZero)
                    {
                        refContext = new SafeDeleteSslContext();
                    }
                }

                bool gotRef = false;
                try
                {
                    refContext!.DangerousAddRef(ref gotRef);
                    errorCode = Interop.SspiCli.CompleteAuthToken(contextHandle.IsZero ? null : &contextHandle, ref inSecurityBufferDescriptor);
                }
                finally
                {
                    if (gotRef)
                    {
                        refContext!.DangerousRelease();
                    }
                }
            }

            return errorCode;
        }

        internal static unsafe int ApplyControlToken(
            ref SafeDeleteSslContext? refContext,
            in SecurityBuffer inSecBuffer)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"refContext = {refContext}, inSecBuffer = {inSecBuffer}");

            int errorCode = (int)Interop.SECURITY_STATUS.InvalidHandle;

            // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
            fixed (byte* pinnedInSecBufferToken = inSecBuffer.token)
            {
                var inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(1);
                Interop.SspiCli.SecBuffer inUnmanagedBuffer = default;
                inSecurityBufferDescriptor.pBuffers = &inUnmanagedBuffer;
                inUnmanagedBuffer.cbBuffer = inSecBuffer.size;
                inUnmanagedBuffer.BufferType = inSecBuffer.type;

                // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                inUnmanagedBuffer.pvBuffer =
                    inSecBuffer.unmanagedToken != null ? inSecBuffer.unmanagedToken.DangerousGetHandle() :
                    inSecBuffer.token == null || inSecBuffer.token.Length == 0 ? IntPtr.Zero :
                    (IntPtr)(pinnedInSecBufferToken + inSecBuffer.offset);

                Interop.SspiCli.CredHandle contextHandle = refContext != null ? refContext._handle : default;

                if (refContext == null || refContext.IsInvalid)
                {
                    // Previous versions unconditionally built a new "refContext" here, but would pass
                    // incorrect arguments to ApplyControlToken in cases where a nonzero "contextHandle" was
                    // already present. In these cases, allow the "refContext" to flow through unmodified
                    // (which will generate an ObjectDisposedException below). In all other cases, continue to
                    // build a new "refContext" in an attempt to maximize compat.
                    if (contextHandle.IsZero)
                    {
                        refContext = new SafeDeleteSslContext();
                    }
                }

                bool gotRef = false;
                try
                {
                    refContext!.DangerousAddRef(ref gotRef);
                    errorCode = Interop.SspiCli.ApplyControlToken(contextHandle.IsZero ? null : &contextHandle, ref inSecurityBufferDescriptor);
                }
                finally
                {
                    if (gotRef)
                    {
                        refContext!.DangerousRelease();
                    }
                }
            }

            return errorCode;
        }
    }

    internal sealed class SafeDeleteSslContext : SafeDeleteContext
    {
        public SafeDeleteSslContext() : base() { }

        protected override bool ReleaseHandle()
        {
            this._EffectiveCredential?.DangerousRelease();
            return Interop.SspiCli.DeleteSecurityContext(ref _handle) == 0;
        }
    }

    // Based on SafeFreeContextBuffer.
    internal abstract class SafeFreeContextBufferChannelBinding : ChannelBinding
    {
        private int _size;

        public override int Size
        {
            get { return _size; }
        }

        public override bool IsInvalid
        {
            get { return handle == new IntPtr(0) || handle == new IntPtr(-1); }
        }

        internal unsafe void Set(IntPtr value)
        {
            this.handle = value;
        }

        internal static SafeFreeContextBufferChannelBinding CreateEmptyHandle()
        {
            return new SafeFreeContextBufferChannelBinding_SECURITY();
        }

        public static unsafe int QueryContextChannelBinding(SafeDeleteContext phContext, Interop.SspiCli.ContextAttribute contextAttribute, SecPkgContext_Bindings* buffer, SafeFreeContextBufferChannelBinding refHandle)
        {
            int status = (int)Interop.SECURITY_STATUS.InvalidHandle;

            // SCHANNEL only supports SECPKG_ATTR_ENDPOINT_BINDINGS and SECPKG_ATTR_UNIQUE_BINDINGS which
            // map to our enum ChannelBindingKind.Endpoint and ChannelBindingKind.Unique.
            if (contextAttribute != Interop.SspiCli.ContextAttribute.SECPKG_ATTR_ENDPOINT_BINDINGS &&
                contextAttribute != Interop.SspiCli.ContextAttribute.SECPKG_ATTR_UNIQUE_BINDINGS)
            {
                return status;
            }

            bool refAdded = false;
            try
            {
                phContext.DangerousAddRef(ref refAdded);
                status = Interop.SspiCli.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
            }
            finally
            {
                if (refAdded)
                {
                    phContext.DangerousRelease();
                }
            }

            if (status == 0 && refHandle != null)
            {
                refHandle.Set((*buffer).Bindings);
                refHandle._size = (*buffer).BindingsLength;
            }

            if (status != 0)
            {
                refHandle?.SetHandleAsInvalid();
            }

            return status;
        }

        public override string? ToString()
        {
            if (IsInvalid)
            {
                return null;
            }

            var bytes = new byte[_size];
            Marshal.Copy(handle, bytes, 0, bytes.Length);
            return BitConverter.ToString(bytes).Replace('-', ' ');
        }
    }

    internal sealed class SafeFreeContextBufferChannelBinding_SECURITY : SafeFreeContextBufferChannelBinding
    {
        protected override bool ReleaseHandle()
        {
            return Interop.SspiCli.FreeContextBuffer(handle) == 0;
        }
    }
}
