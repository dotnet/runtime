// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    //
    // The class maintains the state of the authentication process and the security context.
    // It encapsulates security context and does the real work in authentication and
    // user data encryption with NEGO SSPI package.
    //
    internal static partial class NegotiateStreamPal
    {
        // value should match the Windows sspicli NTE_FAIL value
        // defined in winerror.h
        private const int NTE_FAIL = unchecked((int)0x80090020);

        internal static string QueryContextClientSpecifiedSpn(SafeDeleteContext securityContext)
        {
            throw new PlatformNotSupportedException(SR.net_nego_server_not_supported);
        }

        internal static string QueryContextAuthenticationPackage(SafeDeleteContext securityContext)
        {
            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)securityContext;
            return negoContext.IsNtlmUsed ? NegotiationInfoClass.NTLM : NegotiationInfoClass.Kerberos;
        }

        static byte[] GssWrap(
            SafeGssContextHandle context,
            bool encrypt,
            byte[] buffer,
            int offset,
            int count)
        {
            Debug.Assert((buffer != null) && (buffer.Length > 0), "Invalid input buffer passed to Encrypt");
            Debug.Assert((offset >= 0) && (offset < buffer.Length), "Invalid input offset passed to Encrypt");
            Debug.Assert((count >= 0) && (count <= (buffer.Length - offset)), "Invalid input count passed to Encrypt");

            Interop.NetSecurityNative.GssBuffer encryptedBuffer = default(Interop.NetSecurityNative.GssBuffer);
            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.WrapBuffer(out minorStatus, context, encrypt, buffer, offset, count, ref encryptedBuffer);
                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                }

                return encryptedBuffer.ToByteArray();
            }
            finally
            {
                encryptedBuffer.Dispose();
            }
        }

        private static int GssUnwrap(
            SafeGssContextHandle context,
            byte[] buffer,
            int offset,
            int count)
        {
            Debug.Assert((buffer != null) && (buffer.Length > 0), "Invalid input buffer passed to Decrypt");
            Debug.Assert((offset >= 0) && (offset <= buffer.Length), "Invalid input offset passed to Decrypt");
            Debug.Assert((count >= 0) && (count <= (buffer.Length - offset)), "Invalid input count passed to Decrypt");

            Interop.NetSecurityNative.GssBuffer decryptedBuffer = default(Interop.NetSecurityNative.GssBuffer);
            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.UnwrapBuffer(out minorStatus, context, buffer, offset, count, ref decryptedBuffer);
                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                }

                return decryptedBuffer.Copy(buffer, offset);
            }
            finally
            {
                decryptedBuffer.Dispose();
            }
        }

        private static bool GssInitSecurityContext(
            ref SafeGssContextHandle context,
            SafeGssCredHandle credential,
            bool isNtlm,
            SafeGssNameHandle targetName,
            Interop.NetSecurityNative.GssFlags inFlags,
            byte[] buffer,
            out byte[] outputBuffer,
            out uint outFlags,
            out int isNtlmUsed)
        {
            outputBuffer = null;
            outFlags = 0;

            // EstablishSecurityContext is called multiple times in a session.
            // In each call, we need to pass the context handle from the previous call.
            // For the first call, the context handle will be null.
            if (context == null)
            {
                context = new SafeGssContextHandle();
            }

            Interop.NetSecurityNative.GssBuffer token = default(Interop.NetSecurityNative.GssBuffer);
            Interop.NetSecurityNative.Status status;

            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                status = Interop.NetSecurityNative.InitSecContext(out minorStatus,
                                                          credential,
                                                          ref context,
                                                          isNtlm,
                                                          targetName,
                                                          (uint)inFlags,
                                                          buffer,
                                                          (buffer == null) ? 0 : buffer.Length,
                                                          ref token,
                                                          out outFlags,
                                                          out isNtlmUsed);

                if ((status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE) && (status != Interop.NetSecurityNative.Status.GSS_S_CONTINUE_NEEDED))
                {
                    throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                }

                outputBuffer = token.ToByteArray();
            }
            finally
            {
                token.Dispose();
            }

            return status == Interop.NetSecurityNative.Status.GSS_S_COMPLETE;
        }

        private static SecurityStatusPal EstablishSecurityContext(
          SafeFreeNegoCredentials credential,
          ref SafeDeleteContext context,
          string targetName,
          ContextFlagsPal inFlags,
          SecurityBuffer inputBuffer,
          SecurityBuffer outputBuffer,
          ref ContextFlagsPal outFlags)
        {
            bool isNtlmOnly = credential.IsNtlmOnly;

            if (context == null)
            {
                // Empty target name causes the failure on Linux, hence passing a non-empty string  
                context = isNtlmOnly ? new SafeDeleteNegoContext(credential, credential.UserName) : new SafeDeleteNegoContext(credential, targetName);
            }

            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)context;
            try
            {
                Interop.NetSecurityNative.GssFlags inputFlags = ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(inFlags, isServer: false);
                uint outputFlags;
                int isNtlmUsed;
                SafeGssContextHandle contextHandle = negoContext.GssContext;
                bool done = GssInitSecurityContext(
                   ref contextHandle,
                   credential.GssCredential,
                   isNtlmOnly,
                   negoContext.TargetName,
                   inputFlags,
                   inputBuffer?.token,
                   out outputBuffer.token,
                   out outputFlags,
                   out isNtlmUsed);

                Debug.Assert(outputBuffer.token != null, "Unexpected null buffer returned by GssApi");
                outputBuffer.size = outputBuffer.token.Length;
                outputBuffer.offset = 0;
                outFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop((Interop.NetSecurityNative.GssFlags)outputFlags, isServer: false);
                Debug.Assert(negoContext.GssContext == null || contextHandle == negoContext.GssContext);

                // Save the inner context handle for further calls to NetSecurity
                Debug.Assert(negoContext.GssContext == null || contextHandle == negoContext.GssContext);
                if (null == negoContext.GssContext)
                {
                    negoContext.SetGssContext(contextHandle);
                }

                // Populate protocol used for authentication
                if (done)
                {
                    negoContext.SetAuthenticationPackage(Convert.ToBoolean(isNtlmUsed));
                }

                SecurityStatusPalErrorCode errorCode = done ?
                    (negoContext.IsNtlmUsed && outputBuffer.size > 0 ? SecurityStatusPalErrorCode.OK : SecurityStatusPalErrorCode.CompleteNeeded) :
                    SecurityStatusPalErrorCode.ContinueNeeded;
                return new SecurityStatusPal(errorCode);
            }
            catch (Exception ex)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, ex);
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
        }

        internal static SecurityStatusPal InitializeSecurityContext(
            SafeFreeCredentials credentialsHandle,
            ref SafeDeleteContext securityContext,
            string spn,
            ContextFlagsPal requestedContextFlags,
            SecurityBuffer[] inSecurityBufferArray,
            SecurityBuffer outSecurityBuffer,
            ref ContextFlagsPal contextFlags)
        {
            // TODO (Issue #3718): The second buffer can contain a channel binding which is not supported
            if ((null != inSecurityBufferArray) && (inSecurityBufferArray.Length > 1))
            {
                throw new PlatformNotSupportedException(SR.net_nego_channel_binding_not_supported);
            }

            SafeFreeNegoCredentials negoCredentialsHandle = (SafeFreeNegoCredentials)credentialsHandle;

            if (negoCredentialsHandle.IsDefault && string.IsNullOrEmpty(spn))
            {
                throw new PlatformNotSupportedException(SR.net_nego_not_supported_empty_target_with_defaultcreds);
            }

            SecurityStatusPal status = EstablishSecurityContext(
                negoCredentialsHandle,
                ref securityContext,
                spn,
                requestedContextFlags,
                ((inSecurityBufferArray != null && inSecurityBufferArray.Length != 0) ? inSecurityBufferArray[0] : null),
                outSecurityBuffer,
                ref contextFlags);

            // Confidentiality flag should not be set if not requested
            if (status.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded)
            {
                ContextFlagsPal mask = ContextFlagsPal.Confidentiality;
                if ((requestedContextFlags & mask) != (contextFlags & mask))
                {
                    throw new PlatformNotSupportedException(SR.net_nego_protection_level_not_supported);
                }
            }

            return status;
        }

        internal static SecurityStatusPal AcceptSecurityContext(
            SafeFreeCredentials credentialsHandle,
            ref SafeDeleteContext securityContext,
            ContextFlagsPal requestedContextFlags,
            SecurityBuffer[] inSecurityBufferArray,
            SecurityBuffer outSecurityBuffer,
            ref ContextFlagsPal contextFlags)
        {
            throw new PlatformNotSupportedException(SR.net_nego_server_not_supported);
        }

        internal static Win32Exception CreateExceptionFromError(SecurityStatusPal statusCode)
        {
            return new Win32Exception(NTE_FAIL, (statusCode.Exception != null) ? statusCode.Exception.Message : statusCode.ErrorCode.ToString());
        }

        internal static int QueryMaxTokenSize(string package)
        {
            // This value is not used on Unix
            return 0;
        }
        
        internal static SafeFreeCredentials AcquireDefaultCredential(string package, bool isServer)
        {
            return AcquireCredentialsHandle(package, isServer, new NetworkCredential(string.Empty, string.Empty, string.Empty));
        }

        internal static SafeFreeCredentials AcquireCredentialsHandle(string package, bool isServer, NetworkCredential credential)
        {
            if (isServer)
            {
                throw new PlatformNotSupportedException(SR.net_nego_server_not_supported);
            }

            bool isEmptyCredential = string.IsNullOrWhiteSpace(credential.UserName) ||
                                     string.IsNullOrWhiteSpace(credential.Password);
            bool ntlmOnly = string.Equals(package, NegotiationInfoClass.NTLM, StringComparison.OrdinalIgnoreCase);
            if (ntlmOnly && isEmptyCredential)
            {
                // NTLM authentication is not possible with default credentials which are no-op 
                throw new PlatformNotSupportedException(SR.net_ntlm_not_possible_default_cred);
            }

            try
            {
                return isEmptyCredential ?
                    new SafeFreeNegoCredentials(false, string.Empty, string.Empty, string.Empty) :
                    new SafeFreeNegoCredentials(ntlmOnly, credential.UserName, credential.Password, credential.Domain);
            }
            catch(Exception ex)
            {
                throw new Win32Exception(NTE_FAIL, ex.Message);
            }
        }

        internal static SecurityStatusPal CompleteAuthToken(
            ref SafeDeleteContext securityContext,
            SecurityBuffer[] inSecurityBufferArray)
        {
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }

        internal static int Encrypt(
            SafeDeleteContext securityContext,
            byte[] buffer,
            int offset,
            int count,
            bool isConfidential,
            bool isNtlm,
            ref byte[] output,
            uint sequenceNumber)
        {
            SafeDeleteNegoContext gssContext = (SafeDeleteNegoContext) securityContext;
            byte[] tempOutput = GssWrap(gssContext.GssContext, isConfidential, buffer, offset, count);

            // Create space for prefixing with the length
            const int prefixLength = 4;
            output = new byte[tempOutput.Length + prefixLength];
            Array.Copy(tempOutput, 0, output, prefixLength, tempOutput.Length);
            int resultSize = tempOutput.Length;
            unchecked
            {
                output[0] = (byte)((resultSize) & 0xFF);
                output[1] = (byte)(((resultSize) >> 8) & 0xFF);
                output[2] = (byte)(((resultSize) >> 16) & 0xFF);
                output[3] = (byte)(((resultSize) >> 24) & 0xFF);
            }

            return resultSize + 4;
        }

        internal static int Decrypt(
            SafeDeleteContext securityContext,
            byte[] buffer,
            int offset,
            int count,
            bool isConfidential,
            bool isNtlm,
            out int newOffset,
            uint sequenceNumber)
        {
            if (offset < 0 || offset > (buffer == null ? 0 : buffer.Length))
            {
                NetEventSource.Fail(securityContext, "Argument 'offset' out of range");
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > (buffer == null ? 0 : buffer.Length - offset))
            {
                NetEventSource.Fail(securityContext, "Argument 'count' out of range.");
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            newOffset = offset;
            return GssUnwrap(((SafeDeleteNegoContext)securityContext).GssContext, buffer, offset, count);
        }

        internal static int VerifySignature(SafeDeleteContext securityContext, byte[] buffer, int offset, int count)
        {
            if (offset < 0 || offset > (buffer == null ? 0 : buffer.Length))
            {
                NetEventSource.Fail(securityContext, "Argument 'offset' out of range");
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > (buffer == null ? 0 : buffer.Length - offset))
            {
                NetEventSource.Fail(securityContext, "Argument 'count' out of range.");
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return GssUnwrap(((SafeDeleteNegoContext)securityContext).GssContext, buffer, offset, count);
        }

        internal static int MakeSignature(SafeDeleteContext securityContext, byte[] buffer, int offset, int count, ref byte[] output)
        {
            SafeDeleteNegoContext gssContext = (SafeDeleteNegoContext)securityContext;
            byte[] tempOutput = GssWrap(gssContext.GssContext, false, buffer, offset, count);
            // Create space for prefixing with the length
            const int prefixLength = 4;
            output = new byte[tempOutput.Length + prefixLength];
            Array.Copy(tempOutput, 0, output, prefixLength, tempOutput.Length);
            int resultSize = tempOutput.Length;
            unchecked
            {
                output[0] = (byte)((resultSize) & 0xFF);
                output[1] = (byte)(((resultSize) >> 8) & 0xFF);
                output[2] = (byte)(((resultSize) >> 16) & 0xFF);
                output[3] = (byte)(((resultSize) >> 24) & 0xFF);
            }

            return resultSize + 4;
        }
    }
}
