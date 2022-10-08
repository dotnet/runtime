// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public abstract class CoseMultiSignMessageTests_Sign_CustomHeaderMaps : CoseMessageTests_Sign_CustomHeaderMaps
    {
        internal override CoseMessageKind MessageKind => CoseMessageKind.MultiSign;

        internal override void AddSignature(CoseMultiSignMessage msg, byte[] content, CoseSigner signer, byte[]? associatedData = null)
            => MultiSignAddSignature(msg, content, signer, associatedData);

        internal override CoseMessage Decode(ReadOnlySpan<byte> cborPayload)
            => CoseMessage.DecodeMultiSign(cborPayload);

        internal override CoseHeaderMap GetSigningHeaderMap(CoseMessage msg, bool getProtectedMap)
        {
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            Assert.Equal(1, multiSignMsg.Signatures.Count);
            CoseSignature signature = multiSignMsg.Signatures[0];

            return getProtectedMap ? signature.ProtectedHeaders : signature.UnprotectedHeaders;
        }

        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
            => MultiSignVerify(msg, key, content, expectedSignatures: 1, associatedData);
    }

    public class CoseMultiSignMessageTests_Sign_ByteArray : CoseMultiSignMessageTests_Sign_CustomHeaderMaps
    {
        internal override byte[] Sign(
            byte[] content,
            CoseSigner signer,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            byte[]? associatedData = null,
            bool isDetached = false)
        {
            return isDetached ?
                CoseMultiSignMessage.SignDetached(content, signer, protectedHeaders, unprotectedHeaders, associatedData) :
                CoseMultiSignMessage.SignEmbedded(content, signer, protectedHeaders, unprotectedHeaders, associatedData);
        }
    }

    public class CoseMultiSignMessageTests_TrySign : CoseMultiSignMessageTests_Sign_CustomHeaderMaps
    {
        internal override byte[] Sign(
            byte[] content,
            CoseSigner signer,
            CoseHeaderMap? protectedHeaders = null,
            CoseHeaderMap? unprotectedHeaders = null,
            byte[]? associatedData = null,
            bool isDetached = false)
        {
            Span<byte> destination;
            int bytesWritten;
            byte[] expectedEncodedMsg = SignFixed(content, signer, protectedHeaders, unprotectedHeaders, associatedData, isDetached);

            // Assert TrySign returns false when destination buffer is smaller than what we need (size - i).
            for (int i = 1; i < 10; i++)
            {
                destination = expectedEncodedMsg.AsSpan(0, expectedEncodedMsg.Length - i);
                Assert.False(TrySign(content, destination, signer, out bytesWritten, protectedHeaders, unprotectedHeaders, associatedData, isDetached));
                Assert.Equal(0, bytesWritten);
            }

            // Assert TrySign returns true when destination is double the required size (or at least 2k).
            destination = new byte[Math.Max(expectedEncodedMsg.Length * 2, 2048)];
            Assert.True(TrySign(content, destination, signer, out bytesWritten, protectedHeaders, unprotectedHeaders, associatedData, isDetached));
            Assert.Equal(expectedEncodedMsg.Length, bytesWritten);

            // Assert TrySign returns true when destination is the exact size required.
            destination = destination.Slice(0, expectedEncodedMsg.Length);
            destination.Clear();
            Assert.True(TrySign(content, destination, signer, out bytesWritten, protectedHeaders, unprotectedHeaders, associatedData, isDetached));
            Assert.Equal(destination.Length, bytesWritten);

            return destination.ToArray();
        }

        private byte[] SignFixed(byte[] content, CoseSigner signer, CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders, byte[]? associatedData, bool isDetached)
            => isDetached ?
            CoseMultiSignMessage.SignDetached(content, signer, protectedHeaders, unprotectedHeaders, associatedData) :
            CoseMultiSignMessage.SignEmbedded(content, signer, protectedHeaders, unprotectedHeaders, associatedData);

        private bool TrySign(ReadOnlySpan<byte> content, Span<byte> destination, CoseSigner signer, out int bytesWritten, CoseHeaderMap? protectedHeaders, CoseHeaderMap? unprotectedHeaders, byte[]? associatedData, bool isDetached)
            => isDetached ?
            CoseMultiSignMessage.TrySignDetached(content, destination, signer, out bytesWritten, protectedHeaders, unprotectedHeaders, associatedData) :
            CoseMultiSignMessage.TrySignEmbedded(content, destination, signer, out bytesWritten, protectedHeaders, unprotectedHeaders, associatedData);
    }
}
