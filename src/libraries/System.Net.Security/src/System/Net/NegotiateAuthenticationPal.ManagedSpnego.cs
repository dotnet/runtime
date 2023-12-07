// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace System.Net
{
    internal abstract partial class NegotiateAuthenticationPal
    {
        internal sealed class ManagedSpnegoNegotiateAuthenticationPal : NegotiateAuthenticationPal
        {
            // Input parameters
            private readonly NegotiateAuthenticationClientOptions _clientOptions;

            // State parameters
            private byte[]? _spnegoMechList;
            private bool _isAuthenticated;
            private bool _supportKerberos;
            private NegotiateAuthenticationPal? _optimisticMechanism;
            private NegotiateAuthenticationPal? _mechanism;

            private const string SpnegoOid = "1.3.6.1.5.5.2";
            private const string NtlmOid = "1.3.6.1.4.1.311.2.2.10";
            private const string KerberosOid = "1.2.840.113554.1.2.2";

            // rfc4178
            private enum NegotiationToken
            {
                NegTokenInit = 0,
                NegTokenResp = 1
            }

            private enum NegTokenInit
            {
                MechTypes = 0,
                ReqFlags = 1,
                MechToken = 2,
                MechListMIC = 3
            }

            private enum NegTokenResp
            {
                NegState = 0,
                SupportedMech = 1,
                ResponseToken = 2,
                MechListMIC = 3
            }

            private enum NegState
            {
                Unknown = -1,           // Internal. Not in RFC.
                AcceptCompleted = 0,
                AcceptIncomplete = 1,
                Reject = 2,
                RequestMic = 3
            }

            public override bool IsAuthenticated => _isAuthenticated && _mechanism?.IsAuthenticated == true;
            public override bool IsSigned => _mechanism?.IsSigned ?? false;
            public override bool IsEncrypted => _mechanism?.IsEncrypted ?? false;
            public override bool IsMutuallyAuthenticated => _mechanism?.IsMutuallyAuthenticated ?? false;
            public override string Package => _mechanism?.Package ?? NegotiationInfoClass.Negotiate;
            public override string? TargetName => _clientOptions.TargetName;
            public override IIdentity RemoteIdentity => _mechanism?.RemoteIdentity ?? throw new InvalidOperationException();
            public override System.Security.Principal.TokenImpersonationLevel ImpersonationLevel => _mechanism?.ImpersonationLevel ?? System.Security.Principal.TokenImpersonationLevel.Impersonation;

            public ManagedSpnegoNegotiateAuthenticationPal(NegotiateAuthenticationClientOptions clientOptions, bool supportKerberos = false)
            {
                Debug.Assert(clientOptions.Package == NegotiationInfoClass.Negotiate);
                _clientOptions = clientOptions;
                _supportKerberos = supportKerberos;
            }

            public override void Dispose()
            {
                _optimisticMechanism?.Dispose();
                _optimisticMechanism = null;
                _mechanism?.Dispose();
                _mechanism = null;
                _isAuthenticated = false;
            }

            public override unsafe byte[]? GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
            {
                //Console.WriteLine($"ManagedSpnegoNegotiateAuthenticationPal.GetOutgoingBlob > {Convert.ToBase64String(incomingBlob)}");

                byte[]? outgoingBlob;
                if (_spnegoMechList == null)
                {
                    outgoingBlob = CreateSpNegoNegotiateMessage(incomingBlob, out statusCode);
                }
                else
                {
                    outgoingBlob = ProcessSpNegoChallenge(incomingBlob, out statusCode);
                }

                //Console.WriteLine($"ManagedSpnegoNegotiateAuthenticationPal.GetOutgoingBlob < {(outgoingBlob == null ? "null" : Convert.ToBase64String(outgoingBlob))} {statusCode}");

                return outgoingBlob;
            }

            private NegotiateAuthenticationPal CreateMechanismForPackage(string packageName)
            {
                return NegotiateAuthenticationPal.Create(new NegotiateAuthenticationClientOptions
                {
                    Package = packageName,
                    Credential = _clientOptions.Credential,
                    TargetName = _clientOptions.TargetName,
                    Binding = _clientOptions.Binding,
                    RequiredProtectionLevel = _clientOptions.RequiredProtectionLevel,
                    RequireMutualAuthentication = _clientOptions.RequireMutualAuthentication,
                    AllowedImpersonationLevel = _clientOptions.AllowedImpersonationLevel,
                });
            }

            private IEnumerable<KeyValuePair<string, string>> EnumerateMechanisms()
            {
                if (_supportKerberos)
                {
                    yield return new KeyValuePair<string, string>(NegotiationInfoClass.Kerberos, KerberosOid);
                }

                yield return new KeyValuePair<string, string>(NegotiationInfoClass.NTLM, NtlmOid);
            }

            private byte[]? CreateSpNegoNegotiateMessage(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                using (writer.PushSequence(new Asn1Tag(TagClass.Application, 0)))
                {
                    writer.WriteObjectIdentifier(SpnegoOid);

                    // NegTokenInit::= SEQUENCE {
                    //    mechTypes[0] MechTypeList,
                    //    reqFlags[1] ContextFlags OPTIONAL,
                    //       --inherited from RFC 2478 for backward compatibility,
                    //      --RECOMMENDED to be left out
                    //    mechToken[2] OCTET STRING  OPTIONAL,
                    //    mechListMIC[3] OCTET STRING  OPTIONAL,
                    //    ...
                    // }
                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenInit)))
                    {
                        using (writer.PushSequence())
                        {
                            byte[]? mechBlob = null;

                            // MechType::= OBJECT IDENTIFIER
                            //    -- OID represents each security mechanism as suggested by
                            //   --[RFC2743]
                            //
                            // MechTypeList::= SEQUENCE OF MechType
                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.MechTypes)))
                            {
                                AsnWriter mechListWriter = new AsnWriter(AsnEncodingRules.DER);

                                using (mechListWriter.PushSequence())
                                {
                                    foreach (KeyValuePair<string, string> packageAndOid in EnumerateMechanisms())
                                    {
                                        if (_optimisticMechanism == null)
                                        {
                                            _optimisticMechanism = CreateMechanismForPackage(packageAndOid.Key);
                                            mechBlob = _optimisticMechanism.GetOutgoingBlob(incomingBlob, out statusCode);
                                            if (statusCode != NegotiateAuthenticationStatusCode.ContinueNeeded &&
                                                statusCode != NegotiateAuthenticationStatusCode.Completed)
                                            {
                                                mechBlob = null;
                                                _optimisticMechanism?.Dispose();
                                                _optimisticMechanism = null;
                                                if (statusCode != NegotiateAuthenticationStatusCode.Unsupported)
                                                {
                                                    return null;
                                                }
                                                continue;
                                            }
                                        }

                                        mechListWriter.WriteObjectIdentifier(packageAndOid.Value);
                                    }
                                }

                                _spnegoMechList = mechListWriter.Encode();
                                mechListWriter.CopyTo(writer);
                            }

                            if (mechBlob != null)
                            {
                                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.MechToken)))
                                {
                                    writer.WriteOctetString(mechBlob);
                                }
                            }
                        }
                    }
                }

                statusCode = NegotiateAuthenticationStatusCode.ContinueNeeded;
                return writer.Encode();
            }

            private byte[]? ProcessSpNegoChallenge(ReadOnlySpan<byte> challenge, out NegotiateAuthenticationStatusCode statusCode)
            {
                NegState state = NegState.Unknown;
                string? mech = null;
                byte[]? blob = null;
                byte[]? mechListMIC = null;

                try
                {
                    AsnValueReader reader = new AsnValueReader(challenge, AsnEncodingRules.DER);
                    AsnValueReader challengeReader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenResp));
                    reader.ThrowIfNotEmpty();

                    // NegTokenResp ::= SEQUENCE {
                    //    negState[0] ENUMERATED {
                    //        accept - completed(0),
                    //        accept - incomplete(1),
                    //        reject(2),
                    //        request - mic(3)
                    //    } OPTIONAL,
                    // --REQUIRED in the first reply from the target
                    //    supportedMech[1] MechType OPTIONAL,
                    // --present only in the first reply from the target
                    // responseToken[2] OCTET STRING  OPTIONAL,
                    // mechListMIC[3] OCTET STRING  OPTIONAL,
                    // ...
                    // }

                    challengeReader = challengeReader.ReadSequence();

                    if (challengeReader.HasData && challengeReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.NegState)))
                    {
                        AsnValueReader valueReader = challengeReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.NegState));
                        state = valueReader.ReadEnumeratedValue<NegState>();
                        valueReader.ThrowIfNotEmpty();
                    }

                    if (challengeReader.HasData && challengeReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.SupportedMech)))
                    {
                        AsnValueReader valueReader = challengeReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.SupportedMech));
                        mech = valueReader.ReadObjectIdentifier();
                        valueReader.ThrowIfNotEmpty();
                    }

                    if (challengeReader.HasData && challengeReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken)))
                    {
                        AsnValueReader valueReader = challengeReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken));
                        blob = valueReader.ReadOctetString();
                        valueReader.ThrowIfNotEmpty();
                    }

                    if (challengeReader.HasData && challengeReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.MechListMIC)))
                    {
                        AsnValueReader valueReader = challengeReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.MechListMIC));
                        mechListMIC = valueReader.ReadOctetString();
                        valueReader.ThrowIfNotEmpty();
                    }

                    challengeReader.ThrowIfNotEmpty();
                }
                catch (AsnContentException)
                {
                    statusCode = NegotiateAuthenticationStatusCode.InvalidToken;
                    return null;
                }

                // Validate and choose the mechanism if necessary
                string? requestedPackage = mech switch
                {
                    NtlmOid => NegotiationInfoClass.NTLM,
                    KerberosOid => NegotiationInfoClass.Kerberos,
                    _ => null
                };

                if (_mechanism is null)
                {
                    if (requestedPackage is null)
                    {
                        statusCode = NegotiateAuthenticationStatusCode.Unsupported;
                        return null;
                    }

                    if (requestedPackage == _optimisticMechanism?.Package)
                    {
                        _mechanism = _optimisticMechanism;
                    }
                    else
                    {
                        // Abandon the optimistic path and restart with a new mechanism
                        _optimisticMechanism?.Dispose();
                        _mechanism = CreateMechanismForPackage(requestedPackage);
                    }

                    _optimisticMechanism = null;
                }
                else
                {
                    if (requestedPackage != null &&
                        _mechanism.Package != requestedPackage)
                    {
                        statusCode = NegotiateAuthenticationStatusCode.InvalidToken;
                        return null;
                    }
                }

                if (blob?.Length > 0)
                {
                    // Process decoded blob.
                    byte[]? response = _mechanism.GetOutgoingBlob(blob, out statusCode);

                    if (statusCode != NegotiateAuthenticationStatusCode.ContinueNeeded &&
                        statusCode != NegotiateAuthenticationStatusCode.Completed)
                    {
                        return null;
                    }

                    if (response?.Length > 0)
                    {
                        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

                        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenResp)))
                        {
                            using (writer.PushSequence())
                            {
                                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken)))
                                {
                                    writer.WriteOctetString(response);
                                }

                                if (statusCode == NegotiateAuthenticationStatusCode.Completed)
                                {
                                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.MechListMIC)))
                                    {
                                        ArrayBufferWriter<byte> micBuffer = new ArrayBufferWriter<byte>();
                                        _mechanism.GetMIC(_spnegoMechList, micBuffer);
                                        writer.WriteOctetString(micBuffer.WrittenSpan);
                                    }
                                }
                            }
                        }

                        statusCode = state == NegState.RequestMic ? NegotiateAuthenticationStatusCode.ContinueNeeded : NegotiateAuthenticationStatusCode.Completed;
                        _isAuthenticated = statusCode == NegotiateAuthenticationStatusCode.Completed;
                        return writer.Encode();
                    }
                }

                // Process MIC if the server sent it.
                //
                // We workaround broken servers that send the mechanism token in the mechListMIC
                // field. This is the same workaround that exists in MIT KRB5 and it's attributed to
                // Windows 2000 bug. It was reported in a .NET issue and tracked down as a bug in
                // IBM Websphere 8.5.5.19 on Java 1.8.
                //
                // References:
                // - https://github.com/krb5/krb5/blame/master/src/lib/gssapi/spnego/spnego_mech.c#L3521-L3525
                // - https://github.com/dotnet/runtime/issues/88874
                // - https://krbdev.mit.edu/rt/Ticket/Display.html?id=6726
                // - https://www.ibm.com/support/pages/apar/IV74044
                if (mechListMIC != null &&
                    !mechListMIC.AsSpan().SequenceEqual(blob.AsSpan()))
                {
                    if (_spnegoMechList == null || state != NegState.AcceptCompleted)
                    {
                        statusCode = NegotiateAuthenticationStatusCode.GenericFailure;
                        return null;
                    }

                    if (!_mechanism.VerifyMIC(_spnegoMechList, mechListMIC))
                    {
                        statusCode = NegotiateAuthenticationStatusCode.MessageAltered;
                        return null;
                    }

                    (_mechanism as ManagedNtlmNegotiateAuthenticationPal)?.ResetKeys();
                }

                _isAuthenticated = state == NegState.AcceptCompleted || state == NegState.Reject;
                statusCode = state switch {
                    NegState.AcceptCompleted => NegotiateAuthenticationStatusCode.Completed,
                    NegState.AcceptIncomplete => NegotiateAuthenticationStatusCode.ContinueNeeded,
                    NegState.Reject => NegotiateAuthenticationStatusCode.UnknownCredentials,
                    _ => NegotiateAuthenticationStatusCode.GenericFailure
                };

                return null;
            }

            public override NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted)
            {
                if (_mechanism is null || !_isAuthenticated)
                {
                    throw new InvalidOperationException(SR.net_auth_noauth);
                }

                return _mechanism.Wrap(input, outputWriter, requestEncryption, out isEncrypted);
            }

            public override NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted)
            {
                if (_mechanism is null || !_isAuthenticated)
                {
                    throw new InvalidOperationException(SR.net_auth_noauth);
                }

                return _mechanism.Unwrap(input, outputWriter, out wasEncrypted);
            }

            public override NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted)
            {
                if (_mechanism is null || !_isAuthenticated)
                {
                    throw new InvalidOperationException(SR.net_auth_noauth);
                }

                return _mechanism.UnwrapInPlace(input, out unwrappedOffset, out unwrappedLength, out wasEncrypted);
            }

            public override bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
            {
                if (_mechanism is null || !_isAuthenticated)
                {
                    throw new InvalidOperationException(SR.net_auth_noauth);
                }

                return _mechanism.VerifyMIC(message, signature);
            }

            public override void GetMIC(ReadOnlySpan<byte> message, IBufferWriter<byte> signature)
            {
                if (_mechanism is null || !_isAuthenticated)
                {
                    throw new InvalidOperationException(SR.net_auth_noauth);
                }

                _mechanism.GetMIC(message, signature);
            }
        }
    }
}
