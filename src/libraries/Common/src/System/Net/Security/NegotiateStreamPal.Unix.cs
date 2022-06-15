// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        private static byte[] GssWrap(
            SafeGssContextHandle? context,
            bool encrypt,
            ReadOnlySpan<byte> buffer)
        {
            Interop.NetSecurityNative.GssBuffer encryptedBuffer = default;
            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.WrapBuffer(out minorStatus, context, encrypt, buffer, ref encryptedBuffer);
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
            SafeGssContextHandle? context,
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
            ref SafeGssContextHandle? context,
            SafeGssCredHandle credential,
            bool isNtlm,
            ChannelBinding? channelBinding,
            SafeGssNameHandle? targetName,
            Interop.NetSecurityNative.GssFlags inFlags,
            byte[]? buffer,
            out byte[]? outputBuffer,
            out uint outFlags,
            out bool isNtlmUsed)
        {
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

                if (channelBinding != null)
                {
                    // If a TLS channel binding token (cbt) is available then get the pointer
                    // to the application specific data.
                    int appDataOffset = Marshal.SizeOf<SecChannelBindings>();
                    Debug.Assert(appDataOffset < channelBinding.Size);
                    IntPtr cbtAppData = channelBinding.DangerousGetHandle() + appDataOffset;
                    int cbtAppDataSize = channelBinding.Size - appDataOffset;
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
                }
                else
                {
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
                }

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
            ref SafeGssContextHandle? context,
            SafeGssCredHandle credential,
            byte[]? buffer,
            out byte[] outputBuffer,
            out uint outFlags,
            out bool isNtlmUsed)
        {
            Debug.Assert(credential != null);

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
                                                                    credential,
                                                                    ref context,
                                                                    buffer,
                                                                    buffer?.Length ?? 0,
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

        private static string GssGetUser(
            ref SafeGssContextHandle? context)
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

                ReadOnlySpan<byte> tokenBytes = token.Span;
                int length = tokenBytes.Length;
                if (length > 0 && tokenBytes[length - 1] == '\0')
                {
                    // Some GSS-API providers (gss-ntlmssp) include the terminating null with strings, so skip that.
                    tokenBytes = tokenBytes.Slice(0, length - 1);
                }

#if NETSTANDARD2_0
                return Encoding.UTF8.GetString(tokenBytes.ToArray(), 0, tokenBytes.Length);
#else
                return Encoding.UTF8.GetString(tokenBytes);
#endif
            }
            finally
            {
                token.Dispose();
            }
        }

        private static SecurityStatusPal EstablishSecurityContext(
          SafeFreeNegoCredentials credential,
          ref SafeDeleteContext? context,
          ChannelBinding? channelBinding,
          string? targetName,
          ContextFlagsPal inFlags,
          byte[]? incomingBlob,
          ref byte[]? resultBuffer,
          ref ContextFlagsPal outFlags)
        {
            bool isNtlmOnly = credential.IsNtlmOnly;

            if (context == null)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    string protocol = isNtlmOnly ? "NTLM" : "SPNEGO";
                    NetEventSource.Info(context, $"requested protocol = {protocol}, target = {targetName}");
                }

                context = new SafeDeleteNegoContext(credential, targetName!);
            }

            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)context;
            try
            {
                Interop.NetSecurityNative.GssFlags inputFlags =
                    ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(inFlags, isServer: false);
                uint outputFlags;
                bool isNtlmUsed;
                SafeGssContextHandle? contextHandle = negoContext.GssContext;
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
                    if (NetEventSource.Log.IsEnabled())
                    {
                        string protocol = isNtlmOnly ? "NTLM" : isNtlmUsed ? "SPNEGO-NTLM" : "SPNEGO-Kerberos";
                        NetEventSource.Info(context, $"actual protocol = {protocol}");
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
                    negoContext.SetGssContext(contextHandle!);
                }

                SecurityStatusPalErrorCode errorCode = done ?
                    (negoContext.IsNtlmUsed && resultBuffer.Length > 0 ? SecurityStatusPalErrorCode.OK : SecurityStatusPalErrorCode.CompleteNeeded) :
                    SecurityStatusPalErrorCode.ContinueNeeded;
                return new SecurityStatusPal(errorCode);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, ex);
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
        }

        internal static SecurityStatusPal InitializeSecurityContext(
            ref SafeFreeCredentials credentialsHandle,
            ref SafeDeleteContext? securityContext,
            string? spn,
            ContextFlagsPal requestedContextFlags,
            byte[]? incomingBlob,
            ChannelBinding? channelBinding,
            ref byte[]? resultBlob,
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

            return status;
        }

        internal static SecurityStatusPal AcceptSecurityContext(
            SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteContext? securityContext,
            ContextFlagsPal requestedContextFlags,
            byte[]? incomingBlob,
            ChannelBinding? channelBinding,
            ref byte[] resultBlob,
            ref ContextFlagsPal contextFlags)
        {
            if (securityContext == null)
            {
                securityContext = new SafeDeleteNegoContext((SafeFreeNegoCredentials)credentialsHandle!);
            }

            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)securityContext;
            try
            {
                SafeGssContextHandle? contextHandle = negoContext.GssContext;
                bool done = GssAcceptSecurityContext(
                   ref contextHandle,
                   negoContext.AcceptorCredential,
                   incomingBlob,
                   out resultBlob,
                   out uint outputFlags,
                   out bool isNtlmUsed);

                Debug.Assert(resultBlob != null, "Unexpected null buffer returned by GssApi");
                Debug.Assert(negoContext.GssContext == null || contextHandle == negoContext.GssContext);

                // Save the inner context handle for further calls to NetSecurity
                Debug.Assert(negoContext.GssContext == null || contextHandle == negoContext.GssContext);
                if (null == negoContext.GssContext)
                {
                    negoContext.SetGssContext(contextHandle!);
                }

                contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(
                    (Interop.NetSecurityNative.GssFlags)outputFlags, isServer: true);

                SecurityStatusPalErrorCode errorCode;
                if (done)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        string protocol = isNtlmUsed ? "SPNEGO-NTLM" : "SPNEGO-Kerberos";
                        NetEventSource.Info(securityContext, $"AcceptSecurityContext: actual protocol = {protocol}");
                    }

                    negoContext.SetAuthenticationPackage(isNtlmUsed);
                    errorCode = (isNtlmUsed && resultBlob.Length > 0) ? SecurityStatusPalErrorCode.OK : SecurityStatusPalErrorCode.CompleteNeeded;
                }
                else
                {
                    errorCode = SecurityStatusPalErrorCode.ContinueNeeded;
                }

                return new SecurityStatusPal(errorCode);
            }
            catch (Interop.NetSecurityNative.GssApiException gex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, gex);
                return new SecurityStatusPal(GetErrorCode(gex), gex);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, ex);
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
        }

        // https://www.gnu.org/software/gss/reference/gss.pdf (page 25)
        private static SecurityStatusPalErrorCode GetErrorCode(Interop.NetSecurityNative.GssApiException exception)
        {
            switch (exception.MajorStatus)
            {
                case Interop.NetSecurityNative.Status.GSS_S_NO_CRED:
                    return SecurityStatusPalErrorCode.UnknownCredentials;
                case Interop.NetSecurityNative.Status.GSS_S_BAD_BINDINGS:
                    return SecurityStatusPalErrorCode.BadBinding;
                case Interop.NetSecurityNative.Status.GSS_S_CREDENTIALS_EXPIRED:
                    return SecurityStatusPalErrorCode.CertExpired;
                case Interop.NetSecurityNative.Status.GSS_S_DEFECTIVE_TOKEN:
                    return SecurityStatusPalErrorCode.InvalidToken;
                case Interop.NetSecurityNative.Status.GSS_S_DEFECTIVE_CREDENTIAL:
                    return SecurityStatusPalErrorCode.IncompleteCredentials;
                case Interop.NetSecurityNative.Status.GSS_S_BAD_SIG:
                    return SecurityStatusPalErrorCode.MessageAltered;
                case Interop.NetSecurityNative.Status.GSS_S_BAD_MECH:
                    return SecurityStatusPalErrorCode.Unsupported;
                case Interop.NetSecurityNative.Status.GSS_S_NO_CONTEXT:
                default:
                    return SecurityStatusPalErrorCode.InternalError;
            }
        }

        private static string GetUser(
            ref SafeDeleteContext securityContext)
        {
            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)securityContext;
            try
            {
                SafeGssContextHandle? contextHandle = negoContext.GssContext;
                return GssGetUser(ref contextHandle);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, ex);
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
            catch (Exception ex)
            {
                throw new Win32Exception(NTE_FAIL, ex.Message);
            }
        }

        internal static SecurityStatusPal CompleteAuthToken(
            ref SafeDeleteContext? securityContext,
            byte[]? incomingBlob)
        {
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }

        internal static int Encrypt(
            SafeDeleteContext securityContext,
            ReadOnlySpan<byte> buffer,
            bool isConfidential,
            bool isNtlm,
            [NotNull] ref byte[]? output,
            uint sequenceNumber)
        {
            SafeDeleteNegoContext gssContext = (SafeDeleteNegoContext) securityContext;
            byte[] tempOutput = GssWrap(gssContext.GssContext, isConfidential, buffer);

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
            byte[]? buffer,
            int offset,
            int count,
            bool isConfidential,
            bool isNtlm,
            out int newOffset,
            uint sequenceNumber)
        {
            if (offset < 0 || offset > (buffer == null ? 0 : buffer.Length))
            {
                Debug.Fail("Argument 'offset' out of range");
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > (buffer == null ? 0 : buffer.Length - offset))
            {
                Debug.Fail("Argument 'count' out of range.");
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            newOffset = offset;
            return GssUnwrap(((SafeDeleteNegoContext)securityContext).GssContext, buffer!, offset, count);
        }

        internal static int VerifySignature(SafeDeleteContext securityContext, byte[] buffer, int offset, int count)
        {
            if (offset < 0 || offset > (buffer == null ? 0 : buffer.Length))
            {
                Debug.Fail("Argument 'offset' out of range");
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > (buffer == null ? 0 : buffer.Length - offset))
            {
                Debug.Fail("Argument 'count' out of range.");
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return GssUnwrap(((SafeDeleteNegoContext)securityContext).GssContext, buffer!, offset, count);
        }

        internal static int MakeSignature(SafeDeleteContext securityContext, byte[] buffer, int offset, int count, [AllowNull] ref byte[] output)
        {
            SafeDeleteNegoContext gssContext = (SafeDeleteNegoContext)securityContext;
            output = GssWrap(gssContext.GssContext, false, new ReadOnlySpan<byte>(buffer, offset, count));
            return output.Length;
        }
    }
}
