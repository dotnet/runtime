// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Linq;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseMultiSignMessageTests_Sign : CoseMessageTests_Sign<AsymmetricAlgorithm>
    {
        internal override List<CoseAlgorithm> CoseAlgorithms => Enum.GetValues(typeof(CoseAlgorithm)).Cast<CoseAlgorithm>().ToList();

        internal override CoseMessageKind MessageKind => CoseMessageKind.MultiSign;

        internal override void AddSignature(CoseMultiSignMessage msg, byte[] content, CoseSigner signer, byte[]? associatedData = null)
            => MultiSignAddSignature(msg, content, signer, associatedData);

        internal override CoseMessage Decode(ReadOnlySpan<byte> cborPayload)
            => CoseMessage.DecodeMultiSign(cborPayload);

        internal override byte[] Sign(byte[] content, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null, bool isDetached = false)
        {
            return isDetached ?
                CoseMultiSignMessage.SignDetached(content, signer, protectedHeaders, unprotectedHeaders, associatedData) :
                CoseMultiSignMessage.SignEmbedded(content, signer, protectedHeaders, unprotectedHeaders, associatedData);
        }

        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
            => MultiSignVerify(msg, key, content, expectedSignatures: 1, associatedData);
    }
}
