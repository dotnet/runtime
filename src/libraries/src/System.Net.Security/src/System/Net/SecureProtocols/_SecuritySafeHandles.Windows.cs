// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            return Interop.Secur32.SspiFreeAuthIdentity(handle) == Interop.SecurityStatus.OK;
        }
    }

    //
    //  A set of Safe Handles that depend on native FreeContextBuffer finalizer
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
            res = Interop.Secur32.EnumerateSecurityPackagesW(out pkgnum, out pkgArray_SECURITY);
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
        public unsafe static int QueryContextAttributes(SafeDeleteContext phContext, Interop.Secur32.ContextAttribute contextAttribute, byte* buffer, SafeHandle refHandle)
        {
            return QueryContextAttributes_SECURITY(phContext, contextAttribute, buffer, refHandle);
        }

        private unsafe static int QueryContextAttributes_SECURITY(
            SafeDeleteContext phContext,
            Interop.Secur32.ContextAttribute contextAttribute,
            byte* buffer,
            SafeHandle refHandle)
        {
            int status = (int)Interop.SecurityStatus.InvalidHandle;

            try
            {
                bool ignore = false;
                phContext.DangerousAddRef(ref ignore);
                status = Interop.Secur32.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
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
            Interop.Secur32.ContextAttribute contextAttribute, byte[] buffer)
        {
            return SetContextAttributes_SECURITY(phContext, contextAttribute, buffer);
        }

        private static int SetContextAttributes_SECURITY(
            SafeDeleteContext phContext,
            Interop.Secur32.ContextAttribute contextAttribute,
            byte[] buffer)
        {
            try
            {
                bool ignore = false;
                phContext.DangerousAddRef(ref ignore);
                return Interop.Secur32.SetContextAttributesW(ref phContext._handle, contextAttribute, buffer, buffer.Length);
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
            return Interop.Secur32.FreeContextBuffer(handle) == 0;
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

        internal Interop.Secur32.SSPIHandle _handle;    //should be always used as by ref in PInvokes parameters

        protected SafeFreeCredentials() : base(IntPtr.Zero, true)
        {
            _handle = new Interop.Secur32.SSPIHandle();
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
            Interop.Secur32.CredentialUse intent,
            ref Interop.Secur32.AuthIdentity authdata,
            out SafeFreeCredentials outCredential)
        {
            GlobalLog.Print("SafeFreeCredentials::AcquireCredentialsHandle#1("
                            + package + ", "
                            + intent + ", "
                            + authdata + ")");

            int errorCode = -1;
            long timeStamp;

            outCredential = new SafeFreeCredential_SECURITY();

            errorCode = Interop.Secur32.AcquireCredentialsHandleW(
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
            GlobalLog.Print("Unmanaged::AcquireCredentialsHandle() returns 0x"
                            + String.Format("{0:x}", errorCode)
                            + ", handle = " + outCredential.ToString());
#endif

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }

            return errorCode;
        }

        public unsafe static int AcquireDefaultCredential(
            string package,
            Interop.Secur32.CredentialUse intent,
            out SafeFreeCredentials outCredential)
        {
            GlobalLog.Print("SafeFreeCredentials::AcquireDefaultCredential("
                            + package + ", "
                            + intent + ")");

            int errorCode = -1;
            long timeStamp;

            outCredential = new SafeFreeCredential_SECURITY();

            errorCode = Interop.Secur32.AcquireCredentialsHandleW(
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
            GlobalLog.Print("Unmanaged::AcquireCredentialsHandle() returns 0x"
                            + errorCode.ToString("x")
                            + ", handle = " + outCredential.ToString());
#endif

            if (errorCode != 0)
            {
                outCredential.SetHandleAsInvalid();
            }

            return errorCode;
        }

        public unsafe static int AcquireCredentialsHandle(
            string package,
            Interop.Secur32.CredentialUse intent,
            ref SafeSspiAuthDataHandle authdata,
            out SafeFreeCredentials outCredential)
        {
            int errorCode = -1;
            long timeStamp;

            outCredential = new SafeFreeCredential_SECURITY();
            errorCode = Interop.Secur32.AcquireCredentialsHandleW(
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
            Interop.Secur32.CredentialUse intent,
            ref Interop.Secur32.SecureCredential authdata,
            out SafeFreeCredentials outCredential)
        {
            GlobalLog.Print("SafeFreeCredentials::AcquireCredentialsHandle#2("
                            + package + ", "
                            + intent + ", "
                            + authdata + ")");

            int errorCode = -1;
            long timeStamp;


            // If there is a certificate, wrap it into an array.
            // Not threadsafe.
            IntPtr copiedPtr = authdata.certContextArray;
            try
            {
                IntPtr certArrayPtr = new IntPtr(&copiedPtr);
                if (copiedPtr != IntPtr.Zero)
                {
                    authdata.certContextArray = certArrayPtr;
                }

                outCredential = new SafeFreeCredential_SECURITY();

                errorCode = Interop.Secur32.AcquireCredentialsHandleW(
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
                authdata.certContextArray = copiedPtr;
            }

#if TRACE_VERBOSE
            GlobalLog.Print("Unmanaged::AcquireCredentialsHandle() returns 0x"
                            + errorCode.ToString("x")
                            + ", handle = " + outCredential.ToString());
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
        internal SafeFreeCredentials _Target;

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
            _Target = target;
            SetHandle(new IntPtr(0));   // make this handle valid
        }

        protected override bool ReleaseHandle()
        {
            SafeFreeCredentials target = _Target;
            if (target != null)
            {
                target.DangerousRelease();
            }

            _Target = null;
            return true;
        }
    }

    internal sealed class SafeFreeCredential_SECURITY : SafeFreeCredentials
    {
        public SafeFreeCredential_SECURITY() : base() { }

        protected override bool ReleaseHandle()
        {
            return Interop.Secur32.FreeCredentialsHandle(ref _handle) == 0;
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
        internal Interop.Secur32.SSPIHandle _handle;

        protected SafeFreeCredentials _EffectiveCredential;

        protected SafeDeleteContext() : base(IntPtr.Zero, true)
        {
            _handle = new Interop.Secur32.SSPIHandle();
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
            Interop.Secur32.ContextFlags inFlags,
            Interop.Secur32.Endianness endianness,
            SecurityBuffer inSecBuffer,
            SecurityBuffer[] inSecBuffers,
            SecurityBuffer outSecBuffer,
            ref Interop.Secur32.ContextFlags outFlags)
        {
#if TRACE_VERBOSE
            GlobalLog.Enter("SafeDeleteContext::InitializeSecurityContext");
            GlobalLog.Print("    credential       = " + inCredentials.ToString());
            GlobalLog.Print("    refContext       = " + Logging.ObjectToString(refContext));
            GlobalLog.Print("    targetName       = " + targetName);
            GlobalLog.Print("    inFlags          = " + inFlags);
            GlobalLog.Print("    reservedI        = 0x0");
            GlobalLog.Print("    endianness       = " + endianness);

            if (inSecBuffers == null)
            {
                GlobalLog.Print("    inSecBuffers     = (null)");
            }
            else
            {
                GlobalLog.Print("    inSecBuffers[]   = length:" + inSecBuffers.Length);
            }
#endif
            GlobalLog.Assert(outSecBuffer != null, "SafeDeleteContext::InitializeSecurityContext()|outSecBuffer != null");
            GlobalLog.Assert(inSecBuffer == null || inSecBuffers == null, "SafeDeleteContext::InitializeSecurityContext()|inSecBuffer == null || inSecBuffers == null");

            if (inCredentials == null)
            {
                throw new ArgumentNullException("inCredentials");
            }

            Interop.Secur32.SecurityBufferDescriptor inSecurityBufferDescriptor = null;
            if (inSecBuffer != null)
            {
                inSecurityBufferDescriptor = new Interop.Secur32.SecurityBufferDescriptor(1);
            }
            else if (inSecBuffers != null)
            {
                inSecurityBufferDescriptor = new Interop.Secur32.SecurityBufferDescriptor(inSecBuffers.Length);
            }

            Interop.Secur32.SecurityBufferDescriptor outSecurityBufferDescriptor = new Interop.Secur32.SecurityBufferDescriptor(1);

            // Actually, this is returned in outFlags.
            bool isSspiAllocated = (inFlags & Interop.Secur32.ContextFlags.AllocateMemory) != 0 ? true : false;

            int errorCode = -1;

            Interop.Secur32.SSPIHandle contextHandle = new Interop.Secur32.SSPIHandle();
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
                Interop.Secur32.SecurityBufferStruct[] inUnmanagedBuffer = new Interop.Secur32.SecurityBufferStruct[inSecurityBufferDescriptor == null ? 1 : inSecurityBufferDescriptor.Count];
                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                {
                    if (inSecurityBufferDescriptor != null)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        inSecurityBufferDescriptor.UnmanagedPointer = inUnmanagedBufferPtr;
                        pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.Count];
                        SecurityBuffer securityBuffer;
                        for (int index = 0; index < inSecurityBufferDescriptor.Count; ++index)
                        {
                            securityBuffer = inSecBuffer != null ? inSecBuffer : inSecBuffers[index];
                            if (securityBuffer != null)
                            {
                                // Copy the SecurityBuffer content into unmanaged place holder.
                                inUnmanagedBuffer[index].count = securityBuffer.size;
                                inUnmanagedBuffer[index].type = securityBuffer.type;

                                // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                                if (securityBuffer.unmanagedToken != null)
                                {
                                    inUnmanagedBuffer[index].token = securityBuffer.unmanagedToken.DangerousGetHandle();
                                }
                                else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                                {
                                    inUnmanagedBuffer[index].token = IntPtr.Zero;
                                }
                                else
                                {
                                    pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                                    inUnmanagedBuffer[index].token = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                                }
#if TRACE_VERBOSE
                                GlobalLog.Print("SecBuffer: cbBuffer:" + securityBuffer.size + " BufferType:" + securityBuffer.type);
#endif
                            }
                        }
                    }

                    Interop.Secur32.SecurityBufferStruct[] outUnmanagedBuffer = new Interop.Secur32.SecurityBufferStruct[1];
                    fixed (void* outUnmanagedBufferPtr = outUnmanagedBuffer)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        outSecurityBufferDescriptor.UnmanagedPointer = outUnmanagedBufferPtr;
                        outUnmanagedBuffer[0].count = outSecBuffer.size;
                        outUnmanagedBuffer[0].type = outSecBuffer.type;
                        if (outSecBuffer.token == null || outSecBuffer.token.Length == 0)
                        {
                            outUnmanagedBuffer[0].token = IntPtr.Zero;
                        }
                        else
                        {
                            outUnmanagedBuffer[0].token = Marshal.UnsafeAddrOfPinnedArrayElement(outSecBuffer.token, outSecBuffer.offset);
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

                        GlobalLog.Print("SafeDeleteContext:InitializeSecurityContext  Marshalling OUT buffer");
                        // Get unmanaged buffer with index 0 as the only one passed into PInvoke.
                        outSecBuffer.size = outUnmanagedBuffer[0].count;
                        outSecBuffer.type = outUnmanagedBuffer[0].type;
                        if (outSecBuffer.size > 0)
                        {
                            outSecBuffer.token = new byte[outSecBuffer.size];
                            Marshal.Copy(outUnmanagedBuffer[0].token, outSecBuffer.token, 0, outSecBuffer.size);
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

            GlobalLog.Leave("SafeDeleteContext::InitializeSecurityContext() unmanaged InitializeSecurityContext()", "errorCode:0x" + errorCode.ToString("x8") + " refContext:" + Logging.ObjectToString(refContext));

            return errorCode;
        }

        //
        // After PInvoke call the method will fix the handleTemplate.handle with the returned value.
        // The caller is responsible for creating a correct SafeFreeContextBuffer_XXX flavour or null can be passed if no handle is returned.
        //
        private static unsafe int MustRunInitializeSecurityContext_SECURITY(
            ref SafeFreeCredentials inCredentials,
            void* inContextPtr,
            byte* targetName,
            Interop.Secur32.ContextFlags inFlags,
            Interop.Secur32.Endianness endianness,
            Interop.Secur32.SecurityBufferDescriptor inputBuffer,
            SafeDeleteContext outContext,
            Interop.Secur32.SecurityBufferDescriptor outputBuffer,
            ref Interop.Secur32.ContextFlags attributes,
            SafeFreeContextBuffer handleTemplate)
        {
            int errorCode = (int)Interop.SecurityStatus.InvalidHandle;

            try
            {
                bool ignore = false;
                inCredentials.DangerousAddRef(ref ignore);
                outContext.DangerousAddRef(ref ignore);

                Interop.Secur32.SSPIHandle credentialHandle = inCredentials._handle;

                long timeStamp;

                errorCode = Interop.Secur32.InitializeSecurityContextW(
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
                handleTemplate.Set(((Interop.Secur32.SecurityBufferStruct*)outputBuffer.UnmanagedPointer)->token);
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
            Interop.Secur32.ContextFlags inFlags,
            Interop.Secur32.Endianness endianness,
            SecurityBuffer inSecBuffer,
            SecurityBuffer[] inSecBuffers,
            SecurityBuffer outSecBuffer,
            ref Interop.Secur32.ContextFlags outFlags)
        {
#if TRACE_VERBOSE
            GlobalLog.Enter("SafeDeleteContext::AcceptSecurityContex");
            GlobalLog.Print("    credential       = " + inCredentials.ToString());
            GlobalLog.Print("    refContext       = " + Logging.ObjectToString(refContext));

            GlobalLog.Print("    inFlags          = " + inFlags);

            if (inSecBuffers == null)
            {
                GlobalLog.Print("    inSecBuffers     = (null)");
            }
            else
            {
                GlobalLog.Print("    inSecBuffers[]   = length:" + inSecBuffers.Length);
            }
#endif
            GlobalLog.Assert(outSecBuffer != null, "SafeDeleteContext::AcceptSecurityContext()|outSecBuffer != null");
            GlobalLog.Assert(inSecBuffer == null || inSecBuffers == null, "SafeDeleteContext::AcceptSecurityContext()|inSecBuffer == null || inSecBuffers == null");

            if (inCredentials == null)
            {
                throw new ArgumentNullException("inCredentials");
            }

            Interop.Secur32.SecurityBufferDescriptor inSecurityBufferDescriptor = null;
            if (inSecBuffer != null)
            {
                inSecurityBufferDescriptor = new Interop.Secur32.SecurityBufferDescriptor(1);
            }
            else if (inSecBuffers != null)
            {
                inSecurityBufferDescriptor = new Interop.Secur32.SecurityBufferDescriptor(inSecBuffers.Length);
            }

            Interop.Secur32.SecurityBufferDescriptor outSecurityBufferDescriptor = new Interop.Secur32.SecurityBufferDescriptor(1);

            // Actually, this is returned in outFlags.
            bool isSspiAllocated = (inFlags & Interop.Secur32.ContextFlags.AllocateMemory) != 0 ? true : false;

            int errorCode = -1;

            Interop.Secur32.SSPIHandle contextHandle = new Interop.Secur32.SSPIHandle();
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
                var inUnmanagedBuffer = new Interop.Secur32.SecurityBufferStruct[inSecurityBufferDescriptor == null ? 1 : inSecurityBufferDescriptor.Count];
                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                {
                    if (inSecurityBufferDescriptor != null)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        inSecurityBufferDescriptor.UnmanagedPointer = inUnmanagedBufferPtr;
                        pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.Count];
                        SecurityBuffer securityBuffer;
                        for (int index = 0; index < inSecurityBufferDescriptor.Count; ++index)
                        {
                            securityBuffer = inSecBuffer != null ? inSecBuffer : inSecBuffers[index];
                            if (securityBuffer != null)
                            {
                                // Copy the SecurityBuffer content into unmanaged place holder.
                                inUnmanagedBuffer[index].count = securityBuffer.size;
                                inUnmanagedBuffer[index].type = securityBuffer.type;

                                // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                                if (securityBuffer.unmanagedToken != null)
                                {
                                    inUnmanagedBuffer[index].token = securityBuffer.unmanagedToken.DangerousGetHandle();
                                }
                                else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                                {
                                    inUnmanagedBuffer[index].token = IntPtr.Zero;
                                }
                                else
                                {
                                    pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                                    inUnmanagedBuffer[index].token = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                                }
#if TRACE_VERBOSE
                                GlobalLog.Print("SecBuffer: cbBuffer:" + securityBuffer.size + " BufferType:" + securityBuffer.type);
#endif
                            }
                        }
                    }

                    var outUnmanagedBuffer = new Interop.Secur32.SecurityBufferStruct[1];
                    fixed (void* outUnmanagedBufferPtr = outUnmanagedBuffer)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                        outSecurityBufferDescriptor.UnmanagedPointer = outUnmanagedBufferPtr;
                        // Copy the SecurityBuffer content into unmanaged place holder.
                        outUnmanagedBuffer[0].count = outSecBuffer.size;
                        outUnmanagedBuffer[0].type = outSecBuffer.type;

                        if (outSecBuffer.token == null || outSecBuffer.token.Length == 0)
                        {
                            outUnmanagedBuffer[0].token = IntPtr.Zero;
                        }
                        else
                        {
                            outUnmanagedBuffer[0].token = Marshal.UnsafeAddrOfPinnedArrayElement(outSecBuffer.token, outSecBuffer.offset);
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

                        GlobalLog.Print("SafeDeleteContext:AcceptSecurityContext  Marshalling OUT buffer");
                        // Get unmanaged buffer with index 0 as the only one passed into PInvoke.
                        outSecBuffer.size = outUnmanagedBuffer[0].count;
                        outSecBuffer.type = outUnmanagedBuffer[0].type;
                        if (outSecBuffer.size > 0)
                        {
                            outSecBuffer.token = new byte[outSecBuffer.size];
                            Marshal.Copy(outUnmanagedBuffer[0].token, outSecBuffer.token, 0, outSecBuffer.size);
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

            GlobalLog.Leave("SafeDeleteContext::AcceptSecurityContex() unmanaged AcceptSecurityContex()", "errorCode:0x" + errorCode.ToString("x8") + " refContext:" + Logging.ObjectToString(refContext));

            return errorCode;
        }

        //
        // After PInvoke call the method will fix the handleTemplate.handle with the returned value.
        // The caller is responsible for creating a correct SafeFreeContextBuffer_XXX flavour or null can be passed if no handle is returned.
        //
        private static unsafe int MustRunAcceptSecurityContext_SECURITY(
            ref SafeFreeCredentials inCredentials,
            void* inContextPtr,
            Interop.Secur32.SecurityBufferDescriptor inputBuffer,
            Interop.Secur32.ContextFlags inFlags,
            Interop.Secur32.Endianness endianness,
            SafeDeleteContext outContext,
            Interop.Secur32.SecurityBufferDescriptor outputBuffer,
            ref Interop.Secur32.ContextFlags outFlags,
            SafeFreeContextBuffer handleTemplate)
        {
            int errorCode = (int)Interop.SecurityStatus.InvalidHandle;

            // Run the body of this method as a non-interruptible block.
            try
            {
                bool ignore = false;

                inCredentials.DangerousAddRef(ref ignore);
                outContext.DangerousAddRef(ref ignore);

                Interop.Secur32.SSPIHandle credentialHandle = inCredentials._handle;
                long timeStamp;

                errorCode = Interop.Secur32.AcceptSecurityContext(
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
                handleTemplate.Set(((Interop.Secur32.SecurityBufferStruct*)outputBuffer.UnmanagedPointer)->token);
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
            GlobalLog.Enter("SafeDeleteContext::CompleteAuthToken");
            GlobalLog.Print("    refContext       = " + Logging.ObjectToString(refContext));
#if TRACE_VERBOSE
            GlobalLog.Print("    inSecBuffers[]   = length:" + inSecBuffers.Length);
#endif
            GlobalLog.Assert(inSecBuffers != null, "SafeDeleteContext::CompleteAuthToken()|inSecBuffers == null");
            var inSecurityBufferDescriptor = new Interop.Secur32.SecurityBufferDescriptor(inSecBuffers.Length);

            int errorCode = (int)Interop.SecurityStatus.InvalidHandle;

            // These are pinned user byte arrays passed along with SecurityBuffers.
            GCHandle[] pinnedInBytes = null;

            var inUnmanagedBuffer = new Interop.Secur32.SecurityBufferStruct[inSecurityBufferDescriptor.Count];
            fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
            {
                // Fix Descriptor pointer that points to unmanaged SecurityBuffers.
                inSecurityBufferDescriptor.UnmanagedPointer = inUnmanagedBufferPtr;
                pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.Count];
                SecurityBuffer securityBuffer;
                for (int index = 0; index < inSecurityBufferDescriptor.Count; ++index)
                {
                    securityBuffer = inSecBuffers[index];
                    if (securityBuffer != null)
                    {
                        inUnmanagedBuffer[index].count = securityBuffer.size;
                        inUnmanagedBuffer[index].type = securityBuffer.type;

                        // Use the unmanaged token if it's not null; otherwise use the managed buffer.
                        if (securityBuffer.unmanagedToken != null)
                        {
                            inUnmanagedBuffer[index].token = securityBuffer.unmanagedToken.DangerousGetHandle();
                        }
                        else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                        {
                            inUnmanagedBuffer[index].token = IntPtr.Zero;
                        }
                        else
                        {
                            pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                            inUnmanagedBuffer[index].token = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                        }
#if TRACE_VERBOSE
                        GlobalLog.Print("SecBuffer: cbBuffer:" + securityBuffer.size + " BufferType:" + securityBuffer.type);
#endif
                    }
                }

                Interop.Secur32.SSPIHandle contextHandle = new Interop.Secur32.SSPIHandle();
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
                        errorCode = Interop.Secur32.CompleteAuthToken(contextHandle.IsZero ? null : &contextHandle, inSecurityBufferDescriptor);
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

            GlobalLog.Leave("SafeDeleteContext::CompleteAuthToken() unmanaged CompleteAuthToken()", "errorCode:0x" + errorCode.ToString("x8") + " refContext:" + Logging.ObjectToString(refContext));

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

            return Interop.Secur32.DeleteSecurityContext(ref _handle) == 0;
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

        public unsafe static int QueryContextChannelBinding(SafeDeleteContext phContext, Interop.Secur32.ContextAttribute contextAttribute, Bindings* buffer, SafeFreeContextBufferChannelBinding refHandle)
        {
            return QueryContextChannelBinding_SECURITY(phContext, contextAttribute, buffer, refHandle);
        }

        private unsafe static int QueryContextChannelBinding_SECURITY(SafeDeleteContext phContext, Interop.Secur32.ContextAttribute contextAttribute, Bindings* buffer, SafeFreeContextBufferChannelBinding refHandle)
        {
            int status = (int)Interop.SecurityStatus.InvalidHandle;

            // SCHANNEL only supports SECPKG_ATTR_ENDPOINT_BINDINGS and SECPKG_ATTR_UNIQUE_BINDINGS which
            // map to our enum ChannelBindingKind.Endpoint and ChannelBindingKind.Unique.
            if (contextAttribute != Interop.Secur32.ContextAttribute.EndpointBindings &&
                contextAttribute != Interop.Secur32.ContextAttribute.UniqueBindings)
            {
                return status;
            }

            try
            {
                bool ignore = false;
                phContext.DangerousAddRef(ref ignore);
                status = Interop.Secur32.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
            }
            finally
            {
                phContext.DangerousRelease();
            }

            if (status == 0 && refHandle != null)
            {
                refHandle.Set((*buffer).pBindings);
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
            return Interop.Secur32.FreeContextBuffer(handle) == 0;
        }
    }
}
