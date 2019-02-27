// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
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
            int res = -1;
            SafeFreeContextBuffer_SECURITY pkgArray_SECURITY = null;
            res = Interop.SspiCli.EnumerateSecurityPackagesW(out pkgnum, out pkgArray_SECURITY);
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
        public static unsafe int QueryContextAttributes(SafeDeleteContext phContext, Interop.SspiCli.ContextAttribute contextAttribute, byte* buffer, SafeHandle refHandle)
        {
            int status = (int)Interop.SECURITY_STATUS.InvalidHandle;

            try
            {
                bool ignore = false;
                phContext.DangerousAddRef(ref ignore);
                status = Interop.SspiCli.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
            }
            finally
            {
                phContext.DangerousRelease();
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
            try
            {
                bool ignore = false;
                phContext.DangerousAddRef(ref ignore);
                return Interop.SspiCli.SetContextAttributesW(ref phContext._handle, contextAttribute, buffer, buffer.Length);
            }
            finally
            {
                phContext.DangerousRelease();
            }
        }
    }

    internal sealed class SafeFreeContextBuffer_SECURITY : SafeFreeContextBuffer
    {
        internal SafeFreeContextBuffer_SECURITY() : base() { }

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

        internal SafeFreeCertContext() : base(true) { }

        // This must be ONLY called from this file.
        internal void Set(IntPtr value)
        {
            this.handle = value;
        }

        private const uint CRYPT_ACQUIRE_SILENT_FLAG = 0x00000040;

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

        internal Interop.SspiCli.CredHandle _handle;    //should be always used as by ref in PInvokes parameters

        protected SafeFreeCredentials() : base(IntPtr.Zero, true)
        {
            _handle = new Interop.SspiCli.CredHandle();
        }

#if TRACE_VERBOSE
        public override string ToString()
        {
            return "0x" + _handle.ToString();
        }
#endif

        public override bool IsInvalid
        {
            get { return IsClosed || _handle.IsZero; }
        }

#if DEBUG
        public new IntPtr DangerousGetHandle()
        {
            Debug.Fail("This method should never be called for this type");
            throw NotImplemented.ByDesign;
        }
#endif

        public static unsafe int AcquireDefaultCredential(
            string package,
            Interop.SspiCli.CredentialUse intent,
            out SafeFreeCredentials outCredential)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, package, intent);

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

#if TRACE_VERBOSE
            if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"{nameof(Interop.SspiCli.AcquireCredentialsHandleW)} returns 0x{errorCode:x}, handle = {outCredential}");
#endif

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
            int errorCode = -1;
            long timeStamp;

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
                            out timeStamp);

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }

            return errorCode;
        }

        public static unsafe int AcquireCredentialsHandle(
            string package,
            Interop.SspiCli.CredentialUse intent,
            ref Interop.SspiCli.SCHANNEL_CRED authdata,
            out SafeFreeCredentials outCredential)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, package, intent, authdata);

            int errorCode = -1;
            long timeStamp;


            // If there is a certificate, wrap it into an array.
            // Not threadsafe.
            IntPtr copiedPtr = authdata.paCred;
            try
            {
                IntPtr certArrayPtr = new IntPtr(&copiedPtr);
                if (copiedPtr != IntPtr.Zero)
                {
                    authdata.paCred = certArrayPtr;
                }

                outCredential = new SafeFreeCredential_SECURITY();

                errorCode = Interop.SspiCli.AcquireCredentialsHandleW(
                                null,
                                package,
                                (int)intent,
                                null,
                                ref authdata,
                                null,
                                null,
                                ref outCredential._handle,
                                out timeStamp);
            }
            finally
            {
                authdata.paCred = copiedPtr;
            }

#if TRACE_VERBOSE
            if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"{nameof(Interop.SspiCli.AcquireCredentialsHandleW)} returns 0x{errorCode:x}, handle = {outCredential}");
#endif

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }

            return errorCode;
        }
    }

    //
    // This is a class holding a Credential handle reference, used for static handles cache
    //
#if DEBUG
    internal sealed class SafeCredentialReference : DebugCriticalHandleMinusOneIsInvalid
    {
#else
    internal sealed class SafeCredentialReference : CriticalHandleMinusOneIsInvalid
    {
#endif

        //
        // Static cache will return the target handle if found the reference in the table.
        //
        internal SafeFreeCredentials Target;

        internal static SafeCredentialReference CreateReference(SafeFreeCredentials target)
        {
            SafeCredentialReference result = new SafeCredentialReference(target);
            if (result.IsInvalid)
            {
                return null;
            }

            return result;
        }
        private SafeCredentialReference(SafeFreeCredentials target) : base()
        {
            // Bumps up the refcount on Target to signify that target handle is statically cached so
            // its dispose should be postponed
            bool ignore = false;
            target.DangerousAddRef(ref ignore);
            Target = target;
            SetHandle(new IntPtr(0));   // make this handle valid
        }

        protected override bool ReleaseHandle()
        {
            SafeFreeCredentials target = Target;
            target?.DangerousRelease();
            Target = null;
            return true;
        }
    }

    internal sealed class SafeFreeCredential_SECURITY : SafeFreeCredentials
    {
        public SafeFreeCredential_SECURITY() : base() { }

        protected override bool ReleaseHandle()
        {
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
        private const string dummyStr = " ";
        private static readonly IdnMapping s_idnMapping = new IdnMapping();

        protected SafeFreeCredentials _EffectiveCredential;

        //-------------------------------------------------------------------
        internal static unsafe int InitializeSecurityContext(
            ref SafeFreeCredentials inCredentials,
            ref SafeDeleteContext refContext,
            string targetName,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            ReadOnlySpan<SecurityBuffer> inSecBuffers,
            ref SecurityBuffer outSecBuffer,
            ref Interop.SspiCli.ContextFlags outFlags)
        {
#if TRACE_VERBOSE
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, $"credential:{inCredentials}, crefContext:{refContext}, targetName:{targetName}, inFlags:{inFlags}, endianness:{endianness}");
                NetEventSource.Info(null, $"inSecBuffers.Length = {inSecBuffers.Length}");
            }
#endif

            if (inCredentials == null)
            {
                throw new ArgumentNullException(nameof(inCredentials));
            }

            Interop.SspiCli.SecBufferDesc inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(inSecBuffers.Length);
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
            SafeFreeContextBuffer outFreeContextBuffer = null;
            try
            {
                Span<Interop.SspiCli.SecBuffer> inUnmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[inSecurityBufferDescriptor.cBuffers];
                inUnmanagedBuffer.Clear();

                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                fixed (void* pinnedToken0 = inSecBuffers.Length > 0 ? inSecBuffers[0].token : null)
                fixed (void* pinnedToken1 = inSecBuffers.Length > 1 ? inSecBuffers[1].token : null)
                fixed (void* pinnedToken2 = inSecBuffers.Length > 2 ? inSecBuffers[2].token : null) // pin all buffers, even if null or not used, to avoid needing to allocate GCHandles
                {
                    Debug.Assert(inSecBuffers.Length <= 3);

                    // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                    inSecurityBufferDescriptor.pBuffers = inUnmanagedBufferPtr;
                    for (int index = 0; index < inSecurityBufferDescriptor.cBuffers; ++index)
                    {
                        ref readonly SecurityBuffer securityBuffer = ref inSecBuffers[index];

                        // Copy the SecurityBuffer content into unmanaged place holder.
                        inUnmanagedBuffer[index].cbBuffer = securityBuffer.size;
                        inUnmanagedBuffer[index].BufferType = securityBuffer.type;

                        // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                        inUnmanagedBuffer[index].pvBuffer =
                            securityBuffer.unmanagedToken != null ? securityBuffer.unmanagedToken.DangerousGetHandle() :
                            securityBuffer.token == null || securityBuffer.token.Length == 0 ? IntPtr.Zero :
                            Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
#if TRACE_VERBOSE
                        if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"SecBuffer: cbBuffer:{securityBuffer.size} BufferType:{securityBuffer.type}");
#endif
                    }

                    fixed (byte* pinnedOutBytes = outSecBuffer.token)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        Interop.SspiCli.SecBuffer outUnmanagedBuffer = default;
                        outSecurityBufferDescriptor.pBuffers = &outUnmanagedBuffer;
                        outUnmanagedBuffer.cbBuffer = outSecBuffer.size;
                        outUnmanagedBuffer.BufferType = outSecBuffer.type;
                        outUnmanagedBuffer.pvBuffer = outSecBuffer.token == null || outSecBuffer.token.Length == 0 ?
                            IntPtr.Zero :
                            (IntPtr)(pinnedOutBytes + outSecBuffer.offset);

                        if (isSspiAllocated)
                        {
                            outFreeContextBuffer = SafeFreeContextBuffer.CreateEmptyHandle();
                        }

                        if (refContext == null || refContext.IsInvalid)
                        {
                            // Previous versions unconditionally built a new "refContext" here, but would pass
                            // incorrect arguments to InitializeSecurityContextW in cases where an "contextHandle" was
                            // already present and non-zero.
                            if (isContextAbsent)
                                refContext = new SafeDeleteContext_SECURITY();
                        }

                        if (targetName == null || targetName.Length == 0)
                        {
                            targetName = dummyStr;
                        }

                        string punyCode = s_idnMapping.GetAscii(targetName);
                        fixed (char* namePtr = punyCode)
                        {
                            errorCode = MustRunInitializeSecurityContext(
                                            ref inCredentials,
                                            isContextAbsent,
                                            (byte*)(((object)targetName == (object)dummyStr) ? null : namePtr),
                                            inFlags,
                                            endianness,
                                            &inSecurityBufferDescriptor,
                                            refContext,
                                            ref outSecurityBufferDescriptor,
                                            ref outFlags,
                                            outFreeContextBuffer);
                        }

                        if (NetEventSource.IsEnabled) NetEventSource.Info(null, "Marshalling OUT buffer");

                        // Get unmanaged buffer with index 0 as the only one passed into PInvoke.
                        outSecBuffer.size = outUnmanagedBuffer.cbBuffer;
                        outSecBuffer.type = outUnmanagedBuffer.BufferType;
                        outSecBuffer.token = outSecBuffer.size > 0 ?
                            new Span<byte>((byte*)outUnmanagedBuffer.pvBuffer, outUnmanagedBuffer.cbBuffer).ToArray() :
                            null;
                    }
                }
            }
            finally
            {
                outFreeContextBuffer?.Dispose();
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"errorCode:0x{errorCode:x8}, refContext:{refContext}");
            return errorCode;
        }

        //
        // After PInvoke call the method will fix the handleTemplate.handle with the returned value.
        // The caller is responsible for creating a correct SafeFreeContextBuffer_XXX flavor or null can be passed if no handle is returned.
        //
        private static unsafe int MustRunInitializeSecurityContext(
            ref SafeFreeCredentials inCredentials,
            bool  isContextAbsent,
            byte* targetName,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            Interop.SspiCli.SecBufferDesc* inputBuffer,
            SafeDeleteContext outContext,
            ref Interop.SspiCli.SecBufferDesc outputBuffer,
            ref Interop.SspiCli.ContextFlags attributes,
            SafeFreeContextBuffer handleTemplate)
        {
            int errorCode = (int)Interop.SECURITY_STATUS.InvalidHandle;

            try
            {
                bool ignore = false;
                inCredentials.DangerousAddRef(ref ignore);
                outContext.DangerousAddRef(ref ignore);

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
                else
                {
                    inCredentials.DangerousRelease();
                }

                outContext.DangerousRelease();
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
            ref SafeFreeCredentials inCredentials,
            ref SafeDeleteContext refContext,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            ReadOnlySpan<SecurityBuffer> inSecBuffers,
            ref SecurityBuffer outSecBuffer,
            ref Interop.SspiCli.ContextFlags outFlags)
        {
#if TRACE_VERBOSE
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, $"credential={inCredentials}, refContext={refContext}, inFlags={inFlags}");
                NetEventSource.Info(null, $"inSecBuffers.Length = {inSecBuffers.Length}");
            }
#endif

            if (inCredentials == null)
            {
                throw new ArgumentNullException(nameof(inCredentials));
            }

            Interop.SspiCli.SecBufferDesc inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(inSecBuffers.Length);
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
            SafeFreeContextBuffer outFreeContextBuffer = null;
            try
            {
                Span<Interop.SspiCli.SecBuffer> inUnmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[inSecurityBufferDescriptor.cBuffers];
                inUnmanagedBuffer.Clear();

                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                fixed (void* pinnedToken0 = inSecBuffers.Length > 0 ? inSecBuffers[0].token : null)
                fixed (void* pinnedToken1 = inSecBuffers.Length > 1 ? inSecBuffers[1].token : null)
                fixed (void* pinnedToken2 = inSecBuffers.Length > 2 ? inSecBuffers[2].token : null) // pin all buffers, even if null or not used, to avoid needing to allocate GCHandles
                {
                    Debug.Assert(inSecBuffers.Length <= 3);

                    // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                    inSecurityBufferDescriptor.pBuffers = inUnmanagedBufferPtr;
                    for (int index = 0; index < inSecurityBufferDescriptor.cBuffers; ++index)
                    {
                        ref readonly SecurityBuffer securityBuffer = ref inSecBuffers[index];

                        // Copy the SecurityBuffer content into unmanaged place holder.
                        inUnmanagedBuffer[index].cbBuffer = securityBuffer.size;
                        inUnmanagedBuffer[index].BufferType = securityBuffer.type;

                        // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                        inUnmanagedBuffer[index].pvBuffer =
                            securityBuffer.unmanagedToken != null ? securityBuffer.unmanagedToken.DangerousGetHandle() :
                            securityBuffer.token == null || securityBuffer.token.Length == 0 ? IntPtr.Zero :
                            Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
#if TRACE_VERBOSE
                        if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"SecBuffer: cbBuffer:{securityBuffer.size} BufferType:{securityBuffer.type}");
#endif
                    }

                    fixed (byte* pinnedOutBytes = outSecBuffer.token)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        Interop.SspiCli.SecBuffer outUnmanagedBuffer = default;
                        outSecurityBufferDescriptor.pBuffers = &outUnmanagedBuffer;

                        // Copy the SecurityBuffer content into unmanaged place holder.
                        outUnmanagedBuffer.cbBuffer = outSecBuffer.size;
                        outUnmanagedBuffer.BufferType = outSecBuffer.type;
                        outUnmanagedBuffer.pvBuffer = outSecBuffer.token == null || outSecBuffer.token.Length == 0 ?
                            IntPtr.Zero :
                            (IntPtr)(pinnedOutBytes + outSecBuffer.offset);

                        if (isSspiAllocated)
                        {
                            outFreeContextBuffer = SafeFreeContextBuffer.CreateEmptyHandle();
                        }

                        if (refContext == null || refContext.IsInvalid)
                        {
                            // Previous versions unconditionally built a new "refContext" here, but would pass
                            // incorrect arguments to AcceptSecurityContext in cases where an "contextHandle" was
                            // already present and non-zero.
                            if (isContextAbsent)
                                refContext = new SafeDeleteContext_SECURITY();
                        }

                        errorCode = MustRunAcceptSecurityContext_SECURITY(
                                        ref inCredentials,
                                        isContextAbsent,
                                        &inSecurityBufferDescriptor,
                                        inFlags,
                                        endianness,
                                        refContext,
                                        ref outSecurityBufferDescriptor,
                                        ref outFlags,
                                        outFreeContextBuffer);

                        if (NetEventSource.IsEnabled) NetEventSource.Info(null, "Marshaling OUT buffer");

                        // Get unmanaged buffer with index 0 as the only one passed into PInvoke.
                        outSecBuffer.size = outUnmanagedBuffer.cbBuffer;
                        outSecBuffer.type = outUnmanagedBuffer.BufferType;
                        outSecBuffer.token = outUnmanagedBuffer.cbBuffer > 0 ?
                            new Span<byte>((byte*)outUnmanagedBuffer.pvBuffer, outUnmanagedBuffer.cbBuffer).ToArray() :
                            null;
                    }
                }
            }
            finally
            {
                outFreeContextBuffer?.Dispose();
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"errorCode:0x{errorCode:x8}, refContext:{refContext}");
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
            SafeFreeContextBuffer handleTemplate)
        {
            int errorCode = (int)Interop.SECURITY_STATUS.InvalidHandle;

            // Run the body of this method as a non-interruptible block.
            try
            {
                bool ignore = false;

                inCredentials.DangerousAddRef(ref ignore);
                outContext.DangerousAddRef(ref ignore);

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
                else
                {
                    inCredentials.DangerousRelease();
                }

                outContext.DangerousRelease();
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
            ref SafeDeleteContext refContext,
            in SecurityBuffer inSecBuffer)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, "SafeDeleteContext::CompleteAuthToken");
                NetEventSource.Info(null, $"    refContext       = {refContext}");
                NetEventSource.Info(null, $"    inSecBuffer      = {inSecBuffer}");
            }

            var inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(1);
            int errorCode = (int)Interop.SECURITY_STATUS.InvalidHandle;

            Interop.SspiCli.SecBuffer inUnmanagedBuffer = default;
            inSecurityBufferDescriptor.pBuffers = &inUnmanagedBuffer;
            fixed (byte* pinnedToken = inSecBuffer.token)
            {
                inUnmanagedBuffer.cbBuffer = inSecBuffer.size;
                inUnmanagedBuffer.BufferType = inSecBuffer.type;

                // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                inUnmanagedBuffer.pvBuffer =
                    inSecBuffer.unmanagedToken != null ? inSecBuffer.unmanagedToken.DangerousGetHandle() :
                    inSecBuffer.token == null || inSecBuffer.token.Length == 0 ? IntPtr.Zero :
                    (IntPtr)(pinnedToken + inSecBuffer.offset);
#if TRACE_VERBOSE
                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"SecBuffer: cbBuffer:{inSecBuffer.size} BufferType: {inSecBuffer.type}");
#endif

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
                        refContext = new SafeDeleteContext_SECURITY();
                    }
                }

                bool gotRef = false;
                try
                {
                    refContext.DangerousAddRef(ref gotRef);
                    errorCode = Interop.SspiCli.CompleteAuthToken(contextHandle.IsZero ? null : &contextHandle, ref inSecurityBufferDescriptor);
                }
                finally
                {
                    if (gotRef)
                    {
                        refContext.DangerousRelease();
                    }
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"unmanaged CompleteAuthToken() errorCode:0x{errorCode:x8} refContext:{refContext}");
            return errorCode;
        }

        internal static unsafe int ApplyControlToken(
            ref SafeDeleteContext refContext,
            in SecurityBuffer inSecBuffer)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null);
                NetEventSource.Info(null, $"    refContext       = {refContext}");
                NetEventSource.Info(null, $"    inSecBuffer      = {inSecBuffer}");
            }

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
#if TRACE_VERBOSE
                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"SecBuffer: cbBuffer:{inSecBuffer.size} BufferType:{inSecBuffer.type}");
#endif

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
                        refContext = new SafeDeleteContext_SECURITY();
                    }
                }

                bool gotRef = false;
                try
                {
                    refContext.DangerousAddRef(ref gotRef);
                    errorCode = Interop.SspiCli.ApplyControlToken(contextHandle.IsZero ? null : &contextHandle, ref inSecurityBufferDescriptor);
                }
                finally
                {
                    if (gotRef)
                    {
                        refContext.DangerousRelease();
                    }
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"unmanaged ApplyControlToken() errorCode:0x{errorCode:x8} refContext: {refContext}");
            return errorCode;
        }
    }

    internal sealed class SafeDeleteContext_SECURITY : SafeDeleteContext
    {
        internal SafeDeleteContext_SECURITY() : base() { }

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

            try
            {
                bool ignore = false;
                phContext.DangerousAddRef(ref ignore);
                status = Interop.SspiCli.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
            }
            finally
            {
                phContext.DangerousRelease();
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

        public override string ToString()
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
