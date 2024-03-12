// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    internal partial class NegotiateAuthenticationPal
    {
        private static readonly Lazy<bool> _hasSystemNetSecurityNative = new Lazy<bool>(CheckHasSystemNetSecurityNative);
        internal static bool HasSystemNetSecurityNative => _hasSystemNetSecurityNative.Value;
        private static bool UseManagedNtlm { get; } =
            AppContext.TryGetSwitch("System.Net.Security.UseManagedNtlm", out bool useManagedNtlm) ?
            useManagedNtlm :
            OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() ||
            (OperatingSystem.IsLinux() && RuntimeInformation.RuntimeIdentifier.StartsWith("linux-bionic-", StringComparison.OrdinalIgnoreCase));

        public static NegotiateAuthenticationPal Create(NegotiateAuthenticationClientOptions clientOptions)
        {
            if (UseManagedNtlm)
            {
                switch (clientOptions.Package)
                {
                    case NegotiationInfoClass.NTLM:
                        return ManagedNtlmNegotiateAuthenticationPal.Create(clientOptions);

                    case NegotiationInfoClass.Negotiate:
                        return new ManagedSpnegoNegotiateAuthenticationPal(clientOptions, supportKerberos: HasSystemNetSecurityNative);
                }
            }

            try
            {
                return new UnixNegotiateAuthenticationPal(clientOptions);
            }
            catch (Interop.NetSecurityNative.GssApiException gex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, gex);
                NegotiateAuthenticationStatusCode statusCode = UnixNegotiateAuthenticationPal.GetErrorCode(gex);
                if (statusCode <= NegotiateAuthenticationStatusCode.GenericFailure)
                {
                    statusCode = NegotiateAuthenticationStatusCode.Unsupported;
                }
                return new UnsupportedNegotiateAuthenticationPal(clientOptions, statusCode);
            }
            catch (EntryPointNotFoundException)
            {
                // GSSAPI shim may not be available on some platforms (Linux Bionic)
                return new UnsupportedNegotiateAuthenticationPal(clientOptions);
            }
        }

        public static NegotiateAuthenticationPal Create(NegotiateAuthenticationServerOptions serverOptions)
        {
            try
            {
                return new UnixNegotiateAuthenticationPal(serverOptions);
            }
            catch (Interop.NetSecurityNative.GssApiException gex)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, gex);
                NegotiateAuthenticationStatusCode statusCode = UnixNegotiateAuthenticationPal.GetErrorCode(gex);
                if (statusCode <= NegotiateAuthenticationStatusCode.GenericFailure)
                {
                    statusCode = NegotiateAuthenticationStatusCode.Unsupported;
                }
                return new UnsupportedNegotiateAuthenticationPal(serverOptions, statusCode);
            }
            catch (EntryPointNotFoundException)
            {
                // GSSAPI shim may not be available on some platforms (Linux Bionic)
                return new UnsupportedNegotiateAuthenticationPal(serverOptions);
            }
        }

        internal sealed class UnixNegotiateAuthenticationPal : NegotiateAuthenticationPal
        {
            private bool _isServer;
            private bool _isAuthenticated;
            private byte[]? _tokenBuffer;
            private SafeGssCredHandle _credentialsHandle;
            private SafeGssContextHandle? _securityContext;
            private SafeGssNameHandle? _targetNameHandle;
            private Interop.NetSecurityNative.GssFlags _requestedContextFlags;
            private Interop.NetSecurityNative.GssFlags _contextFlags;
            private string _package;
            private string? _spn;
            private ChannelBinding? _channelBinding;
            private readonly Interop.NetSecurityNative.PackageType _packageType;

            public override bool IsAuthenticated => _isAuthenticated;

            public override bool IsSigned => (_contextFlags & Interop.NetSecurityNative.GssFlags.GSS_C_INTEG_FLAG) != 0;

            public override bool IsEncrypted => (_contextFlags & Interop.NetSecurityNative.GssFlags.GSS_C_CONF_FLAG) != 0;

            public override bool IsMutuallyAuthenticated => (_contextFlags & Interop.NetSecurityNative.GssFlags.GSS_C_MUTUAL_FLAG) != 0;

            public override string Package => _package;

            public override string? TargetName
            {
                get
                {
                    if (_isServer && _spn == null)
                    {
                        Debug.Assert(_securityContext is not null && _isAuthenticated, "Trying to get the client SPN before handshaking is done!");
                        throw new PlatformNotSupportedException(SR.net_nego_server_not_supported);
                    }
                    return _spn;
                }
            }

            public override IIdentity RemoteIdentity
            {
                get
                {
                    IIdentity? result;
                    string? name = _isServer ? null : TargetName;
                    string protocol = Package;

                    Debug.Assert(_securityContext is not null);

                    if (_isServer)
                    {
                        try
                        {
                            name = GssGetUser(_securityContext);
                        }
                        catch (Exception ex)
                        {
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, ex);
                            throw;
                        }
                    }

                    // On the client we don't have access to the remote side identity.
                    result = new GenericIdentity(name ?? string.Empty, protocol);
                    return result;
                }
            }

            public override System.Security.Principal.TokenImpersonationLevel ImpersonationLevel
            {
                get
                {
                    return
                        (_contextFlags & Interop.NetSecurityNative.GssFlags.GSS_C_DELEG_FLAG) != 0 && Package != NegotiationInfoClass.NTLM ? TokenImpersonationLevel.Delegation :
                        (_contextFlags & Interop.NetSecurityNative.GssFlags.GSS_C_IDENTIFY_FLAG) != 0 ? TokenImpersonationLevel.Identification :
                        TokenImpersonationLevel.Impersonation;
                }
            }

            public UnixNegotiateAuthenticationPal(NegotiateAuthenticationClientOptions clientOptions)
            {
                Interop.NetSecurityNative.GssFlags contextFlags = clientOptions.RequiredProtectionLevel switch
                {
                    ProtectionLevel.Sign => Interop.NetSecurityNative.GssFlags.GSS_C_INTEG_FLAG,
                    ProtectionLevel.EncryptAndSign => Interop.NetSecurityNative.GssFlags.GSS_C_INTEG_FLAG | Interop.NetSecurityNative.GssFlags.GSS_C_CONF_FLAG,
                    _ => 0
                };

                contextFlags |= clientOptions.RequireMutualAuthentication ? Interop.NetSecurityNative.GssFlags.GSS_C_MUTUAL_FLAG : 0;

                contextFlags |= clientOptions.AllowedImpersonationLevel switch
                {
                    TokenImpersonationLevel.Identification => Interop.NetSecurityNative.GssFlags.GSS_C_IDENTIFY_FLAG,
                    TokenImpersonationLevel.Delegation => Interop.NetSecurityNative.GssFlags.GSS_C_DELEG_FLAG,
                    _ => 0
                };

                _isServer = false;
                _spn = clientOptions.TargetName;
                _securityContext = null;
                _requestedContextFlags = contextFlags;
                _package = clientOptions.Package;
                _channelBinding = clientOptions.Binding;
                _packageType = GetPackageType(_package);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Peer SPN-> '{_spn}'");

                if (clientOptions.Credential == CredentialCache.DefaultNetworkCredentials ||
                    string.IsNullOrWhiteSpace(clientOptions.Credential.UserName) ||
                    string.IsNullOrWhiteSpace(clientOptions.Credential.Password))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "using DefaultCredentials");

                    if (_packageType == Interop.NetSecurityNative.PackageType.NTLM)
                    {
                        // NTLM authentication is not possible with default credentials which are no-op
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_ntlm_not_possible_default_cred);
                        throw new Interop.NetSecurityNative.GssApiException(Interop.NetSecurityNative.Status.GSS_S_NO_CRED, 0, SR.net_ntlm_not_possible_default_cred);
                    }
                    if (string.IsNullOrEmpty(_spn))
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, SR.net_nego_not_supported_empty_target_with_defaultcreds);
                        throw new Interop.NetSecurityNative.GssApiException(Interop.NetSecurityNative.Status.GSS_S_BAD_NAME, 0, SR.net_nego_not_supported_empty_target_with_defaultcreds);
                    }

                    _credentialsHandle = SafeGssCredHandle.Create(string.Empty, string.Empty, _packageType);
                }
                else
                {
                    _credentialsHandle = AcquireCredentialsHandle(clientOptions.Credential);
                }
            }

            public UnixNegotiateAuthenticationPal(NegotiateAuthenticationServerOptions serverOptions)
            {
                Interop.NetSecurityNative.GssFlags contextFlags = serverOptions.RequiredProtectionLevel switch
                {
                    ProtectionLevel.Sign => Interop.NetSecurityNative.GssFlags.GSS_C_INTEG_FLAG,
                    ProtectionLevel.EncryptAndSign => Interop.NetSecurityNative.GssFlags.GSS_C_INTEG_FLAG | Interop.NetSecurityNative.GssFlags.GSS_C_CONF_FLAG,
                    _ => 0
                };

                // NOTE: Historically serverOptions.Policy was ignored on Unix without an exception
                // or error message. We continue to do so for compatibility reasons and because there
                // are no direct equivalents in GSSAPI.

                _isServer = true;
                _securityContext = null;
                _requestedContextFlags = contextFlags;
                _package = serverOptions.Package;
                _channelBinding = serverOptions.Binding;
                _packageType = GetPackageType(_package);

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Peer SPN-> '{_spn}'");

                if (serverOptions.Credential == CredentialCache.DefaultNetworkCredentials ||
                    string.IsNullOrWhiteSpace(serverOptions.Credential.UserName) ||
                    string.IsNullOrWhiteSpace(serverOptions.Credential.Password))
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "using DefaultCredentials");
                    _credentialsHandle = SafeGssCredHandle.CreateAcceptor();
                }
                else
                {
                    // NOTE: The input parameter was previously ignored and SafeGssCredHandle.CreateAcceptor
                    // was always used. We don't know of any uses with non-default credentials so this code
                    // path is essentially untested.
                    _credentialsHandle = AcquireCredentialsHandle(serverOptions.Credential);
                }
            }

            public override void Dispose()
            {
                _credentialsHandle?.Dispose();
                _targetNameHandle?.Dispose();
                _securityContext?.Dispose();
            }

            public override byte[]? GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
            {
                int resultBlobLength;
                if (!_isServer)
                {
                    // client session
                    statusCode = InitializeSecurityContext(
                        ref _credentialsHandle!,
                        ref _securityContext,
                        ref _targetNameHandle,
                        _spn,
                        _requestedContextFlags,
                        incomingBlob,
                        _channelBinding,
                        ref _tokenBuffer,
                        out resultBlobLength,
                        ref _contextFlags);

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.InitializeSecurityContext() returns statusCode:{statusCode}");
                }
                else
                {
                    // TODO: We don't currently check channel bindings.

                    // Server session.
                    statusCode = AcceptSecurityContext(
                        _credentialsHandle,
                        ref _securityContext,
                        incomingBlob,
                        ref _tokenBuffer,
                        out resultBlobLength,
                        ref _contextFlags);

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.AcceptSecurityContext() returns statusCode:{statusCode}");
                }

                if (statusCode >= NegotiateAuthenticationStatusCode.GenericFailure)
                {
                    Dispose();
                    _isAuthenticated = true;
                    _tokenBuffer = null;
                    return null;
                }

                byte[]? result =
                    resultBlobLength == 0 || _tokenBuffer == null ? null :
                    _tokenBuffer.Length == resultBlobLength ? _tokenBuffer :
                    _tokenBuffer[0..resultBlobLength];

                // The return value will tell us correctly if the handshake is over or not
                if (statusCode == NegotiateAuthenticationStatusCode.Completed)
                {
                    // Success.
                    _isAuthenticated = true;
                    _tokenBuffer = null;
                }
                else
                {
                    // We need to continue.
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"need continue _securityContext:{_securityContext}");
                }

                return result;
            }

            public override NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted)
            {
                Debug.Assert(_securityContext is not null);

                Interop.NetSecurityNative.GssBuffer encryptedBuffer = default;
                try
                {
                    Interop.NetSecurityNative.Status minorStatus;
                    bool encrypt = requestEncryption;
                    Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.WrapBuffer(
                        out minorStatus,
                        _securityContext,
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

            public override NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted)
            {
                Debug.Assert(_securityContext is not null);

                Interop.NetSecurityNative.GssBuffer decryptedBuffer = default(Interop.NetSecurityNative.GssBuffer);
                try
                {
                    Interop.NetSecurityNative.Status minorStatus;
                    Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.UnwrapBuffer(out minorStatus, _securityContext, out wasEncrypted, input, ref decryptedBuffer);
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

            public override unsafe NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted)
            {
                Debug.Assert(_securityContext is not null);

                Interop.NetSecurityNative.GssBuffer decryptedBuffer = default(Interop.NetSecurityNative.GssBuffer);
                try
                {
                    Interop.NetSecurityNative.Status minorStatus;
                    Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.UnwrapBuffer(out minorStatus, _securityContext, out wasEncrypted, input, ref decryptedBuffer);
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

            public override unsafe void GetMIC(ReadOnlySpan<byte> message, IBufferWriter<byte> signature)
            {
                Debug.Assert(_securityContext is not null);

                Interop.NetSecurityNative.GssBuffer micBuffer = default;
                try
                {
                    Interop.NetSecurityNative.Status minorStatus;
                    Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.GetMic(
                        out minorStatus,
                        _securityContext,
                        message,
                        ref micBuffer);
                    if (status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                    {
                        throw new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                    }

                    signature.Write(micBuffer.Span);
                }
                finally
                {
                    micBuffer.Dispose();
                }
            }

            public override unsafe bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
            {
                Debug.Assert(_securityContext is not null);

                Interop.NetSecurityNative.Status status = Interop.NetSecurityNative.VerifyMic(
                    out _,
                    _securityContext,
                    message,
                    signature);
                return status == Interop.NetSecurityNative.Status.GSS_S_COMPLETE;
            }

            private static Interop.NetSecurityNative.PackageType GetPackageType(string package)
            {
                if (string.Equals(package, NegotiationInfoClass.Negotiate, StringComparison.OrdinalIgnoreCase))
                {
                    return Interop.NetSecurityNative.PackageType.Negotiate;
                }
                else if (string.Equals(package, NegotiationInfoClass.NTLM, StringComparison.OrdinalIgnoreCase))
                {
                    return Interop.NetSecurityNative.PackageType.NTLM;
                }
                else if (string.Equals(package, NegotiationInfoClass.Kerberos, StringComparison.OrdinalIgnoreCase))
                {
                    return Interop.NetSecurityNative.PackageType.Kerberos;
                }
                else
                {
                    // Native shim currently supports only NTLM, Negotiate and Kerberos
                    throw new Interop.NetSecurityNative.GssApiException(Interop.NetSecurityNative.Status.GSS_S_UNAVAILABLE, 0);
                }
            }

            private SafeGssCredHandle AcquireCredentialsHandle(NetworkCredential credential)
            {
                try
                {
                    string username = credential.UserName;
                    string password = credential.Password;
                    ReadOnlySpan<char> domain = credential.Domain;

                    Debug.Assert(username != null && password != null, "Username and Password can not be null");

                    // any invalid user format will not be manipulated and passed as it is.
                    int index = username.IndexOf('\\');
                    if (index > 0 && username.IndexOf('\\', index + 1) < 0 && domain.IsEmpty)
                    {
                        domain = username.AsSpan(0, index);
                        username = username.Substring(index + 1);
                    }

                    // remove any leading and trailing whitespace
                    username = username.Trim();
                    domain = domain.Trim();
                    if (!username.Contains('@') && !domain.IsEmpty)
                    {
                        username = string.Concat(username, "@", domain);
                    }

                    return SafeGssCredHandle.Create(username, password, _packageType);
                }
                catch (Exception ex) when (ex is not Interop.NetSecurityNative.GssApiException)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, ex);
                    throw new Interop.NetSecurityNative.GssApiException(Interop.NetSecurityNative.Status.GSS_S_BAD_NAME, 0);
                }
            }

            private static string GssGetUser(SafeGssContextHandle context)
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

                    return Encoding.UTF8.GetString(tokenBytes);
                }
                finally
                {
                    token.Dispose();
                }
            }

            private unsafe NegotiateAuthenticationStatusCode InitializeSecurityContext(
                ref SafeGssCredHandle credentialsHandle,
                ref SafeGssContextHandle? contextHandle,
                ref SafeGssNameHandle? targetNameHandle,
                string? spn,
                Interop.NetSecurityNative.GssFlags requestedContextFlags,
                ReadOnlySpan<byte> incomingBlob,
                ChannelBinding? channelBinding,
                ref byte[]? resultBlob,
                out int resultBlobLength,
                ref Interop.NetSecurityNative.GssFlags contextFlags)
            {
                resultBlob = null;
                resultBlobLength = 0;

                if (contextHandle == null)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        string protocol = _packageType switch
                        {
                            Interop.NetSecurityNative.PackageType.NTLM => "NTLM",
                            Interop.NetSecurityNative.PackageType.Kerberos => "Kerberos",
                            _ => "SPNEGO"
                        };
                        NetEventSource.Info(this, $"requested protocol = {protocol}, target = {spn}");
                    }

                    targetNameHandle = SafeGssNameHandle.CreateTarget(spn!);
                    contextHandle = new SafeGssContextHandle();
                }

                Interop.NetSecurityNative.GssBuffer token = default(Interop.NetSecurityNative.GssBuffer);
                Interop.NetSecurityNative.Status status;
                Interop.NetSecurityNative.Status minorStatus;
                try
                {
                    uint outputFlags;
                    bool isNtlmUsed;

                    if (channelBinding != null)
                    {
                        // If a TLS channel binding token (cbt) is available then get the pointer
                        // to the application specific data.
                        int appDataOffset = sizeof(SecChannelBindings);
                        Debug.Assert(appDataOffset < channelBinding.Size);
                        IntPtr cbtAppData = channelBinding.DangerousGetHandle() + appDataOffset;
                        int cbtAppDataSize = channelBinding.Size - appDataOffset;
                        status = Interop.NetSecurityNative.InitSecContext(out minorStatus,
                                                                        credentialsHandle,
                                                                        ref contextHandle,
                                                                        _packageType,
                                                                        cbtAppData,
                                                                        cbtAppDataSize,
                                                                        targetNameHandle,
                                                                        (uint)requestedContextFlags,
                                                                        incomingBlob,
                                                                        ref token,
                                                                        out outputFlags,
                                                                        out isNtlmUsed);
                    }
                    else
                    {
                        status = Interop.NetSecurityNative.InitSecContext(out minorStatus,
                                                                        credentialsHandle,
                                                                        ref contextHandle,
                                                                        _packageType,
                                                                        targetNameHandle,
                                                                        (uint)requestedContextFlags,
                                                                        incomingBlob,
                                                                        ref token,
                                                                        out outputFlags,
                                                                        out isNtlmUsed);
                    }

                    if ((status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE) &&
                        (status != Interop.NetSecurityNative.Status.GSS_S_CONTINUE_NEEDED))
                    {
                        if (contextHandle.IsInvalid)
                        {
                            targetNameHandle?.Dispose();
                        }

                        Interop.NetSecurityNative.GssApiException gex = new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, gex);
                        resultBlob = Array.Empty<byte>();
                        return GetErrorCode(gex);
                    }

                    resultBlob = token.ToByteArray();
                    resultBlobLength = resultBlob?.Length ?? 0;

                    if (status == Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            string protocol = _packageType switch
                            {
                                Interop.NetSecurityNative.PackageType.NTLM => "NTLM",
                                Interop.NetSecurityNative.PackageType.Kerberos => "Kerberos",
                                _ => isNtlmUsed ? "SPNEGO-NTLM" : "SPNEGO-Kerberos"
                            };
                            NetEventSource.Info(this, $"actual protocol = {protocol}");
                        }

                        // Populate protocol used for authentication
                        _package = isNtlmUsed ? NegotiationInfoClass.NTLM : NegotiationInfoClass.Kerberos;
                    }

                    Debug.Assert(resultBlob != null, "Unexpected null buffer returned by GssApi");
                    contextFlags = (Interop.NetSecurityNative.GssFlags)outputFlags;

                    return status == Interop.NetSecurityNative.Status.GSS_S_COMPLETE ?
                        NegotiateAuthenticationStatusCode.Completed :
                        NegotiateAuthenticationStatusCode.ContinueNeeded;
                }
                catch (Exception ex)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, ex);
                    return NegotiateAuthenticationStatusCode.GenericFailure;
                }
                finally
                {
                    token.Dispose();
                }
            }

            private NegotiateAuthenticationStatusCode AcceptSecurityContext(
                SafeGssCredHandle credentialsHandle,
                ref SafeGssContextHandle? contextHandle,
                //ContextFlagsPal requestedContextFlags,
                ReadOnlySpan<byte> incomingBlob,
                //ChannelBinding? channelBinding,
                ref byte[]? resultBlob,
                out int resultBlobLength,
                ref Interop.NetSecurityNative.GssFlags contextFlags)
            {
                contextHandle ??= new SafeGssContextHandle();

                Interop.NetSecurityNative.GssBuffer token = default(Interop.NetSecurityNative.GssBuffer);
                try
                {
                    Interop.NetSecurityNative.Status status;
                    Interop.NetSecurityNative.Status minorStatus;
                    status = Interop.NetSecurityNative.AcceptSecContext(out minorStatus,
                                                                        credentialsHandle,
                                                                        ref contextHandle,
                                                                        incomingBlob,
                                                                        ref token,
                                                                        out uint outputFlags,
                                                                        out bool isNtlmUsed);

                    if ((status != Interop.NetSecurityNative.Status.GSS_S_COMPLETE) &&
                        (status != Interop.NetSecurityNative.Status.GSS_S_CONTINUE_NEEDED))
                    {
                        Interop.NetSecurityNative.GssApiException gex = new Interop.NetSecurityNative.GssApiException(status, minorStatus);
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, gex);
                        resultBlobLength = 0;
                        return GetErrorCode(gex);
                    }

                    resultBlob = token.ToByteArray();

                    Debug.Assert(resultBlob != null, "Unexpected null buffer returned by GssApi");

                    contextFlags = (Interop.NetSecurityNative.GssFlags)outputFlags;
                    resultBlobLength = resultBlob.Length;

                    NegotiateAuthenticationStatusCode errorCode;
                    if (status == Interop.NetSecurityNative.Status.GSS_S_COMPLETE)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            string protocol = isNtlmUsed ? "SPNEGO-NTLM" : "SPNEGO-Kerberos";
                            NetEventSource.Info(this, $"AcceptSecurityContext: actual protocol = {protocol}");
                        }

                        // Populate protocol used for authentication
                        _package = isNtlmUsed ? NegotiationInfoClass.NTLM : NegotiationInfoClass.Kerberos;
                        errorCode = NegotiateAuthenticationStatusCode.Completed;
                    }
                    else
                    {
                        errorCode = NegotiateAuthenticationStatusCode.ContinueNeeded;
                    }

                    return errorCode;
                }
                catch (Exception ex)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, ex);
                    resultBlobLength = 0;
                    return NegotiateAuthenticationStatusCode.GenericFailure;
                }
                finally
                {
                    token.Dispose();
                }
            }

            // https://www.gnu.org/software/gss/reference/gss.pdf (page 25)
            internal static NegotiateAuthenticationStatusCode GetErrorCode(Interop.NetSecurityNative.GssApiException exception)
            {
                switch (exception.MajorStatus)
                {
                    case Interop.NetSecurityNative.Status.GSS_S_NO_CRED:
                        return NegotiateAuthenticationStatusCode.UnknownCredentials;
                    case Interop.NetSecurityNative.Status.GSS_S_BAD_BINDINGS:
                        return NegotiateAuthenticationStatusCode.BadBinding;
                    case Interop.NetSecurityNative.Status.GSS_S_CREDENTIALS_EXPIRED:
                        return NegotiateAuthenticationStatusCode.CredentialsExpired;
                    case Interop.NetSecurityNative.Status.GSS_S_DEFECTIVE_TOKEN:
                        return NegotiateAuthenticationStatusCode.InvalidToken;
                    case Interop.NetSecurityNative.Status.GSS_S_DEFECTIVE_CREDENTIAL:
                        return NegotiateAuthenticationStatusCode.InvalidCredentials;
                    case Interop.NetSecurityNative.Status.GSS_S_BAD_SIG:
                        return NegotiateAuthenticationStatusCode.MessageAltered;
                    case Interop.NetSecurityNative.Status.GSS_S_BAD_MECH:
                    case Interop.NetSecurityNative.Status.GSS_S_UNAVAILABLE:
                        return NegotiateAuthenticationStatusCode.Unsupported;
                    case Interop.NetSecurityNative.Status.GSS_S_NO_CONTEXT:
                    default:
                        return NegotiateAuthenticationStatusCode.GenericFailure;
                }
            }
        }

        public static bool CheckHasSystemNetSecurityNative()
        {
            try
            {
                return Interop.NetSecurityNative.IsNtlmInstalled();
            }
            catch (Exception e) when (e is EntryPointNotFoundException || e is DllNotFoundException || e is TypeInitializationException)
            {
                // libSystem.Net.Security.Native is not available
                return false;
            }
        }
    }
}
