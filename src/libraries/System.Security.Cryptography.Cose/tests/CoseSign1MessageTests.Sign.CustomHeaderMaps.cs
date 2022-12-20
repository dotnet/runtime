// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public abstract class CoseSign1MessageTests_Sign_CustomHeaderMaps : CoseMessageTests_Sign_CustomHeaderMaps
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

        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
            => Sign1Verify(msg, key, content, associatedData);
    }

    public class CoseSign1MessageTests_Sign_ByteArray : CoseSign1MessageTests_Sign_CustomHeaderMaps
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
                CoseSign1Message.SignDetached(content, signer, associatedData) :
                CoseSign1Message.SignEmbedded(content, signer, associatedData);
        }
    }

    public class CoseSign1MessageTests_TrySign : CoseSign1MessageTests_Sign_CustomHeaderMaps
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
            byte[] expectedEncodedMsg = Sign(content, signer, isDetached, associatedData);

            // Assert TrySign returns false when destination buffer is smaller than what we need (size - i).
            for (int i = 1; i < 10; i++)
            {
                destination = expectedEncodedMsg.AsSpan(0, expectedEncodedMsg.Length - i);
                Assert.False(TrySign(content, destination, signer, isDetached, out bytesWritten, associatedData));
                Assert.Equal(0, bytesWritten);
            }

            // Assert TrySign returns true when destination is double the required size (or at least 2k).
            destination = new byte[Math.Max(expectedEncodedMsg.Length * 2, 2048)];
            Assert.True(TrySign(content, destination, signer, isDetached, out bytesWritten, associatedData));
            Assert.Equal(expectedEncodedMsg.Length, bytesWritten);

            // Assert TrySign returns true when destination is the exact size required.
            destination = destination.Slice(0, expectedEncodedMsg.Length);
            destination.Clear();
            Assert.True(TrySign(content, destination, signer, isDetached, out bytesWritten, associatedData));
            Assert.Equal(destination.Length, bytesWritten);

            return destination.ToArray();
        }

        private byte[] Sign(byte[] content, CoseSigner signer, bool isDetached, byte[]? associatedData)
            => isDetached ?
            CoseSign1Message.SignDetached(content, signer, associatedData) :
            CoseSign1Message.SignEmbedded(content, signer, associatedData);

        private bool TrySign(ReadOnlySpan<byte> content, Span<byte> destination, CoseSigner signer, bool isDetached, out int bytesWritten, byte[]? associatedData)
            => isDetached ?
            CoseSign1Message.TrySignDetached(content, destination, signer, out bytesWritten, associatedData) :
            CoseSign1Message.TrySignEmbedded(content, destination, signer, out bytesWritten, associatedData);
    }
}
