// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Text;
using System.Threading;
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
            ChannelBinding channelBinding,
            SafeGssNameHandle targetName,
            Interop.NetSecurityNative.GssFlags inFlags,
            byte[] buffer,
            out byte[] outputBuffer,
            out uint outFlags,
            out bool isNtlmUsed)
        {
            // If a TLS channel binding token (cbt) is available then get the pointer
            // to the application specific data.
            IntPtr cbtAppData = IntPtr.Zero;
            int cbtAppDataSize = 0;
            if (channelBinding != null)
            {
                int appDataOffset = Marshal.SizeOf<SecChannelBindings>();
                Debug.Assert(appDataOffset < channelBinding.Size);
                cbtAppData = channelBinding.DangerousGetHandle() + appDataOffset;
                cbtAppDataSize = channelBinding.Size - appDataOffset;
            }

            outputBuffer = null;
            outFlags = 0;

            // EstablishSecurityContext is called multiple times in a session.
            // In each call, we need to pass the context handle from the previous call.
            // For the first call, the context handle will be null.
            bool newContext = false;
            if (context == null)
            {
                newContext = true;
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
                                                          cbtAppData,
                                                          cbtAppDataSize,
                                                          targetName,
                                                          (uint)inFlags,
                                                          buffer,
                                                          (buffer == null) ? 0 : buffer.Length,
                                                          ref token,
                                                          out outFlags,
                                                          out isNtlmUsed);

                if ((status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE) &&
                    (status != Interop.NetSecurityNative.Status.GSS_S_CONTINUE_NEEDED))
                {
                    if (newContext)
                    {
                        context.Dispose();
                        context = null;
                    }
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

        private static bool GssAcceptSecurityContext(
            ref SafeGssContextHandle context,
            byte[] buffer,
            out byte[] outputBuffer,
            out uint outFlags)
        {
            bool newContext = false;
            if (context == null)
            {
                newContext = true;
                context = new SafeGssContextHandle();
            }

            Interop.NetSecurityNative.GssBuffer token = default(Interop.NetSecurityNative.GssBuffer);
            Interop.NetSecurityNative.Status status;

            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                status = Interop.NetSecurityNative.AcceptSecContext(out minorStatus,
                                                                    ref context,
                                                                    buffer,
                                                                    buffer?.Length ?? 0,
                                                                    ref token,
                                                                    out outFlags);

                if ((status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE) &&
                    (status != Interop.NetSecurityNative.Status.GSS_S_CONTINUE_NEEDED))
                {
                    if (newContext)
                    {
                        context.Dispose();
                        context = null;
                    }
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

        private static string GssGetUser(
            ref SafeGssContextHandle context)
        {
            Interop.NetSecurityNative.GssBuffer token = default(Interop.NetSecurityNative.GssBuffer);

            try
            {
                Interop.NetSecurityNative.Status status
                    = Interop.NetSecurityNative.GetUser(out var minorStatus,
                                                        context,
                                                        ref token);

                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                }

                return Encoding.UTF8.GetString(token.ToByteArray());
            }
            finally
            {
                token.Dispose();
            }
        }

        private static SecurityStatusPal EstablishSecurityContext(
          SafeFreeNegoCredentials credential,
          ref SafeDeleteContext context,
          ChannelBinding channelBinding,
          string targetName,
          ContextFlagsPal inFlags,
          byte[] incomingBlob,
          ref byte[] resultBuffer,
          ref ContextFlagsPal outFlags)
        {
            bool isNtlmOnly = credential.IsNtlmOnly;

            if (context == null)
            {
                if (NetEventSource.IsEnabled)
                {
                    string protocol = isNtlmOnly ? "NTLM" : "SPNEGO";
                    NetEventSource.Info(null, $"requested protocol = {protocol}, target = {targetName}");
                }

                context = new SafeDeleteNegoContext(credential, targetName);
            }

            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)context;
            try
            {
                Interop.NetSecurityNative.GssFlags inputFlags =
                    ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(inFlags, isServer: false);
                uint outputFlags;
                bool isNtlmUsed;
                SafeGssContextHandle contextHandle = negoContext.GssContext;
                bool done = GssInitSecurityContext(
                   ref contextHandle,
                   credential.GssCredential,
                   isNtlmOnly,
                   channelBinding,
                   negoContext.TargetName,
                   inputFlags,
                   incomingBlob,
                   out resultBuffer,
                   out outputFlags,
                   out isNtlmUsed);

                if (done)
                {
                    if (NetEventSource.IsEnabled)
                    {
                        string protocol = isNtlmOnly ? "NTLM" : isNtlmUsed ? "SPNEGO-NTLM" : "SPNEGO-Kerberos";
                        NetEventSource.Info(null, $"actual protocol = {protocol}");
                    }

                    // Populate protocol used for authentication
                    negoContext.SetAuthenticationPackage(isNtlmUsed);
                }

                Debug.Assert(resultBuffer != null, "Unexpected null buffer returned by GssApi");
                outFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(
                    (Interop.NetSecurityNative.GssFlags)outputFlags, isServer: false);
                Debug.Assert(negoContext.GssContext == null || contextHandle == negoContext.GssContext);

                // Save the inner context handle for further calls to NetSecurity
                Debug.Assert(negoContext.GssContext == null || contextHandle == negoContext.GssContext);
                if (null == negoContext.GssContext)
                {
                    negoContext.SetGssContext(contextHandle);
                }

                SecurityStatusPalErrorCode errorCode = done ?
                    (negoContext.IsNtlmUsed && resultBuffer.Length > 0 ? SecurityStatusPalErrorCode.OK : SecurityStatusPalErrorCode.CompleteNeeded) :
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
            ref SafeFreeCredentials credentialsHandle,
            ref SafeDeleteContext securityContext,
            string spn,
            ContextFlagsPal requestedContextFlags,
            byte[] incomingBlob,
            ChannelBinding channelBinding,
            ref byte[] resultBlob,
            ref ContextFlagsPal contextFlags)
        {
            SafeFreeNegoCredentials negoCredentialsHandle = (SafeFreeNegoCredentials)credentialsHandle;

            if (negoCredentialsHandle.IsDefault && string.IsNullOrEmpty(spn))
            {
                throw new PlatformNotSupportedException(SR.net_nego_not_supported_empty_target_with_defaultcreds);
            }

            SecurityStatusPal status = EstablishSecurityContext(
                negoCredentialsHandle,
                ref securityContext,
                channelBinding,
                spn,
                requestedContextFlags,
                incomingBlob,
                ref resultBlob,
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
            byte[] incomingBlob,
            ChannelBinding channelBinding,
            ref byte[] resultBlob,
            ref ContextFlagsPal contextFlags)
        {
            if (securityContext == null)
            {
                securityContext = new SafeDeleteNegoContext((SafeFreeNegoCredentials)credentialsHandle);
            }

            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)securityContext;
            try
            {
                SafeGssContextHandle contextHandle = negoContext.GssContext;
                bool done = GssAcceptSecurityContext(
                   ref contextHandle,
                   incomingBlob,
                   out resultBlob,
                   out uint outputFlags);

                Debug.Assert(resultBlob != null, "Unexpected null buffer returned by GssApi");
                Debug.Assert(negoContext.GssContext == null || contextHandle == negoContext.GssContext);

                // Save the inner context handle for further calls to NetSecurity
                Debug.Assert(negoContext.GssContext == null || contextHandle == negoContext.GssContext);
                if (null == negoContext.GssContext)
                {
                    negoContext.SetGssContext(contextHandle);
                }

                contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(
                    (Interop.NetSecurityNative.GssFlags)outputFlags, isServer: true);

                SecurityStatusPalErrorCode errorCode = done ?
                    (negoContext.IsNtlmUsed && resultBlob.Length > 0 ? SecurityStatusPalErrorCode.OK : SecurityStatusPalErrorCode.CompleteNeeded) :
                    SecurityStatusPalErrorCode.ContinueNeeded;

                return new SecurityStatusPal(errorCode);
            }
            catch (Exception ex)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, ex);
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
        }

        private static string GetUser(
            ref SafeDeleteContext securityContext)
        {
            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)securityContext;
            try
            {
                SafeGssContextHandle contextHandle = negoContext.GssContext;
                return GssGetUser(ref contextHandle);
            }
            catch (Exception ex)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, ex);
                throw;
            }
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
            byte[] incomingBlob)
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
