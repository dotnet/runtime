// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseSign1MessageTests_SignStream_Async : CoseMessageTests_SignStream_Async
    {
        internal override CoseMessageKind MessageKind => CoseMessageKind.Sign1;

        internal override void AddSignature(CoseMultiSignMessage msg, byte[] content, CoseSigner signer, byte[]? associatedData = null)
            => throw new NotSupportedException();

        internal override CoseMessage Decode(ReadOnlySpan<byte> cborPayload)
            => CoseMessage.DecodeSign1(cborPayload);

        internal override CoseHeaderMap GetSigningHeaderMap(CoseMessage msg, bool getProtectedMap)
        {
            Assert.IsType<CoseSign1Message>(msg);
            return getProtectedMap ? msg.ProtectedHeaders : msg.UnprotectedHeaders;
        }

        internal override Task<byte[]> SignDetachedAsync(Stream detachedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null)
            => CoseSign1Message.SignDetachedAsync(detachedContent, signer, associatedData);

        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            Assert.True(!OnlySupportsDetachedContent || msg.Content == null);
            return Sign1Verify(msg, key, content, associatedData);
        }
    }

    public class CoseSign1MessageTests_SignStream_Sync : CoseMessageTests_SignStream_Sync
    {
        internal override CoseMessageKind MessageKind => CoseMessageKind.Sign1;

        internal override void AddSignature(CoseMultiSignMessage msg, byte[] content, CoseSigner signer, byte[]? associatedData = null)
            => throw new NotSupportedException();

        internal override CoseMessage Decode(ReadOnlySpan<byte> cborPayload)
            => CoseMessage.DecodeSign1(cborPayload);

        internal override CoseHeaderMap GetSigningHeaderMap(CoseMessage msg, bool getProtectedMap)
        {
            CoseSign1Message sign1Msg = Assert.IsType<CoseSign1Message>(msg);
            return getProtectedMap ? msg.ProtectedHeaders : msg.UnprotectedHeaders;
        }

        internal override byte[] SignDetached(Stream detachedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null)
            => CoseSign1Message.SignDetached(detachedContent, signer, associatedData);

        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            Assert.True(!OnlySupportsDetachedContent || msg.Content == null);
            return Sign1Verify(msg, key, content, associatedData);
        }
    }
}
