// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Pkcs.Tests
{
    public static class TimestampRequestTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void BuildExpectedRequest_FromData(bool viaSpan)
        {
            Rfc3161TimestampRequest request = Rfc3161TimestampRequest.CreateFromData(
                "Hello, world!!"u8,
                HashAlgorithmName.SHA256,
                requestSignerCertificates: true);

            VerifyExpectedRequest(request, viaSpan);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void BuildExpectedRequest_FromHashAndName(bool viaSpan)
        {
            Rfc3161TimestampRequest request = Rfc3161TimestampRequest.CreateFromHash(
                "11806C2441295EA697EA96EE4247C0F9C71EE7638863CB8E29CD941A488FCB5A".HexToByteArray(),
                HashAlgorithmName.SHA256,
                requestSignerCertificates: true);

            VerifyExpectedRequest(request, viaSpan);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void BuildExpectedRequest_FromHashAndOid(bool viaSpan)
        {
            Oid hashAlgorithmId = new Oid("2.16.840.1.101.3.4.2.1", "Nothing should read this friendly name");

            Rfc3161TimestampRequest request = Rfc3161TimestampRequest.CreateFromHash(
                "11806C2441295EA697EA96EE4247C0F9C71EE7638863CB8E29CD941A488FCB5A".HexToByteArray(),
                hashAlgorithmId,
                requestSignerCertificates: true);

            Assert.NotSame(hashAlgorithmId, request.HashAlgorithmId);
            Assert.Equal(hashAlgorithmId.Value, request.HashAlgorithmId.Value);

            VerifyExpectedRequest(request, viaSpan);
        }

        private static void VerifyExpectedRequest(Rfc3161TimestampRequest request, bool viaSpan)
        {
            // Captured with Fiddler from a CryptRetrieveTimestamp call
            const string ExpectedHex =
                "30390201013031300D06096086480165030402010500042011806C2441295EA6" +
                "97EA96EE4247C0F9C71EE7638863CB8E29CD941A488FCB5A0101FF";

            Assert.Equal(1, request.Version);
            Assert.Equal(
                "11806C2441295EA697EA96EE4247C0F9C71EE7638863CB8E29CD941A488FCB5A",
                request.GetMessageHash().ByteArrayToHex());

            Assert.False(request.GetNonce().HasValue, "request.GetNonce().HasValue");
            Assert.Equal("2.16.840.1.101.3.4.2.1", request.HashAlgorithmId.Value);
            Assert.Null(request.RequestedPolicyId);
            Assert.True(request.RequestSignerCertificate, "request.RequestSignerCertificate");
            Assert.False(request.HasExtensions);

            if (viaSpan)
            {
                // Twice as big as it needs to be.
                byte[] buf = new byte[ExpectedHex.Length];

                const byte FillByte = 0x55;
                buf.AsSpan().Fill(FillByte);

                // Too small
                Assert.False(request.TryEncode(Span<byte>.Empty, out int bytesWritten));
                Assert.Equal(0, bytesWritten);

                const int WriteOffset = 7;

                // Too small
                Span<byte> dest = new Span<byte>(buf, WriteOffset, (ExpectedHex.Length / 2) - 1);
                Assert.False(request.TryEncode(dest, out bytesWritten));
                Assert.Equal(0, bytesWritten);

                Assert.Equal(new string('5', buf.Length * 2), buf.ByteArrayToHex());

                // Bigger than needed
                dest = new Span<byte>(buf, WriteOffset, buf.Length - WriteOffset);
                Assert.True(request.TryEncode(dest, out bytesWritten));
                Assert.Equal(ExpectedHex.Length / 2, bytesWritten);
                Assert.Equal(ExpectedHex, dest.Slice(0, bytesWritten).ByteArrayToHex());

                Assert.Equal(FillByte, buf[WriteOffset - 1]);
                Assert.Equal(FillByte, buf[WriteOffset + bytesWritten]);

                // Reset
                dest.Fill(FillByte);

                // Perfectly sized
                dest = dest.Slice(0, bytesWritten);
                Assert.True(request.TryEncode(dest, out bytesWritten));
                Assert.Equal(ExpectedHex.Length / 2, bytesWritten);
                Assert.Equal(ExpectedHex, dest.ByteArrayToHex());

                Assert.Equal(FillByte, buf[WriteOffset - 1]);
                Assert.Equal(FillByte, buf[WriteOffset + bytesWritten]);
            }
            else
            {
                byte[] encoded = request.Encode();

                Assert.Equal(ExpectedHex, encoded.ByteArrayToHex());
            }
        }

        [Fact]
        public static void BuildFromSignerInfo()
        {
            ContentInfo content = new ContentInfo(new byte[] { 1, 2, 3, 4 });
            SignedCms cms = new SignedCms(content, false);

            using (X509Certificate2 signerCert = Certificates.RSAKeyTransferCapi1.TryGetCertificateWithPrivateKey())
            {
                CmsSigner signer = new CmsSigner(SubjectIdentifierType.SubjectKeyIdentifier, signerCert);
                signer.SignedAttributes.Add(new Pkcs9SigningTime());
                cms.ComputeSignature(signer);
            }

            SignerInfo signerInfo = cms.SignerInfos[0];
            byte[] sig = signerInfo.GetSignature();

            Rfc3161TimestampRequest fromSigner = Rfc3161TimestampRequest.CreateFromSignerInfo(signerInfo, HashAlgorithmName.SHA256);
            Rfc3161TimestampRequest fromData = Rfc3161TimestampRequest.CreateFromData(sig, HashAlgorithmName.SHA256);

            Assert.Equal(fromData.Encode().ByteArrayToHex(), fromSigner.Encode().ByteArrayToHex());
        }

        [Fact]
        public static void BuildFromNullSignerInfo()
        {
            AssertExtensions.Throws<ArgumentNullException>(
                "signerInfo",
                () => Rfc3161TimestampRequest.CreateFromSignerInfo(null, HashAlgorithmName.SHA256));
        }

        [Fact]
        public static void BuildWithAllOptions()
        {
            byte[] data = { 1, 9, 7, 5, 0, 4, 0, 4 };
            Oid requestedPolicyOid = new Oid("1.2.3", "1.2.3");
            byte[] nonce = "0123456789".HexToByteArray();

            X509ExtensionCollection extensionsIn = new X509ExtensionCollection
            {
                new X509Extension("1.2.3.4.5", new byte[] { 0x05, 0x00 }, false),
                new X509Extension("0.1.2", new byte[] { 0x04, 0x00 }, false),
            };

            Rfc3161TimestampRequest req = Rfc3161TimestampRequest.CreateFromData(
                data,
                HashAlgorithmName.SHA512,
                requestedPolicyOid,
                nonce,
                true,
                extensionsIn);

            Assert.NotNull(req);
            Assert.Equal(512 / 8, req.GetMessageHash().Length);
            Assert.Equal(Oids.Sha512, req.HashAlgorithmId.Value);
            Assert.NotNull(req.RequestedPolicyId);
            Assert.NotSame(requestedPolicyOid, req.RequestedPolicyId);
            Assert.Equal(requestedPolicyOid.Value, req.RequestedPolicyId.Value);
            Assert.True(req.GetNonce().HasValue, "req.GetNonce().HasValue");
            Assert.Equal(nonce.ByteArrayToHex(), req.GetNonce().Value.ByteArrayToHex());
            Assert.True(req.RequestSignerCertificate, "req.RequestSignerCertificate");
            Assert.True(req.HasExtensions, "req.HasExtensions");

            X509ExtensionCollection extensionsOut = req.GetExtensions();

            Assert.NotSame(extensionsIn, extensionsOut);
            Assert.Equal(extensionsIn.Count, extensionsOut.Count);
            Assert.NotSame(extensionsIn[0], extensionsOut[0]);
            Assert.NotSame(extensionsIn[0], extensionsOut[1]);
            Assert.NotSame(extensionsIn[1], extensionsOut[0]);
            Assert.NotSame(extensionsIn[1], extensionsOut[1]);

            // Extensions is order-preserving
            Assert.Equal(extensionsIn[0].Oid.Value, extensionsOut[0].Oid.Value);
            Assert.Equal(extensionsIn[0].RawData, extensionsOut[0].RawData);

            Assert.Equal(extensionsIn[1].Oid.Value, extensionsOut[1].Oid.Value);
            Assert.Equal(extensionsIn[1].RawData, extensionsOut[1].RawData);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TryDecode_WithExtensions(bool withExcessData)
        {
            const string PaddingHex = "0403010203";

            byte[] inputBytes = (
                "307C0201013051300D06096086480165030402030500044060EDD3D91924EC2A2AABA0BD16997" +
                "4AF5A04BC5495342871CF52EF9AF8DF36BAB5B2E456B26C00B42147626C5ADDAAC986291091FA" +
                "7387D504A5BF62427176AD06022A03020501234567890101FFA016300A06042A0304050402050" +
                "030080602010204020400" + PaddingHex).HexToByteArray();

            var dataRange = new ReadOnlyMemory<byte>(inputBytes, 0, inputBytes.Length - PaddingHex.Length / 2);

            ReadOnlyMemory<byte> toUse = withExcessData ? inputBytes : dataRange;

            Rfc3161TimestampRequest request;
            int bytesRead;

            Assert.True(Rfc3161TimestampRequest.TryDecode(toUse, out request, out bytesRead), "TryDecode");
            Assert.Equal(dataRange.Length, bytesRead);
            Assert.NotNull(request);

            const string ExpectedHashHex =
                "60EDD3D91924EC2A2AABA0BD169974AF5A04BC5495342871CF52EF9AF8DF36BA" +
                "B5B2E456B26C00B42147626C5ADDAAC986291091FA7387D504A5BF62427176AD";

            Assert.Equal(1, request.Version);
            Assert.Equal(ExpectedHashHex, request.GetMessageHash().ByteArrayToHex());
            Assert.Equal(Oids.Sha512, request.HashAlgorithmId.Value);
            Assert.NotNull(request.RequestedPolicyId);
            Assert.Equal("1.2.3", request.RequestedPolicyId.Value);
            Assert.True(request.GetNonce().HasValue, "request.GetNonce().HasValue");
            Assert.Equal("0123456789", request.GetNonce().Value.ByteArrayToHex());
            Assert.True(request.RequestSignerCertificate, "request.RequestSignerCertificate");
            Assert.True(request.HasExtensions, "request.HasExtensions");

            X509ExtensionCollection extensions = request.GetExtensions();
            Assert.Equal(2, extensions.Count);
            Assert.Equal("1.2.3.4.5", extensions[0].Oid.Value);
            Assert.Equal("0500", extensions[0].RawData.ByteArrayToHex());
            Assert.Equal("0.1.2", extensions[1].Oid.Value);
            Assert.Equal("0400", extensions[1].RawData.ByteArrayToHex());
        }

        [Theory]
        [InlineData(Rfc3161RequestResponseStatus.Accepted, 0)]
        [InlineData(Rfc3161RequestResponseStatus.HashMismatch, 0)]
        [InlineData(Rfc3161RequestResponseStatus.HashMismatch, 1)]
        [InlineData(Rfc3161RequestResponseStatus.NonceMismatch, 0)]
        [InlineData(Rfc3161RequestResponseStatus.UnexpectedCertificates, 0)]
        [InlineData(Rfc3161RequestResponseStatus.RequestFailed, 0)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 0)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 1)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 2)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 3)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 4)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 5)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 6)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 7)]
        public static void ProcessResponse_FreeTsa_WithCerts_NoNonce(Rfc3161RequestResponseStatus expectedStatus, int variant)
        {
            const string Padding = "0400";

            string inputHex =
                "30820D2D300302010030820D2406092A864886F70D010702A0820D1530820D11" +
                "020103310B300906052B0E03021A050030820196060B2A864886F70D01091001" +
                "04A0820185048201813082017D02010106042A0304013031300D060960864801" +
                "65030402010500042011806C2441295EA697EA96EE4247C0F9C71EE7638863CB" +
                "8E29CD941A488FCB5A020306A5C1181632303138303130343136353634302E35" +
                "39373334385A300A020101800201F48101640101FFA0820111A482010D308201" +
                "093111300F060355040A13084672656520545341310C300A060355040B130354" +
                "534131763074060355040D136D54686973206365727469666963617465206469" +
                "676974616C6C79207369676E7320646F63756D656E747320616E642074696D65" +
                "207374616D70207265717565737473206D616465207573696E67207468652066" +
                "7265657473612E6F7267206F6E6C696E65207365727669636573311830160603" +
                "550403130F7777772E667265657473612E6F72673122302006092A864886F70D" +
                "0109011613627573696C657A617340676D61696C2E636F6D3112301006035504" +
                "071309577565727A62757267310B3009060355040613024445310F300D060355" +
                "0408130642617965726EA082080530820801308205E9A003020102020900C1E9" +
                "86160DA8E982300D06092A864886F70D01010D05003081953111300F06035504" +
                "0A130846726565205453413110300E060355040B1307526F6F74204341311830" +
                "160603550403130F7777772E667265657473612E6F72673122302006092A8648" +
                "86F70D0109011613627573696C657A617340676D61696C2E636F6D3112301006" +
                "035504071309577565727A62757267310F300D0603550408130642617965726E" +
                "310B3009060355040613024445301E170D3136303331333031353733395A170D" +
                "3236303331313031353733395A308201093111300F060355040A130846726565" +
                "20545341310C300A060355040B130354534131763074060355040D136D546869" +
                "73206365727469666963617465206469676974616C6C79207369676E7320646F" +
                "63756D656E747320616E642074696D65207374616D7020726571756573747320" +
                "6D616465207573696E672074686520667265657473612E6F7267206F6E6C696E" +
                "65207365727669636573311830160603550403130F7777772E66726565747361" +
                "2E6F72673122302006092A864886F70D0109011613627573696C657A61734067" +
                "6D61696C2E636F6D3112301006035504071309577565727A62757267310B3009" +
                "060355040613024445310F300D0603550408130642617965726E30820222300D" +
                "06092A864886F70D01010105000382020F003082020A0282020100B591048C4E" +
                "486F34E9DC08627FC2375162236984B82CB130BEFF517CFC38F84BCE5C65A874" +
                "DAB2621AE0BCE7E33563E0EDE934FD5F8823159F07848808227460C1ED882617" +
                "06F4281334359DFBB81BD1353FC179610AF1A8C8C865DC00EA23B3A89BE6BD03" +
                "BA85A9EC827D60565905E22D6A584ED1380AE150280CEE397E98A012F3804640" +
                "07862443BC077CB95F421AF31712D9683CDB6DFFBAF3C8BA5BA566AE523D459D" +
                "6177346D4D840E27886B7C01C5B890D78A2E27BBA8DD2F9A2812E157D62F921C" +
                "65962548069DCDB7D06DE181DE0E9570D66F87220CE28B628AB55906F3EE0C21" +
                "0F7051E8F4858AF8B9A92D09E46AF2D9CBA5BFCFAD168CDF604491A4B06603B1" +
                "14CAF7031F065E7EEEFA53C575F3490C059D2E32DDC76AC4D4C4C710683B97FD" +
                "1BE591BC61055186D88F9A0391B307B6F91ED954DAA36F9ACD6A1E14AA2E4ADF" +
                "17464B54DB18DBB6FFE30080246547370436CE4E77BAE5DE6FE0F3F9D6E7FFBE" +
                "B461E794E92FB0951F8AAE61A412CCE9B21074635C8BE327AE1A0F6B4A646EB0" +
                "F8463BC63BF845530435D19E802511EC9F66C3496952D8BECB69B0AA4D4C41F6" +
                "0515FE7DCBB89319CDDA59BA6AEA4BE3CEAE718E6FCB6CCD7DB9FC50BB15B12F" +
                "3665B0AA307289C2E6DD4B111CE48BA2D9EFDB5A6B9A506069334FB34F6FC7AE" +
                "330F0B34208AAC80DF3266FDD90465876BA2CB898D9505315B6E7B0203010001" +
                "A38201DB308201D730090603551D1304023000301D0603551D0E041604146E76" +
                "0B7B4E4F9CE160CA6D2CE927A2A294B37737301F0603551D23041830168014FA" +
                "550D8C346651434CF7E7B3A76C95AF7AE6A497300B0603551D0F0404030206C0" +
                "30160603551D250101FF040C300A06082B06010505070308306306082B060105" +
                "0507010104573055302A06082B06010505073002861E687474703A2F2F777777" +
                "2E667265657473612E6F72672F7473612E637274302706082B06010505073001" +
                "861B687474703A2F2F7777772E667265657473612E6F72673A32353630303706" +
                "03551D1F0430302E302CA02AA0288626687474703A2F2F7777772E6672656574" +
                "73612E6F72672F63726C2F726F6F745F63612E63726C3081C60603551D200481" +
                "BE3081BB3081B80601003081B2303306082B060105050702011627687474703A" +
                "2F2F7777772E667265657473612E6F72672F667265657473615F6370732E6874" +
                "6D6C303206082B060105050702011626687474703A2F2F7777772E6672656574" +
                "73612E6F72672F667265657473615F6370732E706466304706082B0601050507" +
                "0202303B1A394672656554534120747275737465642074696D657374616D7069" +
                "6E6720536F667477617265206173206120536572766963652028536161532930" +
                "0D06092A864886F70D01010D05000382020100A5C944E2C6FAC0A14D930A7FD0" +
                "A0B172B41FC1483C3E957C68A2BCD9B9764F1A950161FD72472D41A5EED27778" +
                "6203B5422240FB3A26CDE176087B6FB1011DF4CC19E2571AA4A051109665E94C" +
                "46F50BD2ADEE6AC4137E251B25A39DABDA451515D8FF9E07209E8EC20B7874F7" +
                "E1A0EDE7C00937FE84A334F8B3265CED2D8ED9DF61396583677FEB382C1EE3B2" +
                "3E6EA5F05DF30DE7B9F89005D25266F612F39C8B4F6DABA6D7BFBAC19632B906" +
                "37329F52A6F066A10E43EAA81F849A6C5FE3FE8B5EA23275F687F2052E502EA6" +
                "C30762A668CCE07871DD8E97E315BBA929E25589977A0A312CE96C5106B1437C" +
                "779F2B361B182888F3EE8A234374FA063E956192627F7C431073965D1260928E" +
                "BA009E803429AE324CF96F042354F37BCA5AFDDC79F79346AB388BFC79F01DC9" +
                "861254EA6CC129941076B83D20556F3BE51326837F2876F7833B370E7C3D4105" +
                "23827D4F53400C72218D75229FF10C6F8893A9A3A1C0C42BB4C898C13DF41C7F" +
                "6573B4FC56515971A610A7B0D2857C8225A9FB204EACECA2E8971AA1AF87886A" +
                "2AE3C72FE0A0AAE842980A77BEF16B92115458090D982B5946603764E75A0AD3" +
                "D11454B9986F678B9AB6AFE8497033AE3ABFD4EB43B7BC9DEE68815949E64815" +
                "82A82E785277F2282107EFE390200E0508ACB8EA82EA2505276F3C9DA2A3D3B4" +
                "AD38BBF8842BDA36FC2448291F558DC02DD1E03182035A308203560201013081" +
                "A33081953111300F060355040A130846726565205453413110300E060355040B" +
                "1307526F6F74204341311830160603550403130F7777772E667265657473612E" +
                "6F72673122302006092A864886F70D0109011613627573696C657A617340676D" +
                "61696C2E636F6D3112301006035504071309577565727A62757267310F300D06" +
                "03550408130642617965726E310B3009060355040613024445020900C1E98616" +
                "0DA8E982300906052B0E03021A0500A0818C301A06092A864886F70D01090331" +
                "0D060B2A864886F70D0109100104301C06092A864886F70D010905310F170D31" +
                "38303130343136353634305A302306092A864886F70D01090431160414029AC1" +
                "0A42471FBD0586C107BE51F79FA3080004302B060B2A864886F70D010910020C" +
                "311C301A301830160414916DA3D860ECCA82E34BC59D1793E7E968875F14300D" +
                "06092A864886F70D01010105000482020093555D4EA36895232E8D8E3FBAFFD1" +
                "B625FF0C61363411AD1ECF5A53DEBC6A233046539971BD8B50EEAC06E8CE72F2" +
                "DC12C28C01F3AEC0D8276955703E88FD043829F7E67A1781C5BBB949897FBD12" +
                "9ED0E81F252E35FE3E398453783C136A6FAC9B2F519936079C878AF389324D72" +
                "83C0396A94B432C52344BDC9F561110894978900B0EA8121AB937341A08F1BF3" +
                "109C41871CD81456C45F41DA306E164F143FCA9FC708C545B6F9A7032541C2AB" +
                "8B6A8C37114AB66AC142226C740EA695E701E434AE225A488E0484089C785F3D" +
                "873FCE5D8A8D75DE6AA6AB5915C5C11CE76263A463DF4BA07FE164D989DB9055" +
                "54B4207A2C622DF10808F0078F40CC75C7B2B9C161C11A17231C2EFABEB50047" +
                "E7FD76B13A011225ECFB9C8185E82A724C9175C4763D6353F1C3992AE9E6EF0D" +
                "6D32867DF84CD98F55AD0E1B260B48899019FA903F257FCC2DBA5893FF840A99" +
                "E22EB0B20E43868C75A2463E38740B79AF183CD5B6AE50D1D6FE5C2D397C2257" +
                "87C682AF575A7554201725C444747FD6C4644B0029B3BBFE39265ADA82020D5C" +
                "7EB9DEAAA4EEF9EF404CEEC73C4BC907E0E4006BD9CAA41852F12BE13B1279AF" +
                "62D502B5A721B4A4ABE3939DBE114E7C473F29719D1B580E8CE92BE2143E8DFA" +
                "480C07A6CAE881893678BDF0828F7286E47D76A251C6899F41C75728AFADABE6" +
                "6A47E3E28EB64E734356A6374E4CB05EC3" + Padding;

            byte[] inputBytes = inputHex.HexToByteArray();
            ReadOnlyMemory<byte>? nonce = null;
            HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA256;
            byte[] hash = "11806C2441295EA697EA96EE4247C0F9C71EE7638863CB8E29CD941A488FCB5A".HexToByteArray();

            if (expectedStatus == Rfc3161RequestResponseStatus.NonceMismatch)
            {
                nonce = new byte[] { 9, 8, 7, 6 };
            }
            else if (expectedStatus == Rfc3161RequestResponseStatus.HashMismatch)
            {
                if (variant == 0)
                {
                    hash[0] ^= 0xFF;
                }
                else
                {
                    hashAlgorithmName = HashAlgorithmName.SHA384;
                }
            }
            else if (expectedStatus == Rfc3161RequestResponseStatus.RequestFailed)
            {
                // Address determined by data inspection
                Assert.Equal(0, inputBytes[8]);
                inputBytes[8] = 3;
            }
            else if (expectedStatus == Rfc3161RequestResponseStatus.VersionTooNew)
            {
                // Address determined by data inspection
                Assert.Equal(1, inputBytes[79]);
                inputBytes[79] = 2;
            }
            else if (expectedStatus == Rfc3161RequestResponseStatus.DoesNotParse)
            {
                if (variant == 0)
                {
                    // Change the PkiStatus from a SEQUENCE to a SET.

                    // Address determined by data inspection
                    Assert.Equal(0x30, inputBytes[4]);
                    inputBytes[4] = 0x31;
                }
                else if (variant == 1)
                {
                    // Change the SET OF (digestAlgorithms) in the token CMS to SEQUENCE OF

                    // Address determined by data inspection
                    Assert.Equal(0x31, inputBytes[35]);
                    inputBytes[35] = 0x30;
                }
                else if (variant == 2)
                {
                    // Change the id-signedData value to id-data.

                    // Address determined by data inspection
                    Assert.Equal(2, inputBytes[23]);
                    inputBytes[23] = 1;
                }
                else if (variant == 3)
                {
                    // Change the id-ct-TSTInfo into id-ct-receipt

                    // Address determined by data inspection
                    Assert.Equal(4, inputBytes[64]);
                    inputBytes[64] = 1;
                }
                else if (variant == 4)
                {
                    // Change the id-aa-signing-certificate into id-aa-content-hint
                    // Now the signer has no ESSCertId (and it already doesn't have an ESSCertIdV2)

                    // Address determined by data inspection
                    Assert.Equal(12, inputBytes[2815]);
                    inputBytes[2815] = 4;
                }
                else if (variant == 5)
                {
                    // Alter a byte in the certificate required hash value, ESSCertId mismatches

                    // Address determined by data inspection
                    Assert.Equal(0xD8, inputBytes[2829]);
                    inputBytes[2829] ^= 0xFF;
                }
                else if (variant == 6)
                {
                    // Alter the signerInfo signature algorithm to say it's the PKCS#1 module

                    // Address determined by data inspection
                    Assert.Equal(1, inputBytes[2858]);
                    inputBytes[2858] = 0;
                }
                else if (variant == 7)
                {
                    // Change the TSTInfo.Version value, which breaks the signature.

                    // Address determined by data inspection
                    Assert.Equal(1, inputBytes[79]);
                    inputBytes[79] = 2;
                }
                else if (variant == 7)
                {
                    // Change one of the SEQUENCE values in ESSCertId to SET

                    // Address determined by data inspection
                    Assert.Equal(0x30, inputBytes[2820]);
                    inputBytes[2820] = 0x31;
                }
            }

            Rfc3161TimestampRequest request = Rfc3161TimestampRequest.CreateFromHash(
                hash,
                hashAlgorithmName,
                nonce: nonce,
                requestSignerCertificates: expectedStatus != Rfc3161RequestResponseStatus.UnexpectedCertificates);

            if (!SignatureSupport.SupportsRsaSha1Signatures &&
                expectedStatus != Rfc3161RequestResponseStatus.RequestFailed)
            {
                expectedStatus = Rfc3161RequestResponseStatus.DoesNotParse;
            }

            ProcessResponse(expectedStatus, request, inputBytes, Padding.Length / 2);
        }

        [Theory]
        [InlineData(Rfc3161RequestResponseStatus.Accepted, 0)]
        [InlineData(Rfc3161RequestResponseStatus.Accepted, 1)]
        [InlineData(Rfc3161RequestResponseStatus.HashMismatch, 0)]
        [InlineData(Rfc3161RequestResponseStatus.HashMismatch, 1)]
        [InlineData(Rfc3161RequestResponseStatus.NonceMismatch, 0)]
        [InlineData(Rfc3161RequestResponseStatus.RequestedCertificatesMissing, 0)]
        [InlineData(Rfc3161RequestResponseStatus.VersionTooNew, 0)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 0)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 1)]
        [InlineData(Rfc3161RequestResponseStatus.DoesNotParse, 2)]
        public static void ProcessResponse_Symantec_NoCerts_WithNonce(
            Rfc3161RequestResponseStatus expectedStatus,
            int variant)
        {
            const string Padding = "0403000000";

            string inputHex =
                "308203B23003020100308203A906092A864886F70D010702A082039A30820396" +
                "020103310D300B060960864801650304020130820122060B2A864886F70D0109" +
                "100104A08201110482010D30820109020101060B6086480186F8450107170330" +
                "31300D06096086480165030402010500042011806C2441295EA697EA96EE4247" +
                "C0F9C71EE7638863CB8E29CD941A488FCB5A021500D19949957B5677CF5F5581" +
                "630A597827BA80EFD6180F32303138303130353137303931365A300302011E02" +
                "0E3230313830313035313730373030A08186A48183308180310B300906035504" +
                "0613025553311D301B060355040A131453796D616E74656320436F72706F7261" +
                "74696F6E311F301D060355040B131653796D616E746563205472757374204E65" +
                "74776F726B3131302F0603550403132853796D616E7465632053484132353620" +
                "54696D655374616D70696E67205369676E6572202D2047323182025A30820256" +
                "02010130818B3077310B3009060355040613025553311D301B060355040A1314" +
                "53796D616E74656320436F72706F726174696F6E311F301D060355040B131653" +
                "796D616E746563205472757374204E6574776F726B312830260603550403131F" +
                "53796D616E746563205348413235362054696D655374616D70696E6720434102" +
                "105458F2AAD741D644BC84A97BA09652E6300B0609608648016503040201A081" +
                "A4301A06092A864886F70D010903310D060B2A864886F70D0109100104301C06" +
                "092A864886F70D010905310F170D3138303130353137303931365A302F06092A" +
                "864886F70D01090431220420ACA421A6482F4722320ECF53223F8D15099329CA" +
                "4ADFD71EC562631F522C85553037060B2A864886F70D010910022F3128302630" +
                "2430220420CF7AC17AD047ECD5FDC36822031B12D4EF078B6F2B4C5E6BA41F8F" +
                "F2CF4BAD67300B06092A864886F70D010101048201008F4020CFAE55355A0545" +
                "1A1250CCE1439A2DDD62915C81A1C7661888A74F9D0792922051CD426792D3A1" +
                "ED3DC47C6AF2281A9A02ED89C605BB9FB7FD63FAF27335FE45A7681E5904C68C" +
                "C30E5DBB37D127C437785F07BD2EF20C31EB0341AB2FA6F9D70C43ADA15C082E" +
                "E630D64E59CBB06918F094D6B5B19C9C74DC7B203E2F86EC638761E244B279DB" +
                "DAFDC87143288A488398FDFAABBAD82D992EFC9845BE9ABF19D00754E4064D24" +
                "6C8B2C16012FA147B25000570F41C2BE9126082095A4CCA3E2FA3C5C694C1E6B" +
                "BC7BFF4CA8EA692A07B8B9E6AB8E3114701080923A9A83DD6A4257C4248C865F" +
                "C51BA0D8DA57FB5692039F4B102608AECA217204BBD4" + Padding;

            byte[] inputBytes = inputHex.HexToByteArray();

            ReadOnlyMemory<byte> nonce = "3230313830313035313730373030".HexToByteArray();
            HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA256;
            byte[] hash = "11806C2441295EA697EA96EE4247C0F9C71EE7638863CB8E29CD941A488FCB5A".HexToByteArray();

            if (expectedStatus == Rfc3161RequestResponseStatus.NonceMismatch)
            {
                nonce = new byte[] { 9, 8, 7, 6 };
            }
            else if (expectedStatus == Rfc3161RequestResponseStatus.HashMismatch)
            {
                if (variant == 0)
                {
                    hash[0] ^= 0xFF;
                }
                else
                {
                    hashAlgorithmName = HashAlgorithmName.SHA384;
                }
            }
            else if (expectedStatus == Rfc3161RequestResponseStatus.VersionTooNew)
            {
                // Change the TSTInfo.Version value.
                // Since the certificate isn't embedded the signature check doesn't happen.

                // Address determined by data inspection
                Assert.Equal(1, inputBytes[81]);
                inputBytes[81] = 2;
            }
            else if (expectedStatus == Rfc3161RequestResponseStatus.DoesNotParse)
            {
                if (variant == 0)
                {
                    // Change the id-aa-signing-certificateV2 into id-aa-binary-signing-time
                    // Now the signer has no ESSCertIdV2 (and it already doesn't have an ESSCertId)

                    // Address determined by data inspection
                    Assert.Equal(47, inputBytes[634]);
                    inputBytes[634] = 46;
                }
                else if (variant == 1)
                {
                    // Change one of the SEQUENCE values in ESSCertIdV2 to SET

                    // Address determined by data inspection
                    Assert.Equal(0x30, inputBytes[639]);
                    inputBytes[639] = 0x31;
                }
                else if (variant == 2)
                {
                    // Corrupt the structure of the TSTInfo

                    // Address determined by data inspection
                    Assert.Equal(0x02, inputBytes[79]);
                    inputBytes[79] = 0x04;
                }
            }
            else if (expectedStatus == Rfc3161RequestResponseStatus.Accepted)
            {
                if (variant == 1)
                {
                    // Tamper with the hash in the ESSCertIdV2.  This will be accepted because
                    // the cert is unknown.

                    // Address determined by data inspection
                    Assert.Equal(0x7A, inputBytes[646]);
                    inputBytes[646] ^= 0xFF;
                }
            }
            Rfc3161TimestampRequest request = Rfc3161TimestampRequest.CreateFromHash(
                hash,
                hashAlgorithmName,
                nonce: nonce,
                requestSignerCertificates: expectedStatus == Rfc3161RequestResponseStatus.RequestedCertificatesMissing);

            ProcessResponse(expectedStatus, request, inputBytes, Padding.Length / 2);
        }

        private static void ProcessResponse(
            Rfc3161RequestResponseStatus expectedStatus,
            Rfc3161TimestampRequest request,
            byte[] inputBytes,
            int paddingByteCount)
        {
            Rfc3161TimestampToken token;
            int bytesRead;
            Rfc3161RequestResponseStatus status;
            bool result = request.TryProcessResponse(inputBytes, out token, out status, out bytesRead);

            Assert.Equal(expectedStatus, status);

            if (expectedStatus == Rfc3161RequestResponseStatus.Accepted)
            {
                Assert.True(result, "request.TryProcessResponse return value");
            }
            else
            {
                Assert.False(result, "request.TryProcessResponse return value");
            }

            if (expectedStatus == Rfc3161RequestResponseStatus.DoesNotParse)
            {
                Assert.Equal(0, bytesRead);
            }
            else
            {
                Assert.Equal(inputBytes.Length - paddingByteCount, bytesRead);
            }

            switch (expectedStatus)
            {
                case Rfc3161RequestResponseStatus.Accepted:
                case Rfc3161RequestResponseStatus.HashMismatch:
                case Rfc3161RequestResponseStatus.NonceMismatch:
                case Rfc3161RequestResponseStatus.UnexpectedCertificates:
                case Rfc3161RequestResponseStatus.RequestedCertificatesMissing:
                case Rfc3161RequestResponseStatus.VersionTooNew:
                    Assert.NotNull(token);
                    break;
                default:
                    Assert.Null(token);
                    break;
            }

            if (result)
            {
                Rfc3161TimestampToken token2 = request.ProcessResponse(inputBytes, out int bytesRead2);

                Assert.Equal(bytesRead, bytesRead2);
                Assert.NotNull(token2);
                Assert.NotSame(token, token2);
            }
            else
            {
                Assert.Throws<CryptographicException>(() => request.ProcessResponse(inputBytes, out bytesRead));
            }
        }

        [Fact]
        public static void EmptyNonce()
        {
            byte[] sha256 = new byte[256 / 8];

            Rfc3161TimestampRequest req = Rfc3161TimestampRequest.CreateFromHash(
                sha256,
                HashAlgorithmName.SHA256,
                nonce: Array.Empty<byte>());

            Assert.Equal("00", req.GetNonce().Value.ByteArrayToHex());
        }

        [Fact]
        public static void NegativeNonceIsMadePositive()
        {
            byte[] sha256 = new byte[256 / 8];
            byte[] nonce = { 0xFE };

            Rfc3161TimestampRequest req = Rfc3161TimestampRequest.CreateFromHash(
                sha256,
                HashAlgorithmName.SHA256,
                nonce: nonce);

            Assert.Equal("00FE", req.GetNonce().Value.ByteArrayToHex());
        }

        [Fact]
        public static void NonceLeadingZerosIgnored()
        {
            byte[] sha256 = new byte[256 / 8];
            byte[] nonce = { 0x00, 0x00, 0x01, 0xFE };

            Rfc3161TimestampRequest req = Rfc3161TimestampRequest.CreateFromHash(
                sha256,
                HashAlgorithmName.SHA256,
                nonce: nonce);

            Assert.Equal("01FE", req.GetNonce().Value.ByteArrayToHex());
        }

        [Fact]
        public static void NoncePaddingZerosIgnored()
        {
            byte[] sha256 = new byte[256 / 8];
            byte[] nonce = { 0x00, 0x00, 0xFE };

            Rfc3161TimestampRequest req = Rfc3161TimestampRequest.CreateFromHash(
                sha256,
                HashAlgorithmName.SHA256,
                nonce: nonce);

            Assert.Equal("00FE", req.GetNonce().Value.ByteArrayToHex());
        }

        public enum Rfc3161RequestResponseStatus
        {
            Unknown = 0,
            Accepted = 1,
            DoesNotParse = 2,
            RequestFailed = 3,
            HashMismatch = 4,
            VersionTooNew = 5,
            NonceMismatch = 6,
            RequestedCertificatesMissing = 7,
            UnexpectedCertificates = 8,
        }
    }

    internal static class Rfc3161TimestampRequestExtensions
    {
        private static readonly MethodInfo s_tryProcesses;

        static Rfc3161TimestampRequestExtensions()
        {
            s_tryProcesses = typeof(Rfc3161TimestampRequest)
                .GetMethod("ProcessResponse", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        internal static bool TryProcessResponse(
            this Rfc3161TimestampRequest request,
            ReadOnlyMemory<byte> inputBytes,
            out Rfc3161TimestampToken token,
            out TimestampRequestTests.Rfc3161RequestResponseStatus status,
            out int bytesRead)
        {
            object[] parameters = { inputBytes, null, null, null, false };
            object result = s_tryProcesses.Invoke(request, parameters);

            token = (Rfc3161TimestampToken)parameters[1];

            status = (TimestampRequestTests.Rfc3161RequestResponseStatus)
                Enum.ToObject(typeof(TimestampRequestTests.Rfc3161RequestResponseStatus), parameters[2]);

            bytesRead = (int)parameters[3];

            return (bool)result;
        }
    }
}
