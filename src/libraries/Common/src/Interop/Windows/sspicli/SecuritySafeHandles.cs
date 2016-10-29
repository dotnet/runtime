// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

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

            if (res != 0 && pkgArray != null)
            {
                pkgArray.SetHandleAsInvalid();
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
        public unsafe static int QueryContextAttributes(SafeDeleteContext phContext, Interop.SspiCli.ContextAttribute contextAttribute, byte* buffer, SafeHandle refHandle)
        {
            return QueryContextAttributes_SECURITY(phContext, contextAttribute, buffer, refHandle);
        }

        private unsafe static int QueryContextAttributes_SECURITY(
            SafeDeleteContext phContext,
            Interop.SspiCli.ContextAttribute contextAttribute,
            byte* buffer,
            SafeHandle refHandle)
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

            if (status != 0 && refHandle != null)
            {
                refHandle.SetHandleAsInvalid();
            }

            return status;
        }

        public static int SetContextAttributes(
            SafeDeleteContext phContext,
            Interop.SspiCli.ContextAttribute contextAttribute, byte[] buffer)
        {
            return SetContextAttributes_SECURITY(phContext, contextAttribute, buffer);
        }

        private static int SetContextAttributes_SECURITY(
            SafeDeleteContext phContext,
            Interop.SspiCli.ContextAttribute contextAttribute,
            byte[] buffer)
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

        public unsafe static int AcquireCredentialsHandle(
            string package,
            Interop.SspiCli.CredentialUse intent,
            ref Interop.SspiCli.SEC_WINNT_AUTH_IDENTITY_W authdata,
            out SafeFreeCredentials outCredential)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, package, intent, authdata);

            int errorCode = -1;
            long timeStamp;

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
#if TRACE_VERBOSE
            if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"{nameof(Interop.SspiCli.AcquireCredentialsHandleW)} returns 0x{errorCode:x}, handle:{outCredential}");
#endif

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }

            return errorCode;
        }

        public unsafe static int AcquireDefaultCredential(
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

        public unsafe static int AcquireCredentialsHandle(
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

        public unsafe static int AcquireCredentialsHandle(
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
            if (target != null)
            {
                target.DangerousRelease();
            }

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
    internal abstract class SafeDeleteContext : DebugSafeHandle
    {
#else
    internal abstract class SafeDeleteContext : SafeHandle
    {
#endif
        private const string dummyStr = " ";
        private static readonly byte[] s_dummyBytes = new byte[] { 0 };

        //
        // ATN: _handle is internal since it is used on PInvokes by other wrapper methods.
        //      However all such wrappers MUST manually and reliably adjust refCounter of SafeDeleteContext handle.
        //
        internal Interop.SspiCli.CredHandle _handle;

        protected SafeFreeCredentials _EffectiveCredential;

        protected SafeDeleteContext() : base(IntPtr.Zero, true)
        {
            _handle = new Interop.SspiCli.CredHandle();
        }

        public override bool IsInvalid
        {
            get
            {
                return IsClosed || _handle.IsZero;
            }
        }

        public override string ToString()
        {
            return _handle.ToString();
        }

#if DEBUG
        //This method should never be called for this type
        public new IntPtr DangerousGetHandle()
        {
            throw new InvalidOperationException();
        }
#endif

        //-------------------------------------------------------------------
        internal unsafe static int InitializeSecurityContext(
            ref SafeFreeCredentials inCredentials,
            ref SafeDeleteContext refContext,
            string targetName,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            SecurityBuffer inSecBuffer,
            SecurityBuffer[] inSecBuffers,
            SecurityBuffer outSecBuffer,
            ref Interop.SspiCli.ContextFlags outFlags)
        {
#if TRACE_VERBOSE
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, $"credential:{inCredentials}, crefContext:{refContext}, targetName:{targetName}, inFlags:{inFlags}, endianness:{endianness}");
                if (inSecBuffers == null)
                {
                    NetEventSource.Info(null, $"inSecBuffers = (null)");
                }
                else
                {
                    NetEventSource.Info(null, $"inSecBuffers = {inSecBuffers}");
                }
            }
#endif

            if (outSecBuffer == null)
            {
                NetEventSource.Fail(null, "outSecBuffer != null");
            }
            if (inSecBuffer != null && inSecBuffers != null)
            {
                NetEventSource.Fail(null, "inSecBuffer == null || inSecBuffers == null");
            }

            if (inCredentials == null)
            {
                throw new ArgumentNullException(nameof(inCredentials));
            }

            Interop.SspiCli.SecBufferDesc inSecurityBufferDescriptor = null;
            if (inSecBuffer != null)
            {
                inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(1);
            }
            else if (inSecBuffers != null)
            {
                inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(inSecBuffers.Length);
            }

            Interop.SspiCli.SecBufferDesc outSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(1);

            // Actually, this is returned in outFlags.
            bool isSspiAllocated = (inFlags & Interop.SspiCli.ContextFlags.AllocateMemory) != 0 ? true : false;

            int errorCode = -1;

            Interop.SspiCli.CredHandle contextHandle = new Interop.SspiCli.CredHandle();
            if (refContext != null)
            {
                contextHandle = refContext._handle;
            }

            // These are pinned user byte arrays passed along with SecurityBuffers.
            GCHandle[] pinnedInBytes = null;
            GCHandle pinnedOutBytes = new GCHandle();

            // Optional output buffer that may need to be freed.
            SafeFreeContextBuffer outFreeContextBuffer = null;
            try
            {
                pinnedOutBytes = GCHandle.Alloc(outSecBuffer.token, GCHandleType.Pinned);
                Interop.SspiCli.SecBuffer[] inUnmanagedBuffer = new Interop.SspiCli.SecBuffer[inSecurityBufferDescriptor == null ? 1 : inSecurityBufferDescriptor.cBuffers];
                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                {
                    if (inSecurityBufferDescriptor != null)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        inSecurityBufferDescriptor.pBuffers = inUnmanagedBufferPtr;
                        pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.cBuffers];
                        SecurityBuffer securityBuffer;
                        for (int index = 0; index < inSecurityBufferDescriptor.cBuffers; ++index)
                        {
                            securityBuffer = inSecBuffer != null ? inSecBuffer : inSecBuffers[index];
                            if (securityBuffer != null)
                            {
                                // Copy the SecurityBuffer content into unmanaged place holder.
                                inUnmanagedBuffer[index].cbBuffer = securityBuffer.size;
                                inUnmanagedBuffer[index].BufferType = securityBuffer.type;

                                // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                                if (securityBuffer.unmanagedToken != null)
                                {
                                    inUnmanagedBuffer[index].pvBuffer = securityBuffer.unmanagedToken.DangerousGetHandle();
                                }
                                else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                                {
                                    inUnmanagedBuffer[index].pvBuffer = IntPtr.Zero;
                                }
                                else
                                {
                                    pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                                    inUnmanagedBuffer[index].pvBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                                }
#if TRACE_VERBOSE
                                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"SecBuffer: cbBuffer:{securityBuffer.size} BufferType:{securityBuffer.type}");
#endif
                            }
                        }
                    }

                    Interop.SspiCli.SecBuffer[] outUnmanagedBuffer = new Interop.SspiCli.SecBuffer[1];
                    fixed (void* outUnmanagedBufferPtr = outUnmanagedBuffer)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        outSecurityBufferDescriptor.pBuffers = outUnmanagedBufferPtr;
                        outUnmanagedBuffer[0].cbBuffer = outSecBuffer.size;
                        outUnmanagedBuffer[0].BufferType = outSecBuffer.type;
                        if (outSecBuffer.token == null || outSecBuffer.token.Length == 0)
                        {
                            outUnmanagedBuffer[0].pvBuffer = IntPtr.Zero;
                        }
                        else
                        {
                            outUnmanagedBuffer[0].pvBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(outSecBuffer.token, outSecBuffer.offset);
                        }

                        if (isSspiAllocated)
                        {
                            outFreeContextBuffer = SafeFreeContextBuffer.CreateEmptyHandle();
                        }

                        if (refContext == null || refContext.IsInvalid)
                        {
                            refContext = new SafeDeleteContext_SECURITY();
                        }

                        if (targetName == null || targetName.Length == 0)
                        {
                            targetName = dummyStr;
                        }

                        fixed (char* namePtr = targetName)
                        {
                            errorCode = MustRunInitializeSecurityContext_SECURITY(
                                            ref inCredentials,
                                            contextHandle.IsZero ? null : &contextHandle,
                                            (byte*)(((object)targetName == (object)dummyStr) ? null : namePtr),
                                            inFlags,
                                            endianness,
                                            inSecurityBufferDescriptor,
                                            refContext,
                                            outSecurityBufferDescriptor,
                                            ref outFlags,
                                            outFreeContextBuffer);
                        }

                        if (NetEventSource.IsEnabled) NetEventSource.Info(null, "Marshalling OUT buffer");

                        // Get unmanaged buffer with index 0 as the only one passed into PInvoke.
                        outSecBuffer.size = outUnmanagedBuffer[0].cbBuffer;
                        outSecBuffer.type = outUnmanagedBuffer[0].BufferType;
                        if (outSecBuffer.size > 0)
                        {
                            outSecBuffer.token = new byte[outSecBuffer.size];
                            Marshal.Copy(outUnmanagedBuffer[0].pvBuffer, outSecBuffer.token, 0, outSecBuffer.size);
                        }
                        else
                        {
                            outSecBuffer.token = null;
                        }
                    }
                }
            }
            finally
            {
                if (pinnedInBytes != null)
                {
                    for (int index = 0; index < pinnedInBytes.Length; index++)
                    {
                        if (pinnedInBytes[index].IsAllocated)
                        {
                            pinnedInBytes[index].Free();
                        }
                    }
                }
                if (pinnedOutBytes.IsAllocated)
                {
                    pinnedOutBytes.Free();
                }

                if (outFreeContextBuffer != null)
                {
                    outFreeContextBuffer.Dispose();
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"errorCode:0x{errorCode:x8}, refContext:{refContext}");
            return errorCode;
        }

        //
        // After PInvoke call the method will fix the handleTemplate.handle with the returned value.
        // The caller is responsible for creating a correct SafeFreeContextBuffer_XXX flavor or null can be passed if no handle is returned.
        //
        private static unsafe int MustRunInitializeSecurityContext_SECURITY(
            ref SafeFreeCredentials inCredentials,
            void* inContextPtr,
            byte* targetName,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            Interop.SspiCli.SecBufferDesc inputBuffer,
            SafeDeleteContext outContext,
            Interop.SspiCli.SecBufferDesc outputBuffer,
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
                                outputBuffer,
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
                    if (outContext._EffectiveCredential != null)
                    {
                        outContext._EffectiveCredential.DangerousRelease();
                    }

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

            if (inContextPtr == null && (errorCode & 0x80000000) != 0)
            {
                // an error on the first call, need to set the out handle to invalid value
                outContext._handle.SetToInvalid();
            }

            return errorCode;
        }

        //-------------------------------------------------------------------
        internal unsafe static int AcceptSecurityContext(
            ref SafeFreeCredentials inCredentials,
            ref SafeDeleteContext refContext,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            SecurityBuffer inSecBuffer,
            SecurityBuffer[] inSecBuffers,
            SecurityBuffer outSecBuffer,
            ref Interop.SspiCli.ContextFlags outFlags)
        {
#if TRACE_VERBOSE
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, $"credential={inCredentials}, refContext={refContext}, inFlags={inFlags}");
                if (inSecBuffers == null)
                {
                    NetEventSource.Info(null, "inSecBuffers = (null)");
                }
                else
                {
                    NetEventSource.Info(null, $"inSecBuffers[] = (inSecBuffers)");
                }
            }
#endif

            if (outSecBuffer == null)
            {
                NetEventSource.Fail(null, "outSecBuffer != null");
            }
            if (inSecBuffer != null && inSecBuffers != null)
            {
                NetEventSource.Fail(null, "inSecBuffer == null || inSecBuffers == null");
            }

            if (inCredentials == null)
            {
                throw new ArgumentNullException(nameof(inCredentials));
            }

            Interop.SspiCli.SecBufferDesc inSecurityBufferDescriptor = null;
            if (inSecBuffer != null)
            {
                inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(1);
            }
            else if (inSecBuffers != null)
            {
                inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(inSecBuffers.Length);
            }

            Interop.SspiCli.SecBufferDesc outSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(1);

            // Actually, this is returned in outFlags.
            bool isSspiAllocated = (inFlags & Interop.SspiCli.ContextFlags.AllocateMemory) != 0 ? true : false;

            int errorCode = -1;

            Interop.SspiCli.CredHandle contextHandle = new Interop.SspiCli.CredHandle();
            if (refContext != null)
            {
                contextHandle = refContext._handle;
            }

            // These are pinned user byte arrays passed along with SecurityBuffers.
            GCHandle[] pinnedInBytes = null;
            GCHandle pinnedOutBytes = new GCHandle();

            // Optional output buffer that may need to be freed.
            SafeFreeContextBuffer outFreeContextBuffer = null;
            try
            {
                pinnedOutBytes = GCHandle.Alloc(outSecBuffer.token, GCHandleType.Pinned);
                var inUnmanagedBuffer = new Interop.SspiCli.SecBuffer[inSecurityBufferDescriptor == null ? 1 : inSecurityBufferDescriptor.cBuffers];
                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                {
                    if (inSecurityBufferDescriptor != null)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        inSecurityBufferDescriptor.pBuffers = inUnmanagedBufferPtr;
                        pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.cBuffers];
                        SecurityBuffer securityBuffer;
                        for (int index = 0; index < inSecurityBufferDescriptor.cBuffers; ++index)
                        {
                            securityBuffer = inSecBuffer != null ? inSecBuffer : inSecBuffers[index];
                            if (securityBuffer != null)
                            {
                                // Copy the SecurityBuffer content into unmanaged place holder.
                                inUnmanagedBuffer[index].cbBuffer = securityBuffer.size;
                                inUnmanagedBuffer[index].BufferType = securityBuffer.type;

                                // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                                if (securityBuffer.unmanagedToken != null)
                                {
                                    inUnmanagedBuffer[index].pvBuffer = securityBuffer.unmanagedToken.DangerousGetHandle();
                                }
                                else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                                {
                                    inUnmanagedBuffer[index].pvBuffer = IntPtr.Zero;
                                }
                                else
                                {
                                    pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                                    inUnmanagedBuffer[index].pvBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                                }
#if TRACE_VERBOSE
                                if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"SecBuffer: cbBuffer:{securityBuffer.size} BufferType:{securityBuffer.type}");
#endif
                            }
                        }
                    }

                    var outUnmanagedBuffer = new Interop.SspiCli.SecBuffer[1];
                    fixed (void* outUnmanagedBufferPtr = outUnmanagedBuffer)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        outSecurityBufferDescriptor.pBuffers = outUnmanagedBufferPtr;
                        // Copy the SecurityBuffer content into unmanaged place holder.
                        outUnmanagedBuffer[0].cbBuffer = outSecBuffer.size;
                        outUnmanagedBuffer[0].BufferType = outSecBuffer.type;

                        if (outSecBuffer.token == null || outSecBuffer.token.Length == 0)
                        {
                            outUnmanagedBuffer[0].pvBuffer = IntPtr.Zero;
                        }
                        else
                        {
                            outUnmanagedBuffer[0].pvBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(outSecBuffer.token, outSecBuffer.offset);
                        }

                        if (isSspiAllocated)
                        {
                            outFreeContextBuffer = SafeFreeContextBuffer.CreateEmptyHandle();
                        }

                        if (refContext == null || refContext.IsInvalid)
                        {
                            refContext = new SafeDeleteContext_SECURITY();
                        }

                        errorCode = MustRunAcceptSecurityContext_SECURITY(
                                        ref inCredentials,
                                        contextHandle.IsZero ? null : &contextHandle,
                                        inSecurityBufferDescriptor,
                                        inFlags,
                                        endianness,
                                        refContext,
                                        outSecurityBufferDescriptor,
                                        ref outFlags,
                                        outFreeContextBuffer);

                        if (NetEventSource.IsEnabled) NetEventSource.Info(null, "Marshaling OUT buffer");
                        
                        // Get unmanaged buffer with index 0 as the only one passed into PInvoke.
                        outSecBuffer.size = outUnmanagedBuffer[0].cbBuffer;
                        outSecBuffer.type = outUnmanagedBuffer[0].BufferType;
                        if (outSecBuffer.size > 0)
                        {
                            outSecBuffer.token = new byte[outSecBuffer.size];
                            Marshal.Copy(outUnmanagedBuffer[0].pvBuffer, outSecBuffer.token, 0, outSecBuffer.size);
                        }
                        else
                        {
                            outSecBuffer.token = null;
                        }
                    }
                }
            }
            finally
            {
                if (pinnedInBytes != null)
                {
                    for (int index = 0; index < pinnedInBytes.Length; index++)
                    {
                        if (pinnedInBytes[index].IsAllocated)
                        {
                            pinnedInBytes[index].Free();
                        }
                    }
                }

                if (pinnedOutBytes.IsAllocated)
                {
                    pinnedOutBytes.Free();
                }

                if (outFreeContextBuffer != null)
                {
                    outFreeContextBuffer.Dispose();
                }
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
            void* inContextPtr,
            Interop.SspiCli.SecBufferDesc inputBuffer,
            Interop.SspiCli.ContextFlags inFlags,
            Interop.SspiCli.Endianness endianness,
            SafeDeleteContext outContext,
            Interop.SspiCli.SecBufferDesc outputBuffer,
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

                errorCode = Interop.SspiCli.AcceptSecurityContext(
                                ref credentialHandle,
                                inContextPtr,
                                inputBuffer,
                                inFlags,
                                endianness,
                                ref outContext._handle,
                                outputBuffer,
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
                    if (outContext._EffectiveCredential != null)
                    {
                        outContext._EffectiveCredential.DangerousRelease();
                    }

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

            if (inContextPtr == null && (errorCode & 0x80000000) != 0)
            {
                // An error on the first call, need to set the out handle to invalid value.
                outContext._handle.SetToInvalid();
            }

            return errorCode;
        }

        internal unsafe static int CompleteAuthToken(
            ref SafeDeleteContext refContext,
            SecurityBuffer[] inSecBuffers)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, "SafeDeleteContext::CompleteAuthToken");
                NetEventSource.Info(null, $"    refContext       = {refContext}");
                NetEventSource.Info(null, $"    inSecBuffers[]   = {inSecBuffers}");
            }
            if (inSecBuffers == null)
            {
                NetEventSource.Fail(null, "inSecBuffers == null");
            }

            var inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(inSecBuffers.Length);

            int errorCode = (int)Interop.SECURITY_STATUS.InvalidHandle;

            // These are pinned user byte arrays passed along with SecurityBuffers.
            GCHandle[] pinnedInBytes = null;

            var inUnmanagedBuffer = new Interop.SspiCli.SecBuffer[inSecurityBufferDescriptor.cBuffers];
            fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
            {
                // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                inSecurityBufferDescriptor.pBuffers = inUnmanagedBufferPtr;
                pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.cBuffers];
                SecurityBuffer securityBuffer;
                for (int index = 0; index < inSecurityBufferDescriptor.cBuffers; ++index)
                {
                    securityBuffer = inSecBuffers[index];
                    if (securityBuffer != null)
                    {
                        inUnmanagedBuffer[index].cbBuffer = securityBuffer.size;
                        inUnmanagedBuffer[index].BufferType = securityBuffer.type;

                        // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                        if (securityBuffer.unmanagedToken != null)
                        {
                            inUnmanagedBuffer[index].pvBuffer = securityBuffer.unmanagedToken.DangerousGetHandle();
                        }
                        else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                        {
                            inUnmanagedBuffer[index].pvBuffer = IntPtr.Zero;
                        }
                        else
                        {
                            pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                            inUnmanagedBuffer[index].pvBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                        }
#if TRACE_VERBOSE
                        if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"SecBuffer: cbBuffer:{securityBuffer.size} BufferType: {securityBuffer.type}");
#endif
                    }
                }

                Interop.SspiCli.CredHandle contextHandle = new Interop.SspiCli.CredHandle();
                if (refContext != null)
                {
                    contextHandle = refContext._handle;
                }
                try
                {
                    if (refContext == null || refContext.IsInvalid)
                    {
                        refContext = new SafeDeleteContext_SECURITY();
                    }

                    try
                    {
                        bool ignore = false;
                        refContext.DangerousAddRef(ref ignore);
                        errorCode = Interop.SspiCli.CompleteAuthToken(contextHandle.IsZero ? null : &contextHandle, inSecurityBufferDescriptor);
                    }
                    finally
                    {
                        refContext.DangerousRelease();
                    }
                }
                finally
                {
                    if (pinnedInBytes != null)
                    {
                        for (int index = 0; index < pinnedInBytes.Length; index++)
                        {
                            if (pinnedInBytes[index].IsAllocated)
                            {
                                pinnedInBytes[index].Free();
                            }
                        }
                    }
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"unmanaged CompleteAuthToken() errorCode:0x{errorCode:x8} refContext:{refContext}");
            return errorCode;
        }

        internal unsafe static int ApplyControlToken(
            ref SafeDeleteContext refContext,
            SecurityBuffer[] inSecBuffers)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null);
                NetEventSource.Info(null, $"    refContext       = {refContext}");
                NetEventSource.Info(null, $"    inSecBuffers[]   = length:{inSecBuffers.Length}");
            }

            if (inSecBuffers == null)
            {
                NetEventSource.Fail(null, "inSecBuffers == null");
            }

            var inSecurityBufferDescriptor = new Interop.SspiCli.SecBufferDesc(inSecBuffers.Length);

            int errorCode = (int)Interop.SECURITY_STATUS.InvalidHandle;

            // These are pinned user byte arrays passed along with SecurityBuffers.
            GCHandle[] pinnedInBytes = null;

            var inUnmanagedBuffer = new Interop.SspiCli.SecBuffer[inSecurityBufferDescriptor.cBuffers];
            fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
            {
                // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                inSecurityBufferDescriptor.pBuffers = inUnmanagedBufferPtr;
                pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.cBuffers];
                SecurityBuffer securityBuffer;
                for (int index = 0; index < inSecurityBufferDescriptor.cBuffers; ++index)
                {
                    securityBuffer = inSecBuffers[index];
                    if (securityBuffer != null)
                    {
                        inUnmanagedBuffer[index].cbBuffer = securityBuffer.size;
                        inUnmanagedBuffer[index].BufferType = securityBuffer.type;

                        // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                        if (securityBuffer.unmanagedToken != null)
                        {
                            inUnmanagedBuffer[index].pvBuffer = securityBuffer.unmanagedToken.DangerousGetHandle();
                        }
                        else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                        {
                            inUnmanagedBuffer[index].pvBuffer = IntPtr.Zero;
                        }
                        else
                        {
                            pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                            inUnmanagedBuffer[index].pvBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                        }
#if TRACE_VERBOSE
                        if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"SecBuffer: cbBuffer:{securityBuffer.size} BufferType:{securityBuffer.type}");
#endif
                    }
                }
                
                // TODO: (#3114): Optimizations to remove the unnecesary allocation of a CredHandle, remove the AddRef
                // if refContext was previously null, refactor the code to unify CompleteAuthToken and ApplyControlToken.
                Interop.SspiCli.CredHandle contextHandle = new Interop.SspiCli.CredHandle();
                if (refContext != null)
                {
                    contextHandle = refContext._handle;
                }

                try
                {
                    if (refContext == null || refContext.IsInvalid)
                    {
                        refContext = new SafeDeleteContext_SECURITY();
                    }

                    try
                    {
                        bool ignore = false;
                        refContext.DangerousAddRef(ref ignore);
                        errorCode = Interop.SspiCli.ApplyControlToken(contextHandle.IsZero ? null : &contextHandle, inSecurityBufferDescriptor);
                    }
                    finally
                    {
                        refContext.DangerousRelease();
                    }
                }
                finally
                {
                    if (pinnedInBytes != null)
                    {
                        for (int index = 0; index < pinnedInBytes.Length; index++)
                        {
                            if (pinnedInBytes[index].IsAllocated)
                            {
                                pinnedInBytes[index].Free();
                            }
                        }
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
            if (this._EffectiveCredential != null)
            {
                this._EffectiveCredential.DangerousRelease();
            }

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

        public unsafe static int QueryContextChannelBinding(SafeDeleteContext phContext, Interop.SspiCli.ContextAttribute contextAttribute, SecPkgContext_Bindings* buffer, SafeFreeContextBufferChannelBinding refHandle)
        {
            return QueryContextChannelBinding_SECURITY(phContext, contextAttribute, buffer, refHandle);
        }

        private unsafe static int QueryContextChannelBinding_SECURITY(SafeDeleteContext phContext, Interop.SspiCli.ContextAttribute contextAttribute, SecPkgContext_Bindings* buffer, SafeFreeContextBufferChannelBinding refHandle)
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

            if (status != 0 && refHandle != null)
            {
                refHandle.SetHandleAsInvalid();
            }

            return status;
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
