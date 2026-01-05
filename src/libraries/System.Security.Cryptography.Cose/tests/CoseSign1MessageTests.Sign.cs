// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public abstract class CoseSign1MessageTests_Sign<T> : CoseMessageTests_Sign<T> where T : IDisposable
    {
        internal override CoseMessageKind MessageKind => CoseMessageKind.Sign1;

        internal override void AddSignature(CoseMultiSignMessage msg, byte[] content, CoseSigner signer, byte[]? associatedData = null)
            => throw new NotSupportedException();

        internal override bool Verify(CoseMessage msg, T key, byte[] content, byte[]? associatedData = null)
            => Sign1Verify(msg, key, content, associatedData);
    }

    public class CoseSign1MessageTests_Sign_ECDsa : CoseSign1MessageTests_Sign<ECDsa>
    {
        internal override List<CoseAlgorithm> CoseAlgorithms => new() { CoseAlgorithm.ES256, CoseAlgorithm.ES384, CoseAlgorithm.ES512 };

        internal override CoseMessage Decode(ReadOnlySpan<byte> cborPayload) => CoseMessage.DecodeSign1(cborPayload);

        internal override byte[] Sign(byte[] content, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null, bool isDetached = false)
        {
            return isDetached ?
                CoseSign1Message.SignDetached(content, signer, associatedData) :
                CoseSign1Message.SignEmbedded(content, signer, associatedData);
        }
    }

    public class CoseSign1MessageTests_Sign_RSA : CoseSign1MessageTests_Sign<RSA>
    {
        internal override List<CoseAlgorithm> CoseAlgorithms => new() { CoseAlgorithm.PS256, CoseAlgorithm.PS384, CoseAlgorithm.PS512 };

        internal override CoseMessage Decode(ReadOnlySpan<byte> cborPayload)
            => CoseMessage.DecodeSign1(cborPayload);

        internal override byte[] Sign(byte[] content, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null, bool isDetached = false)
        {
            return isDetached ?
                CoseSign1Message.SignDetached(content, signer, associatedData) :
                CoseSign1Message.SignEmbedded(content, signer, associatedData);
        }
    }

    [ConditionalClass(typeof(MLDsa), nameof(MLDsa.IsSupported))]
    public class CoseSign1MessageTests_Sign_MLDsa : CoseSign1MessageTests_Sign<MLDsa>
    {
        internal override bool SupportsHashAlgorithm => false;
        internal override List<CoseAlgorithm> CoseAlgorithms => new() { CoseAlgorithm.MLDsa44, CoseAlgorithm.MLDsa65, CoseAlgorithm.MLDsa87 };

        internal override CoseMessage Decode(ReadOnlySpan<byte> cborPayload)
            => CoseMessage.DecodeSign1(cborPayload);

        internal override byte[] Sign(byte[] content, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null, bool isDetached = false)
        {
            return isDetached ?
                CoseSign1Message.SignDetached(content, signer, associatedData) :
                CoseSign1Message.SignEmbedded(content, signer, associatedData);
        }
    }
}
