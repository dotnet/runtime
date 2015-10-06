// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace System.Net
{
    // For SSL connections:
    internal partial class SSPISecureChannelType : SSPIInterface
    {
        public const string MSSecurityPackage = "Microsoft Unified Security Protocol Provider";

        public const Interop.Secur32.ContextFlags RequiredFlags = Interop.Secur32.ContextFlags.ReplayDetect | Interop.Secur32.ContextFlags.SequenceDetect |
                                                                  Interop.Secur32.ContextFlags.Confidentiality | Interop.Secur32.ContextFlags.AllocateMemory;

        public const Interop.Secur32.ContextFlags ServerRequiredFlags = RequiredFlags | Interop.Secur32.ContextFlags.AcceptStream;

        private static volatile SecurityPackageInfoClass[] s_securityPackages;

        public void VerifyPackageInfo()
        {
            bool found = false;

            EnumerateSecurityPackages();

            if (s_securityPackages != null)
            {
                foreach (var package in s_securityPackages)
                {
                    if (string.Compare(package.Name, MSSecurityPackage, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_sspi_security_package_not_found, MSSecurityPackage));
                }

                throw new NotSupportedException(SR.net_securitypackagesupport);
            }
        }

        public Exception GetException(SecurityStatus status)
        {
            return new Win32Exception((int)status);
        }

        public SecurityStatus AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool remoteCertRequired)
        {
            Interop.Secur32.ContextFlags outFlags = Interop.Secur32.ContextFlags.Zero;
            var retStatus = SafeDeleteContext.AcceptSecurityContext(
                                        ref credential,
                                        ref context,
                                        ServerRequiredFlags | (remoteCertRequired ? Interop.Secur32.ContextFlags.MutualAuth : Interop.Secur32.ContextFlags.Zero),
                                        Interop.Secur32.Endianness.Native,
                                        inputBuffer,
                                        null,
                                        outputBuffer,
                                        ref outFlags
                                        );

            return MapToSecurityStatus((Interop.SecurityStatus)retStatus);
        }

        public SecurityStatus InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer)
        {
            Interop.Secur32.ContextFlags outFlags = Interop.Secur32.ContextFlags.Zero;

            var retStatus = (SecurityStatus)SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, RequiredFlags | Interop.Secur32.ContextFlags.InitManualCredValidation, Interop.Secur32.Endianness.Native, inputBuffer, null, outputBuffer, ref outFlags);

            return MapToSecurityStatus((Interop.SecurityStatus)retStatus);
        }

        public SecurityStatus InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer)
        {
            Interop.Secur32.ContextFlags outFlags = Interop.Secur32.ContextFlags.Zero;
            var retStatus = (SecurityStatus)SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, RequiredFlags | Interop.Secur32.ContextFlags.InitManualCredValidation, Interop.Secur32.Endianness.Native, null, inputBuffers, outputBuffer, ref outFlags);
            return MapToSecurityStatus((Interop.SecurityStatus)retStatus);
        }

        public SafeFreeCredentials AcquireCredentialsHandle(X509Certificate certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            Interop.Secur32.SecureCredential secureCredential = CreateSecureCredential(Interop.Secur32.SecureCredential.CurrentVersion, certificate, protocols, policy, isServer);
            // First try without impersonation, if it fails, then try the process account.
            // I.E. We don't know which account the certificate context was created under.
            try
            {
                //
                // For app-compat we want to ensure the credential are accessed under >>process<< acount.
                //
                return WindowsIdentity.RunImpersonated<SafeFreeCredentials>(SafeAccessTokenHandle.InvalidHandle, () =>
                {
                    return AcquireCredentialsHandle(MSSecurityPackage, isServer, secureCredential);
                });
            }
            catch
            {
                return AcquireCredentialsHandle(MSSecurityPackage, isServer, secureCredential);
            }
        }

        public SecurityStatus EncryptMessage(SafeDeleteContext securityContext, byte[] buffer, int size, int headerSize, int trailerSize, out int resultSize)
        {
            resultSize = 0;

            // Encryption using SCHANNEL requires 4 buffers: header, payload, trailer, empty.
            SecurityBuffer[] securityBuffer = new SecurityBuffer[4];

            securityBuffer[0] = new SecurityBuffer(buffer, 0, headerSize, SecurityBufferType.Header);
            securityBuffer[1] = new SecurityBuffer(buffer, headerSize, size, SecurityBufferType.Data);
            securityBuffer[2] = new SecurityBuffer(buffer, headerSize + size, trailerSize, SecurityBufferType.Trailer);
            securityBuffer[3] = new SecurityBuffer(null, SecurityBufferType.Empty);

            SecurityStatus secStatus = EncryptDecryptHelper(OP.Encrypt, securityContext, securityBuffer, 0);

            if (secStatus == 0)
            {
                // The full buffer may not be used.
                resultSize = securityBuffer[0].size + securityBuffer[1].size + securityBuffer[2].size;
                GlobalLog.Leave("SecureChannel#" + Logging.HashString(this) + "::Encrypt OK", "data size:" + resultSize.ToString());
            }

            return secStatus;
        }

        public SecurityStatus DecryptMessage(SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count)
        {
            // Decryption using SCHANNEL requires four buffers.
            SecurityBuffer[] decspc = new SecurityBuffer[4];
            decspc[0] = new SecurityBuffer(buffer, offset, count, SecurityBufferType.Data);
            decspc[1] = new SecurityBuffer(null, SecurityBufferType.Empty);
            decspc[2] = new SecurityBuffer(null, SecurityBufferType.Empty);
            decspc[3] = new SecurityBuffer(null, SecurityBufferType.Empty);

            SecurityStatus secStatus = EncryptDecryptHelper(OP.Decrypt, securityContext, decspc, 0);

            count = 0;
            for (int i = 0; i < decspc.Length; i++)
            {
                // Successfully decoded data and placed it at the following position in the buffer,
                if ((secStatus == SecurityStatus.OK && decspc[i].type == SecurityBufferType.Data)
                    // or we failed to decode the data, here is the encoded data.
                    || (secStatus != SecurityStatus.OK && decspc[i].type == SecurityBufferType.Extra))
                {
                    offset = decspc[i].offset;
                    count = decspc[i].size;
                    break;
                }
            }

            return secStatus;
        }

        public unsafe int QueryContextChannelBinding(SafeDeleteContext phContext, ChannelBindingKind attribute, out SafeFreeContextBufferChannelBinding refHandle)
        {
            refHandle = SafeFreeContextBufferChannelBinding.CreateEmptyHandle();

            // Bindings is on the stack, so there's no need for a fixed block.
            Bindings bindings = new Bindings();
            int errorCode = SafeFreeContextBufferChannelBinding.QueryContextChannelBinding(phContext, (Interop.Secur32.ContextAttribute)attribute, &bindings, refHandle);

            if (errorCode != 0)
            {
                GlobalLog.Leave("QueryContextChannelBinding", "ERROR = " + ErrorDescription(errorCode));
                refHandle = null;
            }

            return errorCode;
        }

        public int QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes)
        {
            int errorCode;
            streamSizes = QueryContextAttributes(securityContext, Interop.Secur32.ContextAttribute.StreamSizes, out errorCode) as StreamSizes;
            return errorCode;
        }

        public int QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo connectionInfo)
        {
            int errorCode;
            connectionInfo = QueryContextAttributes(securityContext, Interop.Secur32.ContextAttribute.ConnectionInfo, out errorCode) as SslConnectionInfo;
            return errorCode;
        }

        public int QueryContextRemoteCertificate(SafeDeleteContext securityContext, out SafeFreeCertContext remoteCert)
        {
            int errorCode;
            remoteCert = QueryContextAttributes(securityContext, Interop.Secur32.ContextAttribute.RemoteCertificate, out errorCode) as SafeFreeCertContext;
            return errorCode;
        }

        public int QueryContextIssuerList(SafeDeleteContext securityContext, out Object issuerList)
        {
            int errorCode;
            issuerList = QueryContextAttributes(securityContext, Interop.Secur32.ContextAttribute.IssuerListInfoEx, out errorCode);
            return errorCode;
        }

        #region Private Methods

        private enum OP
        {
            Encrypt = 1,
            Decrypt
        }

        private SecurityStatus MapToSecurityStatus(Interop.SecurityStatus secStatus)
        {
            SecurityStatus retStatus;

            if (SecurityStatus.TryParse(secStatus.ToString(), out retStatus))
            {
                return retStatus;
            }
            else
            {
                throw new Win32Exception("Coulbd not map from Interop.SecurityStatus to SecurityStatus. Interop.SecurityStatus value:" + secStatus.ToString());
            }
        }

        private void EnumerateSecurityPackages()
        {
            GlobalLog.Enter("EnumerateSecurityPackages");
            if (s_securityPackages == null)
            {
                lock (this)
                {
                    if (s_securityPackages == null)
                    {
                        int moduleCount = 0;
                        SafeFreeContextBuffer arrayBaseHandle = null;
                        try
                        {
                            int errorCode = SafeFreeContextBuffer.EnumeratePackages(out moduleCount, out arrayBaseHandle);

                            GlobalLog.Print("SSPIWrapper::arrayBase: " + (arrayBaseHandle.DangerousGetHandle().ToString("x")));

                            if (errorCode != 0)
                            {
                                throw new Win32Exception(errorCode);
                            }

                            SecurityPackageInfoClass[] securityPackagesList = new SecurityPackageInfoClass[moduleCount];

                            if (Logging.On)
                            {
                                Logging.PrintInfo(Logging.Web, SR.net_log_sspi_enumerating_security_packages);
                            }

                            for (int i = 0; i < moduleCount; i++)
                            {
                                securityPackagesList[i] = new SecurityPackageInfoClass(arrayBaseHandle, i);

                                if (Logging.On)
                                {
                                    Logging.PrintInfo(Logging.Web, "    " + securityPackagesList[i].Name);
                                }
                            }

                            s_securityPackages = securityPackagesList;
                        }
                        finally
                        {
                            if (arrayBaseHandle != null)
                            {
                                arrayBaseHandle.Dispose();
                            }
                        }
                    }
                }
            }

            GlobalLog.Leave("EnumerateSecurityPackages");
        }

        private SafeFreeCredentials AcquireCredentialsHandle(string package, bool isServer, Interop.Secur32.SecureCredential scc)
        {
            GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#3(): using " + package);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "AcquireCredentialsHandle(" +
                    "package = " + package + ", " +
                    "IsInBoundCred  = " + isServer + ", " +
                    "scc     = " + scc + ")");
            }

            SafeFreeCredentials outCredential = null;
            int errorCode = AcquireCredentialsHandle(
                                            package,
                                            isServer,
                                            ref scc,
                                            out outCredential
                                            );
            if (errorCode != 0)
            {
#if TRACE_VERBOSE
                GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#3(): error " + Interop.MapSecurityStatus((uint)errorCode));
#endif
                if (Logging.On) Logging.PrintError(Logging.Web, SR.Format(SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                throw new Win32Exception(errorCode);
            }

#if TRACE_VERBOSE
            GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#3(): cred handle = " + outCredential.ToString());
#endif
            return outCredential;
        }

        private int AcquireCredentialsHandle(string moduleName, bool IsInBoundCred, ref Interop.Secur32.SecureCredential authdata, out SafeFreeCredentials outCredential)
        {
            Interop.Secur32.CredentialUse intent = IsInBoundCred ? Interop.Secur32.CredentialUse.Inbound : Interop.Secur32.CredentialUse.Outbound;
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, intent, ref authdata, out outCredential);
        }

        private Interop.Secur32.SecureCredential CreateSecureCredential(int version, X509Certificate certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            Interop.Secur32.SecureCredential.Flags flags = Interop.Secur32.SecureCredential.Flags.Zero;

            if (!isServer)
            {
                flags = Interop.Secur32.SecureCredential.Flags.ValidateManual | Interop.Secur32.SecureCredential.Flags.NoDefaultCred;

                if ((protocols.HasFlag(SslProtocols.Tls) || protocols.HasFlag(SslProtocols.Tls11) || protocols.HasFlag(SslProtocols.Tls12))
                     && (policy != EncryptionPolicy.AllowNoEncryption) && (policy != EncryptionPolicy.NoEncryption))
                {
                    flags |= Interop.Secur32.SecureCredential.Flags.UseStrongCrypto;
                }
            }

            var credential = new Interop.Secur32.SecureCredential()
            {
                rootStore = IntPtr.Zero,
                phMappers = IntPtr.Zero,
                palgSupportedAlgs = IntPtr.Zero,
                certContextArray = IntPtr.Zero,
                cCreds = 0,
                cMappers = 0,
                cSupportedAlgs = 0,
                dwSessionLifespan = 0,
                reserved = 0
            };

            if (policy == EncryptionPolicy.RequireEncryption)
            {
                // Prohibit null encryption cipher.
                credential.dwMinimumCipherStrength = 0;
                credential.dwMaximumCipherStrength = 0;
            }
            else if (policy == EncryptionPolicy.AllowNoEncryption)
            {
                // Allow null encryption cipher in addition to other ciphers.
                credential.dwMinimumCipherStrength = -1;
                credential.dwMaximumCipherStrength = 0;
            }
            else if (policy == EncryptionPolicy.NoEncryption)
            {
                // Suppress all encryption and require null encryption cipher only
                credential.dwMinimumCipherStrength = -1;
                credential.dwMaximumCipherStrength = -1;
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.net_invalid_enum, "EncryptionPolicy"), "policy");
            }

            int _protocolFlags = 0;

            if (isServer)
            {
                _protocolFlags = ((int)protocols & Interop.SChannel.ServerProtocolMask);
            }
            else
            {
                _protocolFlags = ((int)protocols & Interop.SChannel.ClientProtocolMask);
            }

            credential.version = version;
            credential.dwFlags = flags;
            credential.grbitEnabledProtocols = _protocolFlags;

            if (certificate != null)
            {
                credential.certContextArray = certificate.Handle;
                credential.cCreds = 1;
            }

            return credential;
        }

        private unsafe SecurityStatus EncryptDecryptHelper(OP op, SafeDeleteContext context, SecurityBuffer[] input, uint sequenceNumber)
        {
            Interop.Secur32.SecurityBufferDescriptor sdcInOut = new Interop.Secur32.SecurityBufferDescriptor(input.Length);
            var unmanagedBuffer = new Interop.Secur32.SecurityBufferStruct[input.Length];

            fixed (Interop.Secur32.SecurityBufferStruct* unmanagedBufferPtr = unmanagedBuffer)
            {
                sdcInOut.UnmanagedPointer = unmanagedBufferPtr;
                GCHandle[] pinnedBuffers = new GCHandle[input.Length];
                byte[][] buffers = new byte[input.Length][];
                try
                {
                    for (int i = 0; i < input.Length; i++)
                    {
                        SecurityBuffer iBuffer = input[i];
                        unmanagedBuffer[i].count = iBuffer.size;
                        unmanagedBuffer[i].type = iBuffer.type;
                        if (iBuffer.token == null || iBuffer.token.Length == 0)
                        {
                            unmanagedBuffer[i].token = IntPtr.Zero;
                        }
                        else
                        {
                            pinnedBuffers[i] = GCHandle.Alloc(iBuffer.token, GCHandleType.Pinned);
                            unmanagedBuffer[i].token = Marshal.UnsafeAddrOfPinnedArrayElement(iBuffer.token, iBuffer.offset);
                            buffers[i] = iBuffer.token;
                        }
                    }

                    // The result is written in the input Buffer passed as type=BufferType.Data.
                    int errorCode;
                    switch (op)
                    {
                        case OP.Encrypt:
                            errorCode = EncryptMessage(context, sdcInOut, sequenceNumber);
                            break;

                        case OP.Decrypt:
                            errorCode = DecryptMessage(context, sdcInOut, sequenceNumber);
                            break;

                        default: throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
                    }

                    // Marshalling back returned sizes / data.
                    for (int i = 0; i < input.Length; i++)
                    {
                        SecurityBuffer iBuffer = input[i];
                        iBuffer.size = unmanagedBuffer[i].count;
                        iBuffer.type = unmanagedBuffer[i].type;

                        if (iBuffer.size == 0)
                        {
                            iBuffer.offset = 0;
                            iBuffer.token = null;
                        }
                        else
                            checked
                            {
                                // Find the buffer this is inside of.  Usually they all point inside buffer 0.
                                int j;
                                for (j = 0; j < input.Length; j++)
                                {
                                    if (buffers[j] == null)
                                    {
                                        continue;
                                    }

                                    byte* bufferAddress = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(buffers[j], 0);
                                    if ((byte*)unmanagedBuffer[i].token >= bufferAddress &&
                                        (byte*)unmanagedBuffer[i].token + iBuffer.size <= bufferAddress + buffers[j].Length)
                                    {
                                        iBuffer.offset = (int)((byte*)unmanagedBuffer[i].token - bufferAddress);
                                        iBuffer.token = buffers[j];
                                        break;
                                    }
                                }

                                if (j >= input.Length)
                                {
                                    GlobalLog.Assert("SSPIWrapper::EncryptDecryptHelper", "Output buffer out of range.");
                                    iBuffer.size = 0;
                                    iBuffer.offset = 0;
                                    iBuffer.token = null;
                                }
                            }

                        // Backup validate the new sizes.
                        GlobalLog.Assert(iBuffer.offset >= 0 && iBuffer.offset <= (iBuffer.token == null ? 0 : iBuffer.token.Length), "SSPIWrapper::EncryptDecryptHelper|'offset' out of range.  [{0}]", iBuffer.offset);
                        GlobalLog.Assert(iBuffer.size >= 0 && iBuffer.size <= (iBuffer.token == null ? 0 : iBuffer.token.Length - iBuffer.offset), "SSPIWrapper::EncryptDecryptHelper|'size' out of range.  [{0}]", iBuffer.size);
                    }

                    if (errorCode != 0 && Logging.On)
                    {
                        if (errorCode == 0x90321)
                        {
                            Logging.PrintError(Logging.Web, SR.Format(SR.net_log_operation_returned_something, op, "SEC_I_RENEGOTIATE"));
                        }
                        else
                        {
                            Logging.PrintError(Logging.Web, SR.Format(SR.net_log_operation_failed_with_error, op, String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                        }
                    }
                    return MapToSecurityStatus((Interop.SecurityStatus)errorCode);
                }
                finally
                {
                    for (int i = 0; i < pinnedBuffers.Length; ++i)
                    {
                        if (pinnedBuffers[i].IsAllocated)
                        {
                            pinnedBuffers[i].Free();
                        }
                    }
                }
            }
        }

        private int EncryptMessage(SafeDeleteContext context, Interop.Secur32.SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)Interop.SecurityStatus.InvalidHandle;

            try
            {
                bool ignore = false;
                context.DangerousAddRef(ref ignore);
                status = Interop.Secur32.EncryptMessage(ref context._handle, 0, inputOutput, sequenceNumber);
                return status;
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        private unsafe int DecryptMessage(SafeDeleteContext context, Interop.Secur32.SecurityBufferDescriptor inputOutput,
            uint sequenceNumber)
        {
            int status = (int)Interop.SecurityStatus.InvalidHandle;

            try
            {
                bool ignore = false;
                context.DangerousAddRef(ref ignore);
                status = Interop.Secur32.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, null);
                return status;
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        private object QueryContextAttributes(SafeDeleteContext securityContext, Interop.Secur32.ContextAttribute contextAttribute, out int errorCode)
        {
            GlobalLog.Enter("QueryContextAttributes", contextAttribute.ToString());

            int nativeBlockSize = IntPtr.Size;
            Type handleType = null;

            switch (contextAttribute)
            {
                case Interop.Secur32.ContextAttribute.Sizes:
                    nativeBlockSize = SecSizes.SizeOf;
                    break;
                case Interop.Secur32.ContextAttribute.StreamSizes:
                    nativeBlockSize = StreamSizes.SizeOf;
                    break;

                case Interop.Secur32.ContextAttribute.Names:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.Secur32.ContextAttribute.PackageInfo:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.Secur32.ContextAttribute.NegotiationInfo:
                    handleType = typeof(SafeFreeContextBuffer);
                    nativeBlockSize = Marshal.SizeOf<NegotiationInfo>();
                    break;

                case Interop.Secur32.ContextAttribute.ClientSpecifiedSpn:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.Secur32.ContextAttribute.RemoteCertificate:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case Interop.Secur32.ContextAttribute.LocalCertificate:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case Interop.Secur32.ContextAttribute.IssuerListInfoEx:
                    nativeBlockSize = Marshal.SizeOf<Interop.Secur32.IssuerListInfoEx>();
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.Secur32.ContextAttribute.ConnectionInfo:
                    nativeBlockSize = Marshal.SizeOf<SslConnectionInfo>();
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.net_invalid_enum, "ContextAttribute"), "contextAttribute");
            }

            SafeHandle SspiHandle = null;
            object attribute = null;

            try
            {
                byte[] nativeBuffer = new byte[nativeBlockSize];
                errorCode = QueryContextAttributes(securityContext, contextAttribute, nativeBuffer, handleType, out SspiHandle);
                if (errorCode != 0)
                {
                    GlobalLog.Leave("Win32:QueryContextAttributes", "ERROR = " + ErrorDescription(errorCode));
                    return null;
                }

                switch (contextAttribute)
                {
                    case Interop.Secur32.ContextAttribute.Sizes:
                        attribute = new SecSizes(nativeBuffer);
                        break;

                    case Interop.Secur32.ContextAttribute.StreamSizes:
                        attribute = new StreamSizes(nativeBuffer);
                        break;

                    case Interop.Secur32.ContextAttribute.Names:
                        attribute = Marshal.PtrToStringUni(SspiHandle.DangerousGetHandle());
                        break;

                    case Interop.Secur32.ContextAttribute.PackageInfo:
                        attribute = new SecurityPackageInfoClass(SspiHandle, 0);
                        break;

                    case Interop.Secur32.ContextAttribute.NegotiationInfo:
                        unsafe
                        {
                            fixed (void* ptr = nativeBuffer)
                            {
                                attribute = new NegotiationInfoClass(SspiHandle, Marshal.ReadInt32(new IntPtr(ptr), NegotiationInfo.NegotiationStateOffest));
                            }
                        }
                        break;

                    case Interop.Secur32.ContextAttribute.ClientSpecifiedSpn:
                        attribute = Marshal.PtrToStringUni(SspiHandle.DangerousGetHandle());
                        break;

                    case Interop.Secur32.ContextAttribute.LocalCertificate:
                        goto case Interop.Secur32.ContextAttribute.RemoteCertificate;
                    case Interop.Secur32.ContextAttribute.RemoteCertificate:
                        attribute = SspiHandle;
                        SspiHandle = null;
                        break;

                    case Interop.Secur32.ContextAttribute.IssuerListInfoEx:
                        attribute = new Interop.Secur32.IssuerListInfoEx(SspiHandle, nativeBuffer);
                        SspiHandle = null;
                        break;

                    case Interop.Secur32.ContextAttribute.ConnectionInfo:
                        attribute = new SslConnectionInfo(nativeBuffer);
                        break;
                    default:
                        // Will return null.
                        break;
                }
            }
            finally
            {
                if (SspiHandle != null)
                {
                    SspiHandle.Dispose();
                }
            }
            GlobalLog.Leave("QueryContextAttributes", Logging.ObjectToString(attribute));
            return attribute;
        }

        private unsafe int QueryContextAttributes(SafeDeleteContext phContext, Interop.Secur32.ContextAttribute attribute, byte[] buffer, Type handleType, out SafeHandle refHandle)
        {
            refHandle = null;
            if (handleType != null)
            {
                if (handleType == typeof(SafeFreeContextBuffer))
                {
                    refHandle = SafeFreeContextBuffer.CreateEmptyHandle();
                }
                else if (handleType == typeof(SafeFreeCertContext))
                {
                    refHandle = new SafeFreeCertContext();
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.SSPIInvalidHandleType, handleType.FullName), "handleType");
                }
            }
            fixed (byte* bufferPtr = buffer)
            {
                return SafeFreeContextBuffer.QueryContextAttributes(phContext, attribute, bufferPtr, refHandle);
            }
        }

        private string ErrorDescription(int errorCode)
        {
            if (errorCode == -1)
            {
                return "An exception when invoking Win32 API";
            }

            switch ((Interop.SecurityStatus)errorCode)
            {
                case Interop.SecurityStatus.InvalidHandle:
                    return "Invalid handle";
                case Interop.SecurityStatus.InvalidToken:
                    return "Invalid token";
                case Interop.SecurityStatus.ContinueNeeded:
                    return "Continue needed";
                case Interop.SecurityStatus.IncompleteMessage:
                    return "Message incomplete";
                case Interop.SecurityStatus.WrongPrincipal:
                    return "Wrong principal";
                case Interop.SecurityStatus.TargetUnknown:
                    return "Target unknown";
                case Interop.SecurityStatus.PackageNotFound:
                    return "Package not found";
                case Interop.SecurityStatus.BufferNotEnough:
                    return "Buffer not enough";
                case Interop.SecurityStatus.MessageAltered:
                    return "Message altered";
                case Interop.SecurityStatus.UntrustedRoot:
                    return "Untrusted root";
                default:
                    return "0x" + errorCode.ToString("x", NumberFormatInfo.InvariantInfo);
            }
        }

        #endregion
    }

    #region structures

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityPackageInfo
    {
        // see SecPkgInfoW in <sspi.h>
        internal int Capabilities;
        internal short Version;
        internal short RPCID;
        internal int MaxToken;
        internal IntPtr Name;
        internal IntPtr Comment;

        internal static readonly int Size = Marshal.SizeOf<SecurityPackageInfo>();
        internal static readonly int NameOffest = (int)Marshal.OffsetOf<SecurityPackageInfo>("Name");
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Bindings
    {
        // see SecPkgContext_Bindings in <sspi.h>
        internal int BindingsLength;
        internal IntPtr pBindings;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NegotiationInfo
    {
        // see SecPkgContext_NegotiationInfoW in <sspi.h>

        // [MarshalAs(UnmanagedType.LPStruct)] internal SecurityPackageInfo PackageInfo;
        internal IntPtr PackageInfo;
        internal uint NegotiationState;
        internal static readonly int Size = Marshal.SizeOf<NegotiationInfo>();
        internal static readonly int NegotiationStateOffest = (int)Marshal.OffsetOf<NegotiationInfo>("NegotiationState");
    }

    #endregion
}