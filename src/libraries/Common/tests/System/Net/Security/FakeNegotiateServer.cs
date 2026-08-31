// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;
using System.Text;
using System.Net;
using System.Security.Cryptography;
using System.Net.Security;
using System.Formats.Asn1;
using Xunit;

namespace System.Net.Security
{
    internal class FakeNegotiateServer
    {
        FakeNtlmServer _ntlmServer;
        byte[]? _spnegoMechList = null;
        bool _ntlmPassthrough = false;

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
            AcceptCompleted = 0,
            AcceptIncomplete = 1,
            Reject = 2,
            RequestMic = 3
        }

        private const string SpnegoOid = "1.3.6.1.5.5.2";
        private const string NtlmOid = "1.3.6.1.4.1.311.2.2.10";
        private static ReadOnlySpan<byte> NtlmHeader => "NTLMSSP\0"u8;

        // Behavior modifiers
        public bool RequestMIC { get; set; } = true;

        // Negotiation results
        public bool IsAuthenticated { get; private set; }

        public FakeNegotiateServer(FakeNtlmServer ntlmServer)
        {
            _ntlmServer = ntlmServer;
        }

        public byte[]? GetOutgoingBlob(byte[]? incomingBlob)
        {
            if (_spnegoMechList == null && incomingBlob.AsSpan().StartsWith(NtlmHeader))
            {
                _ntlmPassthrough = true;
                // Windows often sends pure NTLM instead of proper Negotiate, handle that as passthrough
                byte[]? outgoingBlob = _ntlmServer.GetOutgoingBlob(incomingBlob);
                IsAuthenticated = _ntlmServer.IsAuthenticated;
                return outgoingBlob;
            }

            Assert.False(_ntlmPassthrough);

            AsnReader reader = new AsnReader(incomingBlob, AsnEncodingRules.DER);
            if (_spnegoMechList == null)
            {
                AsnReader initialContextTokenReader = reader.ReadSequence(new Asn1Tag(TagClass.Application, 0));

                string spNegoOid = initialContextTokenReader.ReadObjectIdentifier();
                Assert.Equal(SpnegoOid, spNegoOid);

                AsnReader negTokenInitReader  = initialContextTokenReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenInit)).ReadSequence();
                AsnReader mechTypesOuterReader = negTokenInitReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.MechTypes));
                _spnegoMechList = mechTypesOuterReader.PeekEncodedValue().ToArray();

                bool hasNtlm = false;
                bool isNtlmPreferred = false;
                bool first = true;
                AsnReader mechTypesReader = mechTypesOuterReader.ReadSequence();
                while (mechTypesReader.HasData)
                {
                    string mechType = mechTypesReader.ReadObjectIdentifier();
                    if (mechType == NtlmOid)
                    {
                        hasNtlm = true;
                        isNtlmPreferred = first;
                    }
                    first = false;
                }

                // Skip context flags, if present
                if (negTokenInitReader.HasData && negTokenInitReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.ReqFlags)))
                {
                    negTokenInitReader.ReadSequence();
                }

                byte[]? mechToken = null;
                if (negTokenInitReader.HasData && negTokenInitReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.MechToken)))
                {
                    AsnReader mechTokenReader = negTokenInitReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.MechToken));
                    mechToken = mechTokenReader.ReadOctetString();
                    Assert.False(mechTokenReader.HasData);
                }

                byte[]? mechListMIC = null;
                if (negTokenInitReader.HasData && negTokenInitReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.MechListMIC)))
                {
                    AsnReader mechListMICReader = negTokenInitReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenInit.MechListMIC));
                    mechListMIC = mechListMICReader.ReadOctetString();
                    Assert.False(mechListMICReader.HasData);
                }

                Assert.True(hasNtlm);

                // If the preferred mechanism was NTLM then proceed with the given token
                byte[]? outgoingBlob = null;
                if (isNtlmPreferred && mechToken != null)
                {
                    Assert.Null(mechListMIC);
                    outgoingBlob = _ntlmServer.GetOutgoingBlob(mechToken);
                }

                // Generate reply
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenResp)))
                {
                    using (writer.PushSequence())
                    {
                        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.NegState)))
                        {
                            if (RequestMIC)
                            {
                                writer.WriteEnumeratedValue(NegState.RequestMic);
                            }
                            else
                            {
                                writer.WriteEnumeratedValue(NegState.AcceptIncomplete);
                            }
                        }

                        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.SupportedMech)))
                        {
                            writer.WriteObjectIdentifier(NtlmOid);
                        }

                        if (outgoingBlob != null)
                        {
                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken)))
                            {
                                writer.WriteOctetString(outgoingBlob);
                            }
                        }
                    }
                }

                return writer.Encode();
            }
            else
            {
                AsnReader negTokenRespReader = reader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenResp)).ReadSequence();

                Assert.True(negTokenRespReader.HasData);
                NegState? clientState;
                if (negTokenRespReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.NegState)))
                {
                    AsnReader valueReader = negTokenRespReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.NegState));
                    clientState = valueReader.ReadEnumeratedValue<NegState>();
                    Assert.False(valueReader.HasData);

                    Assert.NotEqual(NegState.Reject, clientState);
                    Assert.NotEqual(NegState.RequestMic, clientState);
                }

                // Client should not send mechanism
                Assert.False(negTokenRespReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.SupportedMech)));

                byte[]? mechToken = null;
                if (negTokenRespReader.HasData && negTokenRespReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken)))
                {
                    AsnReader mechTokenReader = negTokenRespReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken));
                    mechToken = mechTokenReader.ReadOctetString();
                    Assert.False(mechTokenReader.HasData);
                }

                byte[]? mechListMIC = null;
                if (negTokenRespReader.HasData && negTokenRespReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.MechListMIC)))
                {
                    AsnReader mechListMICReader = negTokenRespReader.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.MechListMIC));
                    mechListMIC = mechListMICReader.ReadOctetString();
                    Assert.False(mechListMICReader.HasData);
                }

                Assert.NotNull(mechToken);
                byte[]? outgoingBlob = _ntlmServer.GetOutgoingBlob(mechToken);

                if (_ntlmServer.IsAuthenticated)
                {
                    if (RequestMIC)
                    {
                        Assert.NotNull(mechListMIC);
                    }

                    // Validate mechListMIC, if present
                    if (mechListMIC is not null)
                    {
                        _ntlmServer.VerifyMIC(_spnegoMechList, mechListMIC);
                    }
                }
                else
                {
                    Assert.Null(mechListMIC);
                }

                // Generate reply
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegotiationToken.NegTokenResp)))
                {
                    using (writer.PushSequence())
                    {
                        using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.NegState)))
                        {
                            if (_ntlmServer.IsAuthenticated)
                            {
                                writer.WriteEnumeratedValue(NegState.AcceptCompleted);
                            }
                            else if (outgoingBlob != null)
                            {
                                writer.WriteEnumeratedValue(NegState.AcceptIncomplete);
                            }
                            else
                            {
                                writer.WriteEnumeratedValue(NegState.Reject);
                            }
                        }

                        if (outgoingBlob != null)
                        {
                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.ResponseToken)))
                            {
                                writer.WriteOctetString(outgoingBlob);
                            }
                        }

                        if (mechListMIC != null)
                        {
                            using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, (int)NegTokenResp.MechListMIC)))
                            {
                                Span<byte> mic = stackalloc byte[16];
                                _ntlmServer.GetMIC(_spnegoMechList, mic);
                                writer.WriteOctetString(mic);

                                // MS-SPNG section 3.2.5.1 NTLM RC4 Key State for MechListMIC and First Signed Message
                                // specifies that the RC4 sealing keys are reset back to the initial state for the
                                // first message.
                                _ntlmServer.ResetKeys();
                            }
                        }
                    }
                }

                IsAuthenticated = _ntlmServer.IsAuthenticated;

                return writer.Encode();
            }
        }
    }
}
