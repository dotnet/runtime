// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using static System.Security.Cryptography.Cose.Tests.CoseTestHelpers;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseMultiSignMessageTests_SignStream_Async : CoseMessageTests_SignStream_Async
    {
        internal override CoseMessageKind MessageKind => CoseMessageKind.MultiSign;

        internal override void AddSignature(CoseMultiSignMessage msg, byte[] content, CoseSigner signer, byte[]? associatedData = null)
        {
            using Stream stream = GetTestStream(content);
            msg.AddSignatureForDetachedAsync(stream, signer, associatedData).GetAwaiter().GetResult();
        }

        internal override CoseMessage Decode(ReadOnlySpan<byte> cborPayload)
            => CoseMessage.DecodeMultiSign(cborPayload);

        internal override CoseHeaderMap GetSigningHeaderMap(CoseMessage msg, bool getProtectedMap)
        {
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            Assert.Equal(1, multiSignMsg.Signatures.Count);
            CoseSignature signature = multiSignMsg.Signatures[0];

            return getProtectedMap ? signature.ProtectedHeaders : signature.UnprotectedHeaders;
        }

        internal override Task<byte[]> SignDetachedAsync(Stream detachedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null)
            => CoseMultiSignMessage.SignDetachedAsync(detachedContent, signer, protectedHeaders, unprotectedHeaders, associatedData);

        internal override bool Verify(CoseMessage msg, IDisposable key, byte[] content, byte[]? associatedData = null)
        {
            Assert.True(!OnlySupportsDetachedContent || msg.Content == null);
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);

            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;
            Assert.Equal(1, signatures.Count);

            using Stream stream = GetTestStream(content);

            CoseAlgorithm algorithm = GetCoseAlgorithmFromCoseMessage(msg);
            CoseKey coseKey = CoseKeyFromAlgorithmAndKey(algorithm, key);

            bool result = signatures[0].VerifyDetachedAsync(coseKey, stream, associatedData).GetAwaiter().GetResult();

            if (key is AsymmetricAlgorithm keyAsymmetricAlgorithm)
            {
                stream.Position = 0;
                Assert.Equal(result, signatures[0].VerifyDetachedAsync(keyAsymmetricAlgorithm, stream, associatedData).GetAwaiter().GetResult());
            }

            return result;
        }
    }

    public class CoseMultiSignMessageTests_SignStream_Sync : CoseMessageTests_SignStream_Sync
    {
        internal override CoseMessageKind MessageKind => CoseMessageKind.MultiSign;

        internal override void AddSignature(CoseMultiSignMessage msg, byte[] content, CoseSigner signer, byte[]? associatedData = null)
        {
            using Stream stream = GetTestStream(content);
            msg.AddSignatureForDetached(stream, signer, associatedData);
        }

        internal override CoseMessage Decode(ReadOnlySpan<byte> cborPayload)
            => CoseMessage.DecodeMultiSign(cborPayload);

        internal override CoseHeaderMap GetSigningHeaderMap(CoseMessage msg, bool getProtectedMap)
        {
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);
            Assert.Equal(1, multiSignMsg.Signatures.Count);
            CoseSignature signature = multiSignMsg.Signatures[0];

            return getProtectedMap ? signature.ProtectedHeaders : signature.UnprotectedHeaders;
        }

        internal override byte[] SignDetached(Stream detachedContent, CoseSigner signer, CoseHeaderMap? protectedHeaders = null, CoseHeaderMap? unprotectedHeaders = null, byte[]? associatedData = null)
            => CoseMultiSignMessage.SignDetached(detachedContent, signer, protectedHeaders, unprotectedHeaders, associatedData);

        internal override bool Verify(CoseMessage msg, IDisposable key, byte[] content, byte[]? associatedData = null)
        {
            Assert.True(!OnlySupportsDetachedContent || msg.Content == null);
            CoseMultiSignMessage multiSignMsg = Assert.IsType<CoseMultiSignMessage>(msg);

            ReadOnlyCollection<CoseSignature> signatures = multiSignMsg.Signatures;
            Assert.Equal(1, signatures.Count);

            using Stream stream = GetTestStream(content);

            CoseAlgorithm algorithm = GetCoseAlgorithmFromCoseMessage(msg);
            CoseKey coseKey = CoseKeyFromAlgorithmAndKey(algorithm, key);

            bool result = signatures[0].VerifyDetached(coseKey, stream, associatedData);

            if (key is AsymmetricAlgorithm keyAsymmetricAlgorithm)
            {
                stream.Position = 0;
                Assert.Equal(result, signatures[0].VerifyDetached(keyAsymmetricAlgorithm, stream, associatedData));
            }

            return result;
        }
    }
}
