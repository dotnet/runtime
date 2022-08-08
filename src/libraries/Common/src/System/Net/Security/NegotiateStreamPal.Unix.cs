// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Buffers;
using System.Buffers.Binary;
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
            SafeGssContextHandle context,
            ref bool encrypt,
            ReadOnlySpan<byte> buffer)
        {
            Interop.NetSecurityNative.GssBuffer encryptedBuffer = default;
            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.WrapBuffer(out minorStatus, context, ref encrypt, buffer, ref encryptedBuffer);
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
            out bool encrypt,
            Span<byte> buffer)
        {
            Interop.NetSecurityNative.GssBuffer decryptedBuffer = default(Interop.NetSecurityNative.GssBuffer);
            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.UnwrapBuffer(out minorStatus, context, out encrypt, buffer, ref decryptedBuffer);
                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                }

                decryptedBuffer.Span.CopyTo(buffer);
                return decryptedBuffer.Span.Length;
            }
            finally
            {
                decryptedBuffer.Dispose();
            }
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
          ReadOnlySpan<byte> incomingBlob,
          out byte[]? resultBuffer,
          ref ContextFlagsPal outFlags)
        {
            Interop.NetSecurityNative.PackageType packageType = credential.PackageType;

            resultBuffer = null;

            if (context == null)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    string protocol = packageType switch {
                        Interop.NetSecurityNative.PackageType.NTLM => "NTLM",
                        Interop.NetSecurityNative.PackageType.Kerberos => "Kerberos",
                        _ => "SPNEGO"
                    };
                    NetEventSource.Info(context, $"requested protocol = {protocol}, target = {targetName}");
                }

                context = new SafeDeleteNegoContext(credential, targetName!);
            }

            Interop.NetSecurityNative.GssBuffer token = default(Interop.NetSecurityNative.GssBuffer);
            Interop.NetSecurityNative.Status status;
            Interop.NetSecurityNative.Status minorStatus;
            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)context;
            SafeGssContextHandle contextHandle = negoContext.GssContext;
            try
            {
                Interop.NetSecurityNative.GssFlags inputFlags =
                    ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(inFlags, isServer: false);
                uint outputFlags;
                bool isNtlmUsed;

                if (channelBinding != null)
                {
                    // If a TLS channel binding token (cbt) is available then get the pointer
                    // to the application specific data.
                    int appDataOffset = Marshal.SizeOf<SecChannelBindings>();
                    Debug.Assert(appDataOffset < channelBinding.Size);
                    IntPtr cbtAppData = channelBinding.DangerousGetHandle() + appDataOffset;
                    int cbtAppDataSize = channelBinding.Size - appDataOffset;
                    status = Interop.NetSecurityNative.InitSecContext(out minorStatus,
                                                                      credential.GssCredential,
                                                                      ref contextHandle,
                                                                      packageType,
                                                                      cbtAppData,
                                                                      cbtAppDataSize,
                                                                      negoContext.TargetName,
                                                                      (uint)inputFlags,
                                                                      incomingBlob,
                                                                      ref token,
                                                                      out outputFlags,
                                                                      out isNtlmUsed);
                }
                else
                {
                    status = Interop.NetSecurityNative.InitSecContext(out minorStatus,
                                                                      credential.GssCredential,
                                                                      ref contextHandle,
                                                                      packageType,
                                                                      negoContext.TargetName,
                                                                      (uint)inputFlags,
                                                                      incomingBlob,
                                                                      ref token,
                                                                      out outputFlags,
                                                                      out isNtlmUsed);
                }

                if ((status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE) &&
                    (status != Interop.NetSecurityNative.Status.GSS_S_CONTINUE_NEEDED))
                {
                    if (negoContext.GssContext.IsInvalid)
                    {
                        context.Dispose();
                    }

                    Interop.NetSecurityNative.GssApiException gex = new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, gex);
                    resultBuffer = Array.Empty<byte>();
                    return new SecurityStatusPal(GetErrorCode(gex), gex);
                }

                resultBuffer = token.ToByteArray();

                if (status == Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        string protocol = packageType switch {
                            Interop.NetSecurityNative.PackageType.NTLM => "NTLM",
                            Interop.NetSecurityNative.PackageType.Kerberos => "Kerberos",
                            _ => isNtlmUsed ? "SPNEGO-NTLM" : "SPNEGO-Kerberos"
                        };
                        NetEventSource.Info(context, $"actual protocol = {protocol}");
                    }

                    // Populate protocol used for authentication
                    negoContext.SetAuthenticationPackage(isNtlmUsed);
                }

                Debug.Assert(resultBuffer != null, "Unexpected null buffer returned by GssApi");
                outFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(
                    (Interop.NetSecurityNative.GssFlags)outputFlags, isServer: false);

                SecurityStatusPalErrorCode errorCode = status == Interop.NetSecurityNative.Status.GSS_S_COMPLETE ?
                    SecurityStatusPalErrorCode.OK :
                    SecurityStatusPalErrorCode.ContinueNeeded;
                return new SecurityStatusPal(errorCode);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, ex);
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
            finally
            {
                token.Dispose();

                // Save the inner context handle for further calls to NetSecurity
                //
                // For the first call `negoContext.GssContext` is invalid and we expect the
                // inital handle to be returned from InitSecContext. For any subsequent
                // call the handle should stay the same or it can be destroyed by the native
                // InitSecContext call.
                Debug.Assert(
                    negoContext.GssContext == contextHandle ||
                    negoContext.GssContext.IsInvalid ||
                    contextHandle.IsInvalid);
                negoContext.SetGssContext(contextHandle);
            }
        }

        internal static SecurityStatusPal InitializeSecurityContext(
            ref SafeFreeCredentials credentialsHandle,
            ref SafeDeleteContext? securityContext,
            string? spn,
            ContextFlagsPal requestedContextFlags,
            ReadOnlySpan<byte> incomingBlob,
            ChannelBinding? channelBinding,
            ref byte[]? resultBlob,
            out int resultBlobLength,
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
                out resultBlob,
                ref contextFlags);
            resultBlobLength = resultBlob?.Length ?? 0;

            return status;
        }

        internal static SecurityStatusPal AcceptSecurityContext(
            SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteContext? securityContext,
            ContextFlagsPal requestedContextFlags,
            ReadOnlySpan<byte> incomingBlob,
            ChannelBinding? channelBinding,
            ref byte[] resultBlob,
            out int resultBlobLength,
            ref ContextFlagsPal contextFlags)
        {
            securityContext ??= new SafeDeleteNegoContext((SafeFreeNegoCredentials)credentialsHandle!);

            SafeDeleteNegoContext negoContext = (SafeDeleteNegoContext)securityContext;
            SafeGssContextHandle contextHandle = negoContext.GssContext;
            Interop.NetSecurityNative.GssBuffer token = default(Interop.NetSecurityNative.GssBuffer);
            try
            {
                Interop.NetSecurityNative.Status status;
                Interop.NetSecurityNative.Status minorStatus;
                status = Interop.NetSecurityNative.AcceptSecContext(out minorStatus,
                                                                    negoContext.AcceptorCredential,
                                                                    ref contextHandle,
                                                                    incomingBlob,
                                                                    ref token,
                                                                    out uint outputFlags,
                                                                    out bool isNtlmUsed);

                if ((status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE) &&
                    (status != Interop.NetSecurityNative.Status.GSS_S_CONTINUE_NEEDED))
                {
                    if (negoContext.GssContext.IsInvalid)
                    {
                        contextHandle.Dispose();
                    }

                    Interop.NetSecurityNative.GssApiException gex = new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, gex);
                    resultBlobLength = 0;
                    return new SecurityStatusPal(GetErrorCode(gex), gex);
                }

                resultBlob = token.ToByteArray();

                Debug.Assert(resultBlob != null, "Unexpected null buffer returned by GssApi");

                contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(
                    (Interop.NetSecurityNative.GssFlags)outputFlags, isServer: true);
                resultBlobLength = resultBlob.Length;

                SecurityStatusPalErrorCode errorCode;
                if (status == Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        string protocol = isNtlmUsed ? "SPNEGO-NTLM" : "SPNEGO-Kerberos";
                        NetEventSource.Info(securityContext, $"AcceptSecurityContext: actual protocol = {protocol}");
                    }

                    negoContext.SetAuthenticationPackage(isNtlmUsed);
                    errorCode = SecurityStatusPalErrorCode.OK;
                }
                else
                {
                    errorCode = SecurityStatusPalErrorCode.ContinueNeeded;
                }

                return new SecurityStatusPal(errorCode);
            }
            catch (Exception ex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, ex);
                resultBlobLength = 0;
                return new SecurityStatusPal(SecurityStatusPalErrorCode.InternalError, ex);
            }
            finally
            {
                token.Dispose();

                // Save the inner context handle for further calls to NetSecurity
                //
                // For the first call `negoContext.GssContext` is invalid and we expect the
                // inital handle to be returned from AcceptSecContext. For any subsequent
                // call the handle should stay the same or it can be destroyed by the native
                // AcceptSecContext call.
                Debug.Assert(
                    negoContext.GssContext == contextHandle ||
                    negoContext.GssContext.IsInvalid ||
                    contextHandle.IsInvalid);
                negoContext.SetGssContext(contextHandle);
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
            Interop.NetSecurityNative.PackageType packageType;

            if (string.Equals(package, NegotiationInfoClass.Negotiate, StringComparison.OrdinalIgnoreCase))
            {
                packageType = Interop.NetSecurityNative.PackageType.Negotiate;
            }
            else if (string.Equals(package, NegotiationInfoClass.NTLM, StringComparison.OrdinalIgnoreCase))
            {
                packageType = Interop.NetSecurityNative.PackageType.NTLM;
                if (isEmptyCredential && !isServer)
                {
                    // NTLM authentication is not possible with default credentials which are no-op
                    throw new PlatformNotSupportedException(SR.net_ntlm_not_possible_default_cred);
                }
            }
            else if (string.Equals(package, NegotiationInfoClass.Kerberos, StringComparison.OrdinalIgnoreCase))
            {
                packageType = Interop.NetSecurityNative.PackageType.Kerberos;
            }
            else
            {
                // Native shim currently supports only NTLM, Negotiate and Kerberos
                throw new PlatformNotSupportedException(SR.net_securitypackagesupport);
            }

            try
            {
                return isEmptyCredential ?
                    new SafeFreeNegoCredentials(packageType, string.Empty, string.Empty, string.Empty) :
                    new SafeFreeNegoCredentials(packageType, credential.UserName, credential.Password, credential.Domain);
            }
            catch (Exception ex)
            {
                throw new Win32Exception(NTE_FAIL, ex.Message);
            }
        }

        internal static SecurityStatusPal CompleteAuthToken(
            ref SafeDeleteContext? securityContext,
            ReadOnlySpan<byte> incomingBlob)
        {
            return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
        }

        internal static int Encrypt(
            SafeDeleteContext securityContext,
            ReadOnlySpan<byte> buffer,
            bool isConfidential,
            bool isNtlm,
            [NotNull] ref byte[]? output)
        {
            SafeGssContextHandle gssContext = ((SafeDeleteNegoContext)securityContext).GssContext!;
            int resultSize;

            if (isNtlm && !isConfidential)
            {
                Interop.NetSecurityNative.GssBuffer micBuffer = default;
                try
                {
                    Interop.NetSecurityNative.Status minorStatus;
                    Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.GetMic(
                        out minorStatus,
                        gssContext,
                        buffer,
                        ref micBuffer);
                    if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                    {
                        throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                    }

                    resultSize = micBuffer.Span.Length + buffer.Length;
                    if (output == null || output.Length < resultSize + 4)
                    {
                        output = new byte[resultSize + 4];
                    }

                    micBuffer.Span.CopyTo(output.AsSpan(4));
                    buffer.CopyTo(output.AsSpan(micBuffer.Span.Length + 4));
                    BinaryPrimitives.WriteInt32LittleEndian(output, resultSize);

                    return resultSize + 4;
                }
                finally
                {
                    micBuffer.Dispose();
                }
            }

            byte[] tempOutput = GssWrap(gssContext, ref isConfidential, buffer);

            // Create space for prefixing with the length
            const int prefixLength = 4;
            output = new byte[tempOutput.Length + prefixLength];
            Array.Copy(tempOutput, 0, output, prefixLength, tempOutput.Length);
            resultSize = tempOutput.Length;
            BinaryPrimitives.WriteInt32LittleEndian(output, resultSize);

            return resultSize + 4;
        }

        internal static int Decrypt(
            SafeDeleteContext securityContext,
            Span<byte> buffer,
            bool isConfidential,
            bool isNtlm,
            out int newOffset)
        {
            SafeGssContextHandle gssContext = ((SafeDeleteNegoContext)securityContext).GssContext!;

            if (isNtlm && !isConfidential)
            {
                const int NtlmSignatureLength = 16;

                if (buffer.Length < NtlmSignatureLength)
                {
                    Debug.Fail("Argument 'count' out of range.");
                    throw new Interop.NetSecurityNative.GssApiException(Interop.NetSecurityNative.Status.GSS_S_DEFECTIVE_TOKEN, 0);
                }

                Interop.NetSecurityNative.Status minorStatus;
                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.VerifyMic(
                    out minorStatus,
                    gssContext,
                    buffer.Slice(NtlmSignatureLength),
                    buffer.Slice(0, NtlmSignatureLength));
                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                }

                newOffset = NtlmSignatureLength;
                return buffer.Length - NtlmSignatureLength;
            }

            newOffset = 0;
            return GssUnwrap(gssContext, out _, buffer);
        }

        internal static NegotiateAuthenticationStatusCode Unwrap(
            SafeDeleteContext securityContext,
            ReadOnlySpan<byte> input,
            IBufferWriter<byte> outputWriter,
            out bool isEncrypted)
        {
            SafeGssContextHandle gssContext = ((SafeDeleteNegoContext)securityContext).GssContext!;
            Interop.NetSecurityNative.GssBuffer decryptedBuffer = default(Interop.NetSecurityNative.GssBuffer);
            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.UnwrapBuffer(out minorStatus, gssContext, out isEncrypted, input, ref decryptedBuffer);
                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    return status switch
                    {
                        Interop.NetSecurityNative.Status.GSS_S_BAD_SIG => NegotiateAuthenticationStatusCode.MessageAltered,
                        _ => NegotiateAuthenticationStatusCode.InvalidToken
                    };
                }

                decryptedBuffer.Span.CopyTo(outputWriter.GetSpan(decryptedBuffer.Span.Length));
                outputWriter.Advance(decryptedBuffer.Span.Length);
                return NegotiateAuthenticationStatusCode.Completed;
            }
            finally
            {
                decryptedBuffer.Dispose();
            }
        }

        internal static NegotiateAuthenticationStatusCode UnwrapInPlace(
            SafeDeleteContext securityContext,
            Span<byte> input,
            out int unwrappedOffset,
            out int unwrappedLength,
            out bool isEncrypted)
        {
            SafeGssContextHandle gssContext = ((SafeDeleteNegoContext)securityContext).GssContext!;
            Interop.NetSecurityNative.GssBuffer decryptedBuffer = default(Interop.NetSecurityNative.GssBuffer);
            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.UnwrapBuffer(out minorStatus, gssContext, out isEncrypted, input, ref decryptedBuffer);
                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    unwrappedOffset = 0;
                    unwrappedLength = 0;
                    return status switch
                    {
                        Interop.NetSecurityNative.Status.GSS_S_BAD_SIG => NegotiateAuthenticationStatusCode.MessageAltered,
                        _ => NegotiateAuthenticationStatusCode.InvalidToken
                    };
                }

                decryptedBuffer.Span.CopyTo(input);
                unwrappedOffset = 0;
                unwrappedLength = decryptedBuffer.Span.Length;
                return NegotiateAuthenticationStatusCode.Completed;
            }
            finally
            {
                decryptedBuffer.Dispose();
            }
        }

        internal static NegotiateAuthenticationStatusCode Wrap(
            SafeDeleteContext securityContext,
            ReadOnlySpan<byte> input,
            IBufferWriter<byte> outputWriter,
            bool requestEncryption,
            out bool isEncrypted)
        {
            SafeGssContextHandle gssContext = ((SafeDeleteNegoContext)securityContext).GssContext!;
            Interop.NetSecurityNative.GssBuffer encryptedBuffer = default;
            try
            {
                Interop.NetSecurityNative.Status minorStatus;
                bool encrypt = requestEncryption;
                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.WrapBuffer(
                    out minorStatus,
                    gssContext,
                    ref encrypt,
                    input,
                    ref encryptedBuffer);
                isEncrypted = encrypt;
                if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                {
                    return NegotiateAuthenticationStatusCode.GenericFailure;
                }

                encryptedBuffer.Span.CopyTo(outputWriter.GetSpan(encryptedBuffer.Span.Length));
                outputWriter.Advance(encryptedBuffer.Span.Length);
                return NegotiateAuthenticationStatusCode.Completed;
            }
            finally
            {
                encryptedBuffer.Dispose();
            }

        }
    }
}
