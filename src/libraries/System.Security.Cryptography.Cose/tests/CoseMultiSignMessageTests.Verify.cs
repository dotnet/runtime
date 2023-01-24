using Xunit;
using Test.Cryptography;
using System.Formats.Cbor;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;
using System.Collections.ObjectModel;

namespace System.Security.Cryptography.Cose.Tests
{
    public abstract class CoseMultiSignMessageTests_Verify : CoseMessageTests_Verify
    {
        internal override CoseMessage Decode(byte[] cborPayload)
            => CoseMessage.DecodeMultiSign(cborPayload);
    }

    public class CoseMultiSignMessageTests_VerifyEmbedded: CoseMultiSignMessageTests_Verify
    {
        internal override bool UseDetachedContent => false;

        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;
            Assert.Equal(1, signatures.Count);

            return signatures[0].VerifyEmbedded(key, associatedData);
        }

        internal override byte[] Sign(byte[] content, CoseSigner signer)
            => CoseMultiSignMessage.SignEmbedded(content, signer);
    }

    public class CoseMultiSignMessageTests_VerifyDetached : CoseMultiSignMessageTests_Verify
    {
        internal override bool UseDetachedContent => true;

        internal override bool Verify(CoseMessage msg, AsymmetricAlgorithm key, byte[] content, byte[]? associatedData = null)
        {
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;
            Assert.Equal(1, signatures.Count);

            return signatures[0].VerifyDetached(key, content, associatedData);
        }

        internal override byte[] Sign(byte[] content, CoseSigner signer)
            => CoseMultiSignMessage.SignDetached(content, signer);
    }
}
