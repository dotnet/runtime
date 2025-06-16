// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable SYSLIB5006

using System;
using System.Collections.Generic;
using System.Text;
using Test.Cryptography;
using Xunit;

using System.Formats.Cbor;
using System.Linq;

namespace System.Security.Cryptography.Cose.Tests
{
    public partial class CoseKeyTests : IClassFixture<CoseTestKeyManager.TestFixture>
    {
        private CoseTestKeyManager.TestFixture _keyManagerFixture;
        private CoseTestKeyManager KeyManager => _keyManagerFixture.KeyManager;
        private CoseTestKeyManager BadKeyManager => _keyManagerFixture.BadKeyManager;

        public CoseKeyTests(CoseTestKeyManager.TestFixture keyManagerFixture)
        {
            _keyManagerFixture = keyManagerFixture;
        }

        [ConditionalTheory(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        [MemberData(nameof(AllMLDsaCoseDraftExamples))]
        public static void DecodeSign1MLDsaCoseDraftExamples(MLDsaAlgorithm algorithm, byte[] mldsaPk, byte[] sign1)
        {
            using MLDsa pubKey = MLDsa.ImportMLDsaPublicKey(algorithm, mldsaPk);

            CoseSign1Message msg = CoseMessage.DecodeSign1(sign1);
            Assert.NotNull(msg.Content);
            Assert.True(msg.VerifyEmbedded(CoseKey.FromKey(pubKey)));

            Assert.Equal(2, msg.ProtectedHeaders.Count);
            Assert.Equal(0, msg.UnprotectedHeaders.Count);

            if (msg.ProtectedHeaders.TryGetValue(CoseHeaderLabel.Algorithm, out var alg))
            {
                Assert.Equal(GetExpectedMLDsaAlgorithm(algorithm), alg.GetValueAsInt32());
            }

            if (msg.ProtectedHeaders.TryGetValue(CoseHeaderLabel.KeyIdentifier, out var keyIdentifier))
            {
                // this doesn't seem to correlate with anything provided in the test vectors therefore we just check the length
                Assert.Equal(32, keyIdentifier.GetValueAsBytes().Length);
            }
        }

        private static int GetExpectedMLDsaAlgorithm(MLDsaAlgorithm algorithm)
        {
            if (algorithm == MLDsaAlgorithm.MLDsa44)
            {
                return (int)CoseTestHelpers.CoseAlgorithm.MLDsa44;
            }
            else if (algorithm == MLDsaAlgorithm.MLDsa65)
            {
                return (int)CoseTestHelpers.CoseAlgorithm.MLDsa65;
            }
            else if (algorithm == MLDsaAlgorithm.MLDsa87)
            {
                return (int)CoseTestHelpers.CoseAlgorithm.MLDsa87;
            }
            else
            {
                Assert.Fail($"Unrecognized MLDsa algorithm: {algorithm.Name}");
                return 0;
            }
        }

        [Theory]
        [MemberData(nameof(AllKeysAndSign1Implementations))]
        public void TestSignVerifySingleSignerAllAlgorithms(string keyId, CoseTestSign1 signerImplementation)
        {
            byte[] payload = Encoding.UTF8.GetBytes("Hello World");
            byte[] signature = signerImplementation.Sign(KeyManager, keyId, payload);
            Assert.NotNull(signature);
            Assert.True(signature.Length > 0);
            Assert.True(signerImplementation.Verify(KeyManager, keyId, payload, signature));

            // we try different key
            CoseTestKey differentKey = KeyManager.GetDifferentKey(keyId);
            Assert.False(signerImplementation.Verify(KeyManager, differentKey.Id, payload, signature));

            // we try bad signature (or bad key with good signature, same thing)
            Assert.False(signerImplementation.Verify(BadKeyManager, keyId, payload, signature));

            // we try fake payload
            if (!signerImplementation.IsEmbedded)
            {
                // embedded ignore payload arg
                byte[] fakePayload = Encoding.UTF8.GetBytes("Hello World 2");
                Assert.False(signerImplementation.Verify(KeyManager, keyId, fakePayload, signature));
            }
        }

        [Theory]
        [MemberData(nameof(AllKeysAndMultiSignImplementations))]
        public void TestSignVerifyMultiSignerAllAlgorithms(string[] keyIds, CoseTestMultiSign signerImplementation)
        {
            byte[] payload = Encoding.UTF8.GetBytes("Hello World");
            byte[] signature = signerImplementation.Sign(KeyManager, keyIds, payload);
            Assert.NotNull(signature);
            Assert.True(signature.Length > 0);
            Assert.True(signerImplementation.Verify(KeyManager, keyIds, payload, signature));

            // we try fake signature
            Assert.False(signerImplementation.Verify(BadKeyManager, keyIds, payload, signature));

            // we try fake payload
            if (!signerImplementation.IsEmbedded)
            {
                byte[] fakePayload = Encoding.UTF8.GetBytes("Hello World 2");
                Assert.False(signerImplementation.Verify(KeyManager, keyIds, fakePayload, signature));
            }
        }

        public static IEnumerable<object[]> AllKeysAndSign1Implementations()
        {
            foreach (string keyId in CoseTestKeyManager.GetAllKeyIds())
            {
                foreach (CoseTestSign1 sign1 in CoseTestSign1.GetImplementations())
                {
                    yield return [keyId, sign1];
                }
            }
        }

        public static IEnumerable<object[]> AllKeysAndMultiSignImplementations()
        {
            string[] keyIds = CoseTestKeyManager.GetAllKeyIds();
            int[] nrOfKeys = [1, 2, 3, 3, 5];

            for (int i = 0; i < keyIds.Length; i++)
            {
                string[] keysToTest = PickNKeys(nrOfKeys[i % nrOfKeys.Length], i, keyIds);
                foreach (CoseTestMultiSign multiSign in CoseTestMultiSign.GetImplementations())
                {
                    yield return [keysToTest, multiSign];
                }
            }

            static string[] PickNKeys(int n, int atIndex, string[] keys)
            {
                Assert.True(keys.Length >= 1);

                // If n is larger than the number of keys, we just wrap around and use same key multiple times.

                string[] ret = new string[n];
                for (int i = 0; i < n; i++)
                {
                    ret[i] = keys[(atIndex + i) % keys.Length];
                }

                return ret;
            }
        }

        public static IEnumerable<object[]> AllMLDsaCoseDraftExamples()
        {
            yield return new object[] { MLDsaAlgorithm.MLDsa44, MLDsa44PkHex.HexToByteArray(), MLDsa44Sign1Hex.HexToByteArray() };
            yield return new object[] { MLDsaAlgorithm.MLDsa65, MLDsa65PkHex.HexToByteArray(), MLDsa65Sign1Hex.HexToByteArray() };
            yield return new object[] { MLDsaAlgorithm.MLDsa87, MLDsa87PkHex.HexToByteArray(), MLDsa87Sign1Hex.HexToByteArray() };
        }

        private const string MLDsa44Sign1Hex =
            "d2845827a201382f045820b8969ab4b37da9f0684e42647eb8a0be8b5b661ebf5d76f0583bf5b8d3" +
            "a8059aa0581d68656c6c6f20706f7374207175616e74756d207369676e6174757265735909742657" +
            "237b7520fd4cb8803f69a6e4ab613f4816420cd38e6474e548a370c6f0a18851ce8b7bb1b43c658b" +
            "795303d0f22d23aad9afc7077877ab77d7cc92947bcf800e09626d7ceb809f74d2dc435200b272ec" +
            "c92a993901087a42eaeaa6b9009df00f26055e6032ccca2995bf9c455e93c95adb9dda970ba07d77" +
            "8a9b4950169b289a86ec272bb810f9506b960941fa4ac804de49cb80f9bd54f51adef76670c06f94" +
            "bf948ad7675ab28aa3254944753aac0cdbd8594752a438552e846fb476be3e31df0c91222db5e5d7" +
            "0bddb05b624a78103654d4e9ec514f6be91cfe8fa3b8529b2659a89e70227f35d0059362ed51c752" +
            "3bf4a8ca7ceb0da6216bea77576548cd98f5ad6f87326facc8b308debce4461f1f2c4b190bd4950e" +
            "ec52cb66da70c9913e8a476826a0ea05edd8f2d3ca53e485ffcebc4e7ae33aeeb1d8dc3ee6b8d09c" +
            "ea138377ceeaed4fef57d868c16311e18c64b9df501791a6142085083850b3ad2e74901298c09b7f" +
            "c4d87a660031e955b39cf9e6fbbe3cae5b36360f6b61f904771d55d542fbc68be5468738f5b8c44e" +
            "b624da535a112c0266f79b9ae7ac996feab2c5874c65f59a72bf671b568d06e57b89f6fa168f4805" +
            "0f869e9fe0b95490487597e1746d7f54ef04eca32710bd4655a2269fd9afdfa0c7630c09ad59273d" +
            "5d76f6bc026b623e5fee4fe3978efb4fdc5f905d8a346259cad9cd8ad826cdea818fcca6804bd78d" +
            "dddb70d46d723ec63980fe7bb2eb8dab84692cb6f6a560eb80381dc0d5ded38d1de896772702f996" +
            "37f6b9a9b207be86e2a401187bb250f68230f7840ecf9787bb6073e2e29f1287cd73bdf1dae8302f" +
            "cf23f942305c4c9807aba037af66f8b278003c98a30084f9ad3f2e4c4b31eb1b3f20170c70f0310f" +
            "71932a4e0065a2bd79eedc70e59f9cc261aed96fd7ebec86be2490789ad0dffc76f4cccc28ed675a" +
            "769edf9f8d6e9fd78d59393687fb19b641626f70bbed7c6496a3a1393be6751f533e7af8f20f9ef3" +
            "2c7b58b231feb4231aa407ecf5e0be7921c449a537ab58871b4cef2f8b1212b189ddc9e207b0ebe8" +
            "135be534b30f25ce0aa33371a94971da4b6b78bb2cb708035b539f3706348d1f6ef0e2ab9c741f1f" +
            "fce5bd34c20c2ded6272c583188d2f48404cbd10f6aa759fecb1e5b87c755573db0d86ef17fecd72" +
            "31179f47a19b0bcdafadad9a8b20dfe1d2792cc2d78d13c76722739d6c31563bc938fb07a0bc5d96" +
            "d3a4e852141815b526ac74fa210c48ce1e2ffa3faa682191aea55a476a6cd7e0ab42902180b1444a" +
            "2e08302c17608b5831daa4c4008dbb54f0b4ce566c069ed48d4a9c5b542816f3156cde0d7323bb07" +
            "1cccc98ee35672248e873b5907d02a153a57e5777c6767fd75e833df46813c2abe44dc6492e8de44" +
            "87f4fa1d1377d4ae273d28869c6630ba4865e65676d9dc9ca0998a0082e95c78314d543068f6fd38" +
            "a27bdbc98f8b5fefa21e704e4bc8ac7ed46ea5c03eb700cf0e549b8a1c50b5d051bd7c2588938f7c" +
            "9f5499e7b95430b1e567a2e36b4a55252829d7fb319c7edab4e19108fa2a784c96ec1027f19f5714" +
            "48132b6c8c4441a7a7488ddda530b84ba0221120c95311eab37660b1329a70365117eebbb7e0240c" +
            "c5052ec723e0121c2a175053c762b88943ac7b965d10239c4b8f8d39a1a57ace097a1631c7e93c36" +
            "abc8a085a21a18a14b621cff49369707891e06e508e41970b26490c8f5c038bcb2e62a72d24591f5" +
            "63c42fed3dfa3539f75dacbc7918919642220a01da483a2c0413360e424c6cc30dfc502858a57ffd" +
            "c20d30bb57c1659a7d4beb6794c4675524e813a27e3807547d0bc16e91242d7925b01f0a8cf03f5c" +
            "6e867710373ad02e53816f82a21b2c9f359e7d586ec0590c0a1780a6755e1723981ebd866d251e20" +
            "a0a5b2dc08e05beb325797aa7c2746596c534964cc751ff341d49e39c8b6f8a903549779189c5732" +
            "b841abde352eddff9ffb67f20b9c27d30078994ac96c8250b3428c65a714c05c91c897a18ee58f90" +
            "8557062bd733444a9d73ed89a637c62143e46e1cb3723c6a8fd2df0d90d03b6cdfb4e6c033f67c51" +
            "a803b6eaea79e0ecfe4a3b22c5dc951d51683ea716149958c59ab43f1085d8e5896aa3c8d972d549" +
            "98d3de2b27c2d67e0059b78dff6f804cd491dfae0308b4c8983ea1c574b4414df8ca772fbb60dc49" +
            "249f8dbab9c43357016893f7a4b2eb28c0a8de635157b717e20ad60d5a52d37e2ebf5b87dcdcccdd" +
            "d1f40825d56b948e60015118e8988f6000dd157ce92a0f0ec1d5459890317ee861a0d29f73053310" +
            "47886e1918b8438d1df534e685c93f2f11317b000b0bd7da766e5f1d4a0816a7af878be4c8dc8fdd" +
            "208abd5c7f98aa0e882772387ef5032f60e71a7c1c630a8eacdde2a7c5e86277b20e1317cd8b9892" +
            "e8509647d55143dccca07ffdd678d5856eaab93f55df72ff4c909146de54393aeed095cbd9fc1a24" +
            "b7f7950cb80eb423ed114cdc21e59593b2a5fcbbdf1613810fd63c8dd45e39bc5bd02d71328cfea8" +
            "7d2deadda75089ca7d4529e0b5b64fb887fc38cb9531033386255c6a155af95447b2154354e6d163" +
            "b752bef91f248b5068f3e620365c8c497cfcbe61930d0cf08387308310f485bfa23c31bf2d01900e" +
            "801352a388c97212ef58b6a81f5082f08831433a7ca8c0df910cc462b36d61f532325eeee540547b" +
            "6c07c738b010daf7384f8cf01975761101e556e8639848dfd049ee5360bb9b62bb38aef0fc84970d" +
            "ad3e78c0f3413573042abe52805b5aec545bcb43142f5d44a9c1d2b6cdf3ded20907f02ebc78e78f" +
            "598beadd0fc1faa676560edffbd7a83b61795bc29b6fbe4c7c6e9097139dbb85b54a8b446a37f2fd" +
            "6a7db528f1c5da5fe367823f8fa39adae0bd23196f689059e2de3cfcbaad6bec710464156cd72be7" +
            "0d5950075953286feb605f6898746586750e3aef767b0e80136453c1ab388ff5462bfc0316ed7893" +
            "7ea235dd883e9fedbd66f9060b542272ac9747fe3109a27a89403fc1c2380ccb1e3f199077582aa5" +
            "65fba4621092c5665f2f7803f5ecfdaf86878ec045a780ea3751bd32333cd02fef8b4eb9386f51fa" +
            "7a5f3bb81c55fb0de38c905ba4002dadfcc5123bf561bef2d32c40577dc487736162c69444279d91" +
            "7abd0d2320fb715299c1043defb582a20fec3190a6c0e484360910388889c122c4a13adc73031a09" +
            "69e3c1a9008d8467c4c4d59c848d9ca2441ec57b02034fd5872b4cf75185d5fb14e6af1aead0e172" +
            "7db42db39877f01d674558f7b59b0e0f10363e3f505d82a7c0c7cadd1618233541424f5759647677" +
            "7d80a6b8dfe6eefcfd0515196c8e99c3cfd2ebf2020b0c16202b3337484e525657a4b5bec3cad2d4" +
            "d5d6dd00000000000000000000000e232e45";

        private const string MLDsa44PkHex =
            "ba71f9f64e11baeb58fa9c6fbb6e14e61f18643dab495b47539a9166ca0198131c44f826bbd56e34" +
            "e55db5e5e2d733485e39ea260fc6000c5ea4ba80d3455cde53b46f34482aedfd5450fc2e1ba4f25d" +
            "15f9c144242fb39bb52287189030c50498e1717b7c758b190a6748ea9aa3f7acaaf2c7cb526ed717" +
            "c9f79aeb84214fa5cd8ded92a0c3fa1558810f12c7050a367708d196cd24e5af974904aed8e4ce88" +
            "72e8696b0b7bca50e452cd7d30ea9a4adac0311d672c6bde8496240b07431463708895cd9bafc316" +
            "32d7397649388fdafcbf7d305a3de9a495eca7433a8f83ba0f0b25c413c6e39c96eb7d691b34d37c" +
            "e37f1eead1cf217e25ef34eecf3f7c60f84b8edfdde8405d4f832576c61ef98e0a2f28da18770095" +
            "3924f686b94614705bcf53d33fedd4348edddbdf28b5065e1f20775043e85cf931f829179363a1a7" +
            "e7404a838ec00086b0976386fe637c98244757e3f769ddd4467471bfad670f9a05f8246ee50a7b1e" +
            "af87fc4069c3ae2aa2033258117792f0bcd49e083fd1bc7496abff29cc94e4868b21214ed3165253" +
            "99a610fbdd4a80e7c80715f29578e2a84bb40bdddbd9f47a11b6e7da118a1b658d359e8aef55eb46" +
            "b5376b5b655979984a922beebfc59bcd600d5309dccd72dbf0787db8ba757b537c1eafd5c0f50ea4" +
            "bc9583549e2829a42c28cac248c96d78124c47159b18aedd754aba17b19d430fb78f633ea9d26f54" +
            "a9bd50f8d8f6b73594f828976e7ea09c53bbb9f11a56c9507fb89b9a5ebc037a37267a95f85b8d64" +
            "ca97192b10a66f417b3f61fe9ca57130a48fd925eae2ab5502d571c8a51903c1d398f4c1f76a7e11" +
            "743976afdbc697f23094a3cd761ff9685de32e09fb3c28add453490300bc7c89dc01780096071722" +
            "945775f264e1b0623bcf4619c712c838761205d87691b75ef360196cbb9e9b92a0d4c4ed62326e50" +
            "24d77510b8ee2c7426cc22eae209dc9f13bde6bf08f5e7181bd3b459450b451a51539a715c21d67d" +
            "d330eb5970db00d9edbfb2822b036fa13bafeb86d8dc78866e3f8d43e53d78cca5595a6faf886b5d" +
            "c112f1cf4adcfa875800d90b48883af97316fe1506873fc157e570eacbfd222868d14234101966af" +
            "b6bf9940829253a953ada89fc756b6a849f70acb9838e69faa50bba75e3e89c2adb57e86d088ab9b" +
            "04a28e670709172243ec5e0008a5ceaf3f8722f487302596ffd755ad1b82a49c34b3469515b46aa2" +
            "90cd86ee38ea7a9be3f103610335b531cca333ddfe32b14510f4b07ef95fc6684e8c454a92c10dbb" +
            "5d59c7a7c63fb305fe881967d99e669eb632840582560bb403431d40f75a4954908482278292821f" +
            "4ea91e42e78fa48caee3c836146dcfd738d117e92e9a15137d28e8e6a4b4622650cb413504cb3a33" +
            "5d44beec5746c1c294b1e8cb99cb608d928f8ce3563632c521f23d13c61a8f61c01df8c96c7360db" +
            "4f3c68aa5d2fdd342a62ff3459c116389421ab43e8584c45882b50e6e4e96db6f0b8fde890d5dbfa" +
            "dcd88690b449e64240ddb2023747f308363e301aa77757169fc6150628d5920b5aa1ab1c8cbf44cb" +
            "00e025d7879d72b479e3af5311c785725590da9c89b9fc3b8450769554eb44d203eba2bbaef9cad2" +
            "237011c2ea44eff00f299a48ffe28ca93ddf85f76608242ef8d6cc24610a1e2078fcac4f9385c314" +
            "905ecaa82e553916d94d1a7c1ec652aa08897083daa2ebb1775fbc471ae27777d7904ea9f1b92bca" +
            "c3d8a3158426087b645b1108f0d65fec93789c053743ca14fd63d05e98b652df2b9c2ff9ce05f194" +
            "0703ffb273f80e0e2732eca9960d981b4cfd3b7bb8045b3c3830546b9dd8db0d";

        private const string MLDsa65Sign1Hex =
            "d2845827a2013830045820b788acf242f1f1d6532926d816e76e1636874267f2a48c84c4e65789ab" +
            "80cc02a0581d68656c6c6f20706f7374207175616e74756d207369676e617475726573590cedd5bd" +
            "2448903e4f81fb949158eefdeb93e2f40e58d3ffe5703d23954aeb547b2f490226b7e4bc617a9015" +
            "6acd6afa662c0a5fe83be1f9e2d458436f9b9119c853c71fa7c7591b6471d9d68366d5bf12833c18" +
            "2ac927f7f0edd816e52ecea715c66e71e35029083fd26d0f16040e1da74b378950429fae8229af04" +
            "95104549e2de909d6f8be09fcfc982e08425da663c181e862510b647f2f679ec16b7226fae6a9b90" +
            "d8131c780a984b231c45811156470c143a5a9a611248532b574d40c0ef9728264892ad97d523ca91" +
            "46a8f965996dda13bc7eacde9040a7745a92790c2ec6672d8a665761495c873ddd4b9dc347db786c" +
            "cfeabfb4f584bae9086f43639ade01f6c81a8f15d3c01ec9aaf0b04699c38163de65967cc921acc6" +
            "6935cdbea43f393d9f65303a4640c081a6073f762fd78c532911ecc60400688e329d7bca72d24fec" +
            "7c8cd307130f0dfb37ce333470501d9e2ff16810ede1fc811873fe8b38cf1c656d1927c190d240c0" +
            "020514b9e71f6ad14fee3baac3444111c6a1a1676dc92036e481c35b9db29a6282fa619a8b011026" +
            "5b870f57c9b42d48b223c348b0621f55654fed735bae9344bae117deb583ab54e66a26f360468c47" +
            "e3e40f553127164bb3eb803d17cb76d18d576d942db7c18b5870fb26699b13e91f15c75b35d55eb2" +
            "b10f6ffad617ee2c77b6bfaf2fc1b2a4cb2703a528959f80d02e9325c88aff95cd51351cb6992e4e" +
            "04ff124968d790056eef96664ed015c4563ec71807022f6b92d8542a0feda0b8190ac2db5ea9c967" +
            "836cda38839ce3bd5f46369bdb752fec8b047f4fb4608d6b21afc294564ac9d943566237f7a6dcce" +
            "bc1805cef60303f6058d43b7b612cce12232e5a895f9e5237da5461b8ee17907b7caeb08d25488f8" +
            "0c786c849103d4c44c2c6bca1b57e9a3b55f307c9c299e322a9ec81abfcc5f38fe036fb17fa34374" +
            "8ef746f0e31350d05a47d0f37002b55624df95831c72ddce2dfd91382879b1673f5fcb1600c65d56" +
            "0034ee163eeb5c11164ef88efed87f4e364fcd6e9d6cea384a62afbbaf34a6b4bdbd1b270a733a80" +
            "4d2f58703cc99a91e8ce88d992f685b08d7ede6d36fc821e5094cc69085896f60b2a9d9cacb0c4d7" +
            "7bd44eab94f11638b4798c3e462b8e020e4f22f0e14782051f16f2d7cb314dc24d4820549ff27ad4" +
            "58408d1a663f5f5fc22a4e921ff26c97fa84c5f12d35ad9c89310d0c9c075ba373024a1dc208f5f1" +
            "7c592b5b5c3bdf4129bf304b2b731d383b844ffc48a234c0d07ff8ff550619f6b6eff3cad399c1a2" +
            "b61bd4aa68a7fd86cf661f73a309c3bafa512b6fc81f7702857d350744958be7050aac6d1f040bfd" +
            "866df38727df3bfd1ff3896f68550dfcb520c308fea4d1716790b1b6d51ef9c815e05d537c644608" +
            "93beb9d82c350393ad15992e1c1ba16ff59a87c5d6fa19b4e88e2c433e0e96ffc6a8a7d49f84769f" +
            "f9057bef8daf353e8516a852247e2f17ff13c81be266fff7c916c9b726a83058c66ac0366335ee6e" +
            "7b079095cf367bf79a3cc38da62d53e84a3b1a4ca97f40dd147e0d6c90dec5aa93c178096884fc77" +
            "18a675eee7900e4cb3ccc3601a08bf0003c3a029ca62a1924cc5bb83b29817f892c5a5e7253abeb5" +
            "36d58d885008914a94bb2747f8a22478f35490d6f9693d0ff50073289adda762b62823a9e4b13447" +
            "8642d9f1c44e20559bc5506df6baf76056c9cfbf15bb7134cd95f29527f006a0a49ebc4bb8e8ccfe" +
            "3757a1f61c83a25ef44d2856f15d13272de73bfe726df6a775b18157c85d419d20a7614dc18eb74d" +
            "fb26af89fb2996ebcefe37dbdff37d3d2408411f9aad75f6d2cae122bf90e51ad6c4f6bbf85c50a5" +
            "0e78afaf86fa5e367d00c4fdade27148949fb8db485eb7950d63c90013313db410ecf9b314a94c10" +
            "2dc8bf7e9e27ffdbedd64b9441bc687a534874739c52759d1af213bf8ebd916e456561973f822e26" +
            "aae6827b06ec4fcd45c146ac5c6637168e024c188f93315dd57e7fb8a12879d1a83fbd2421368a1d" +
            "bf54898b487951c24ad2535a0344d7f7380808d44b207ac16b490c51155d275da3b863f775a13c84" +
            "83f05c76aa6b64e8faf96fb2ff78672361d139183abe3957c6f431b342779e2fa96b07de7a530469" +
            "d7096c01567c0c1ec7d3556d0ac636a9482a84aef2087ad2c2bbb5fc49739c16d771203529b1134d" +
            "a0d0373a4e2305741711a21016a132cd213fe2867b37465a103b68e16ce6ada0cbe1da2a0590f2a6" +
            "d1afa8e06e29b4dc3c9ae21ef6ca67e3c34a0e8f43dfaa0882d24e7fcc770ff28450efa19b88de83" +
            "e8327e499b155529745473ce9e1da81e9ce0fa1a816100c8d08741bfc8260fb0a6624c373b5823b5" +
            "87b34d16d1bddb6a03501f6e8ccac59b877ee751cc841f2290eb8c37fbf119b93dbe6b0a700e3ee8" +
            "e7a697b80d1a304a71e3c1ebe734a412a8403c80d9ca3096c3a764bf8f6524427efd2648210a387f" +
            "dcfbd05e4bbb6c353437750324b320458aaff555fe41765bb827c3c43d80bee1ef45dd3993d06ab1" +
            "245e9c95aa7976f54ba17aa031c8694e9b167a986cc289e534f1359f14ae335f7c41683dc85ccaf4" +
            "ee2b4c1cdd2116552f396ac8d6567e0f458c8cc0342086c31c0f8bffa3ac0d31677b10494c45e68e" +
            "66432b3f270a25cd389c126943b1d877ac6396d88a2df32c74eff79b9dbf1504b3cd55bcbbfa8ab2" +
            "a16979dfa53631a5d7d948bdc26c37eed9d2e2855338d029365b63b6b22abc211ed2ac1d3974550d" +
            "2d783be4c8b286fd8868a7c221ba15a527b1ccd14c50fc85907016930691f44f593a9c4ed3a1cec2" +
            "4f026735b719275fe27af036d234baeb812c5d60babae2f2b7032f0ad34a09cf98537a8b623f266e" +
            "ee28151acaa735af300ad6ce3e33c982b46db37479d5e3ad808b22b1453451dee5dbac26a03ae649" +
            "90917b7060ee48281e1b8c486218a8c20d371f621fdd4466254c5d3cab08fc07dc96b41c83d75537" +
            "7fe0363d11969802431cd4f2ff5cb92eb362591f12cf6f69fcd25727309235aa75acdd915c5a0940" +
            "3194a27b2f3b11cf51240ffeb0a457d383dd49503d3021ee19e83ef1b5d7f0aa243c7a4b69978e1e" +
            "f33911ecc320351a1e459ee1f672be88db2f0f5755758468a4509d067f5edafb45334179d1317a41" +
            "30e45320019cdc3113222c7933f0d12f3a71b23461cb9ebf072c3f7001797c9124bb7f39778c7b39" +
            "3eeadeee2f6fd9ed76f39d16291722bf9bf68761e307438649ee7e0042e7801e8c46d741fb216b13" +
            "ab8d243c608d7d5cc6cc758d429c90b9ac1dc1275314bd506fbd4e41767c8e8ec02282375b4f9e2d" +
            "77b78c1c00dfd527c07506d0803dd2b9963535281cb9473f03c37fc34b22aca3fea6630dc1f53e7c" +
            "e938c9dbe3550076fd724675107f2cbdf186389f189492f6388da43baf6f9ea72982f665dcb1ec9f" +
            "861021ee974abb8d0e36da8187dbb5dbe0c7100f0c07fb6c0702e84e9591ee3c6cd9ca2482079556" +
            "559ed691dbd97dc0bb1f052d64a938e260795192a876f97bf34097eb4380cb16e7415f58021fdf7d" +
            "ec9df8e521575b62d618bfc331b7efc3ea92394f73a0808df15e8794818649d9675edf3daaed3c51" +
            "70a843d448bd1ec5d2e8e5dfd4254e334f4ad27d73b614fe0f8542a0a644f6f824422e8e1e10cd12" +
            "5b9363da6f015354baa244921f8960ebc44f97ad1a29330ac6adbce3269922e9a1990feb9e4c89a7" +
            "e34368a04b79f5db62cda84af2ba028594de966674fa11ed21634922f8e5b4dbc0b9c9c899881dca" +
            "ba8d6724d114b231b1dc3088337a45070f5846c742f6184b0f0a1e55fe87bf37822cfc3ddb356c39" +
            "7ef85d9c1c0c65db191a9d03469096c2ce42b919145708e3ee8b35e8d72db1c738d3a4389ae996f9" +
            "604ea6903e61ac0bbe56c8ba108cda00d1bdcc6904644705c9a858adc8cdc08f4449ef11f4d0e285" +
            "50586478ac6c8a8c8aed3927ca90e3b31fc8f5722aa68ad028642c14706b8ab0e413201305f9f1a8" +
            "99f2ddd5fb6eff9985d0e57009956bc24f1d2c7b420eb3716a284df6408e38cedc4c7ec1c11c205c" +
            "8567cda8b12d4d8d97691015be532160a5a1731d8af5bd17a35f0d958ca423abfd1c6346f9472ba7" +
            "d7aa70b845ff343acdf9153aa939bcd101f0578fafe84d4cc77c5b67eff3bdbc5bea27b703d4ca3c" +
            "b5c4f4943855ff512517b2c57535bcca7726e7c2cc739dc65cf805b018167ce1324ea5578f9af037" +
            "8eb281c2a3b28fdab5775a4249bbe587c06077eb20c1ddab672d4206cbcb0d48b461b92bdee42494" +
            "08f132e3a36e63e8ebd8dced63ef150da21c8264bdc65379a39f0331895e6d589444d9dbd56f7626" +
            "252d7145905dab7ed44ab0d14707fb1c19198196da8fc7388056a7a59fb0e19cc05d88ce6a60802c" +
            "73f9d785b48992318ae993397044f43c38709c319ef5a8e68a452bc5b79bd86ae50981e58f7cbc58" +
            "c7e17946804ab019c18a570c499e8b425a600201ef63a40f7d918b60ec9eeba668201cdab4624c35" +
            "fdc014cdfaf2e7749e056f195f1eefc1949420e5569c461bc26f888b1aca0418552ad2dc1c5b62e6" +
            "c972b60ba643344d52cbdade3286497595a5adc1c40d0f10366cc9dbf9fb0e22445d5e7ba14c759f" +
            "bfd1d400000000000000000000000000000000000006080f181f25";

        private const string MLDsa65PkHex =
            "424b2f267e58d5b3b44d71acfc6a656bb26950d57c61db1c880bcfa1feab443f0942ab8bdbad7d70" +
            "8abbc356078f6d99a252271fe62c74091eb94afb9b9264c50a888e0dfed80cd5fb2cbd3667e60d53" +
            "9ebe44930219cd4faed15dbb3455a264802b9f49bce42ee7550feffdd4642a55ade693868a460cbe" +
            "c03f4fc99a4e30bccffa8a475e5395396674ebb81a94937587880f6dbd27bf1c4f5a9ee43cdd8b0e" +
            "53b3b7fb49c73adfbc2d4f8c54303520c29bf97e26ee57db342d957c893936522d0942b41d82ee37" +
            "72a00570adfb545c1143922b0496f826a0a970064b36ddf534b5f8e1c1cd0b5565ea846b45431f06" +
            "18143ece89777bb3f61179ad20295fe0a6e062ae6eecbc2ef38f2ac1a22dc93b7b126336223c55b6" +
            "1eb8c0795542bbb2dc65e722eadc6866ffa9683beb8a999ad7a83e5e6e016c2e4c35f6f7649ad3bd" +
            "52ec67ec1c5c6e7b9972771218be9554bba7727f0b84c44b9b0a8bd831fcff2c9779ccd4ca30c6ad" +
            "75b04983e41de893ee5f39ea7355180b709c7045c22d33a083f6ae07a114746d1bfdccbee5b90438" +
            "79bb5a2e120e2a4636283f4a1cd4924a2de6a4aa3d99ddd88f48aaa4e88bfd1ea769d82c10779f2d" +
            "ed796db542971ca289b76863ede5997b7e9ce183b43ccec278b10d92b87442ce0435bb1625171db5" +
            "554b470239c50d2a0c3a41b2a38807db070b47bfb3e7d10f3cd979d69963c8d79f8029cc4a48eb04" +
            "fcb3d708844febaa8b6ddff01ab64d59358e6505c4ec1d7cbb14ed2212df458ecefc03fe03037b15" +
            "05a4c9444322f5f98dfa91a4cb8c45860a2dadc7515350bb6d431e49a6bc8f5ba956e682b0e51332" +
            "1a97d1962602891c9078f62a8a9646a31387a6f09684264837899e0d8ec7d11c565901298b20b345" +
            "081690eb4c562c1aa3a25bef06566cb34c79bc0b25e4095d6ba793e81311e41a3329152686f00d48" +
            "97f84fc4edf4b26d545365785ead8d63aef64a87c0b91a2e5500383956cdf5f6e37cf9d5482d1c8e" +
            "3a5be38f17259ac45c9fa1c4bd3bf177d312ee52a6da023c05722a8738274dda8d1b04e99831cf57" +
            "c87282a256c565c296d0524a063a3a41a48a83009978d98d8abf61af68e8013b594fe151d9bec199" +
            "902c4c70b49584201743c6b53103d2fd24bdf078dc90b5a188b4f8d772179988d0416c94d4c57c08" +
            "60b9d7b53d4cd261f332a1851565d52ac37f008747cafe320f363d9beb6e4117db43fd8aeebe5e0c" +
            "e2f54e3f0367eb3cc971bbe0c301a8e52f96094936035c6ee3ca2d13db483a0dd04dc16247de0e08" +
            "94ad7cb7e1ae7ebd4f8f900582b20021e77f70254501c6ac3dd15d43bbb7931c5283244312158c2e" +
            "b1b3e1117e194f0a1e4c783efbc62c9f81c21562d0d34a5f042b5eaaf32f31f95c5b055f4e7a2070" +
            "fb096f56c415549cde74f3864e8b9fc27e3299724b4639986044b55928fd6972785b280c25a3e21a" +
            "ab814ecbfb0c3cbec0914907ec907f25a1d88bce3d319ae8222a35945db62af7cc75cd29c1f5d98f" +
            "cb93f750dc3031076979bb51dfc37d23e8eea78073a24d3e26c68e7bb10e459f2577b90080359ae0" +
            "aec10318dcd9e0f9e34029c31b3e54b1855645db420618783346dad5b55eddb4f977b326a655525e" +
            "be2195eca9cec38a3c0d2273b77d3e68f1901c2ca5149734a51177bcb089476b18cba09fa8b9b46d" +
            "94a2946f358e1decb1998652c58a90852423e2c85e79d19724461627e6390d1a81fb1a72f9c7edc4" +
            "bd747dd5c85217b5856141028414ddbe71458f0a0b2b589df2e1b051783b8f718676b1defbae98ba" +
            "496c2a935e92eeadea0a8393ef59f9e914f0743fe65640ddf9981cea6dbdd957a534ad4e790efc97" +
            "4ee89938ad99d53c5b680775399326834729bb37b082e795f8d87f52e6c8a8db68e515c277bbea82" +
            "a7570d4280896c987a0608903e306c632a223c55f0ea3682039c4a3f5440f4b5ac3e6ed2b2dc900c" +
            "ecc72b72f50e49b2629ad30f0487b2707b86286f8c4f55659b25f9bdd7a6af460cc3c57a3982663b" +
            "b717461581e196894929d84153d87a7f482d284b5b894ce1a78216b2a011f2b88742cee52d5133e8" +
            "fe77edae242f5af91637c37ffca32430509b2fe4756303a9a3659fe32528af1e10d8d43bea991b2d" +
            "109786cc66d35b1d78df254b92cdaa40f91a987e4a922ca81050e5bc3530ca85493bdf2a825374d0" +
            "a8310a6860284ec3ec732326eeeffc42bbd42bc91b73e5e7c6b599d016490637629f3876c3e42f8d" +
            "b590e66a85a7838c818f78fffb4853cbef09434989803545dca87657cf7c7e7e6afa71382bc10fa0" +
            "bb6480f243eea1b861101006fa0cff3275621943cc58eb4dc3a0428a5e425670fe82268de71c511d" +
            "8ffbdc11b0d0f961120e971015ad5f448886b802e3fac11672319d487c84f1001339cb969784cb57" +
            "344f2807f8b425f1d73caf8496d742ed237f4c9fcd5a4e84fba7e27fb1a8ae12c4f0427ae24e910d" +
            "951bd8c35d61f8a678db01caea8ef789a95b62ee1b8c5d32c6baa536ba88a1070ea61aabbf59294e" +
            "3f6f974c4c91cafc5bbf6b7ecfd57a18fb7557d71e06e900d281b0b49aa00feabb35714af33870ed" +
            "d7ac2393d93177f79ee5606c9df176f025ce49a6e5ff51a2a412ebf86ac0f40471c96ad4c119df23" +
            "0be6173df530ed656cbd8069214741ecdd0271c603fb6c4a8614ff878d33e726cac6693e938ca3fb" +
            "a82c4995c14a2d4af9014fe4c4c50b794cac596b52189f66a7106fb325b526ea";

        private const string MLDsa87Sign1Hex =
            "d2845827a2013831045820d9bc439f97bd6d4093e68f0f3fcf09c9a97adf888ed7308dd565247a16" +
            "6cb4faa0581d68656c6c6f20706f7374207175616e74756d207369676e617475726573591213e132" +
            "b492fc022d5fd1d2205f52dbf1ad1aaef3ece4622b4e3875696d64d66dccc74df743c1b85552a0f3" +
            "c1bdaaa8f789a15fdb3ce6329021f5815316cda1da5b012f1ccea4a47ef7e93eff319048ac9e3b6e" +
            "d46cd58c6557af1b340da3bd7966f1588f8bd88e05383aee12a7248db3aec96ba5d9afd1d79d6286" +
            "5eee04cac9cc2176ae585ae914d614805d916c142b4969be7ad95a44bf9c154d19bd41d8a3882ad6" +
            "f0b0802d1e037c7579453a0606bbbb31db164fc607646477572c63b71720f8d47bbb7615dd264f58" +
            "29f726e22740cb3a1e1b5e381c4f692f7ecaa0979ae17aea3139d733491fe213eeddcd5f68e06ee7" +
            "1b80f14ed693f407ce6e199cb3edb048d3e2905ce75b31bd6837a1d4b5eefa35431d0ca407200e60" +
            "768b2dc5b0370e91a6c03c3d0e5c47225616034f55fb0a30a66fd2074847be3c1230b93650d11949" +
            "2efc20338af0c4cb6a176191d7c5bbc7427f6f0c9bb49a0d73e7f026cf3855547fc7ca9369733313" +
            "963ffe4647e155a93cfa5403edebfc7842e75dad9ae2accba720487e476ffe3bc0a60cf322714292" +
            "42023d0d0a8f6e4e77a874afd2074a11ff20bae28d00fd5d990f839ca99c6db28a55da94a785290a" +
            "6b536893a237224639717ba5cf833d57db2cca0a7aeeed874597c6ee71e0dc35e06851e9d2bd022c" +
            "37b5fbcd2a4d5e8daea98a44cb9c97df43a0aa512005358c8d5d5db88fba610e47d5a863f53f9ec7" +
            "a8f3cb0b0ec2f02b1dfe9867a1437e84e941392b149275e868b959c58b9e814fb618c61208cb6838" +
            "81247bb0dcab96e84a77e0195b4e93f693c1e98dcb99e495632f6cd5839d3bcccdfa2bf6b2692175" +
            "9d0a293595a96e6ddd42c83d8d9a7b10a001b34f47c20fd46d1e09100e532e5b1900b89f14400bbc" +
            "dd5ee0cf61a1ca353398a498da488b0f117effcf999f5aafe4a587deaa3ff78cc431637adcbb4e40" +
            "ec385fac23e8176b74e0e750460f7d2002bf7465944caa2708835d3849199732090b7c514575311e" +
            "f9999c9bfcf737a4d906af914d0507f5a7c2e61ff12359999d173f88db9ef85a6d71ac2e3a8074be" +
            "d9472e00aedac26c48ab9c2d1ad96eeffe6e200686efa17086317a541ffad5c8b5707279aecb12ca" +
            "48f7e7d755e8cdc2bc990c6391abf9351c2f5305bda2c57bf54e419dce477947a64de07eb0432e5f" +
            "1cc87234ed673fa810d562095d0d0eed260bef5fc3daed8506756acb9059257b025471d8df2d4e69" +
            "7a3e7c74c47081b569519e1de636a971668a376b4d84d95c30a554894475be8f01bda7c6d7898957" +
            "2aea4c4e976a408b4a04b406e79e176163dcc1ffa7a7e9bd6ef6f854128097aa760ed3d6ad279f7e" +
            "a4ad6003f43cfb75de79c0bf8113a6f788e35e92f30185695f4abb18e924b29abf218e708977cb27" +
            "76a7abf46a41e46afe24863eed4fe890916f95d5b1fbe6cea096ceb4ad478fc994214f59c5e68c8b" +
            "9695c27f8fada32c90ed324925540912da750451f033359177579e4e4a04e5d5b9bfa72616df63df" +
            "20f17038e7d4bbe61562583046c35e3a71a0f596f119ef183786ed76e1da6d3be98277db1583c8f2" +
            "c8784c08f5c098abfd31baa9fc6abdc0cc441b6f93961b6630c45b9e7dda60d88be7c9577b6fbc5e" +
            "df65de4ed0b53f4377ce83b1e55c4d62015569b96ea094644e0f9cbe89cff4c539f77c629a5101c2" +
            "59c56cb9d31e20160dfe28386b37e610c2db9ecf6a000bfb2a85756e585ae6b97915e5113970946d" +
            "f068e6da7f0af0a48802b9e0464bfac7e0c6b7dee953665061ac7486d9eee3bf21137383e97eb393" +
            "a708e91a94f5012fa1d072c04f5c5ca2bbf894e7b275805fe5d81341d75b9f7fcb89b3ff7da2b623" +
            "c35d717d0da7180e258384ff39a914c2f30f893af4e1520d64a15bb0997b852f3ec6ca398d245361" +
            "fc2297c83867f388c8aa35e704d3f7081a961c528751faa64fd7efbcc03bed69e99ae1517d499117" +
            "227cb08254d7b8aa079530af39fb19b246d45a4d41a9955095636c786dcc3c30c817ee3c8e60689f" +
            "d9a494c9e774463df7b884a78dfe80f1e247b3f122401b0834e54fdeb57d7835f0409126983e40d8" +
            "922bbd54981e2b651cc3fdee9193468c041201c2c472749300250628225052f935d37ab9dd8e466b" +
            "6a3acb63ee93023013443ddeb91347b84eb6ecf37423996d682967a5aab7ccaaa73a690a9cbd45b1" +
            "5ee38f2ae698aac3cb2117ecc9160870993a3e50ea7647b0c03cb5f6daa290492dd693a0b8a9a975" +
            "9d0d977b662d45c4af5dce95084062f7a0f39d0bc3bdabaa31d8f815047244303e6a6e5d977ec757" +
            "a56797f0630ef1d4f02f7a0e7680c0865c4404c97bbf7014bfa64d0f89b02a2b981f616c0e16090c" +
            "4b7fe9998a0232469b0c059d3daff58af6148cee567e52f1a41730b28d6af79358ad3679cf4c3dec" +
            "0df780eccc91d004a4ec0a4d20cc6771dbb3f42da69dc2140994c228a60adc0e97438a2332db657e" +
            "91e5b7f5029541d13ce8456d93bb4933522c94e16ee4766af28b5754b5a74c71efb6ff445dc1aed5" +
            "f3e21cfe512cf9862489582925c763251376279f69e633a43aaadcf4c104744cfca747f477c1af8c" +
            "18f65109b06680cd392d14e3e0ed0ba117643c8794bc8d036e85222d543063a01f91cb9bceb80d88" +
            "4b1305d0065ef3555d6f64be4893816e0e9be111c65c9cb1840774ddd4ed5906ae2a73c7ca6103ec" +
            "d3bb506747681e192dd6c259b66dc63c39dbdc33dac1d564b54db4d8de935370abca642e05c6e95e" +
            "e827fb71d6086691bc6d1be2a0c7e491ce22163d3e8d48541cd7aca76eecc93f4e02ac0179cf7073" +
            "d5000b3ff338d096bc87eada3f4019fdf1a5d12470a33f65c2990c217680710069de75e0338d1e26" +
            "c179b1cf9ef853edaaab2c0519026b3d9f574b82b5bd316686b26c9e9e87870041fdfa1a5c538cb9" +
            "8cb39200856b4c9bdacaa1c7717a22b883c06ca0f78229cd59362614361cc5ec76d836d2dafc1ed5" +
            "1066afa7297db869508cc80543b1efcddc62eb7c4ffabe3fafbc02f4802b4992ff0be194ae123880" +
            "ceee6187e08c7db96b22438ac4bdabadd8aba7af68b8e2ac0c60c0f9aad5e4e1bf886db18212e9cb" +
            "46d8bc201e4a1a6fb231d91a309dbca30b6b1269ec31bda1bb6b6d8ff41d84baf7db7ac30de5ad5d" +
            "9259398bbfb5ffbb89cce88a15f0f7413c9c71487b56eea23573697a583fa57d3537588ba5558363" +
            "d8abd679f2968dbca3ff00b141d68d5baed53fa66480029f46f6b195e3dc99a0e09f990841c62735" +
            "db8e4b6b867ee416cd946b65fae48acd2c580c5ee461fdc78c2d671abb657f9296f976cb74c02496" +
            "76a111e7608785ede2acde6f8af297f88ce9ad0e6b4cfdac1c586519042ac700a4178f5c23add663" +
            "4c26588f47bfb7ec9e244ae8c71382042c24606410ef598ccfcaa459923e748d608caf3c82dd141c" +
            "8eb32a500a03bb7fbd5d628b625fecd3d9399254d7c569ab1418c6af0db64009f09d457e287b0e8b" +
            "83bf40d529379464df999d7519a454bb3fa9684f398c965c4f980061d33fd8883c5ad2964806f27f" +
            "dfb09458b1bb2daaced0fbc0c68d46d62224f63725932b58ae0a694ec7fa0d4e5fcdc75840b467e1" +
            "2514f1cd006befcf3410cf7a5c81adf3d29cd93ef5680c1a953daa0645273d0f5fdc8e6e0f8c632b" +
            "67a674fb4f390c392720ec6d8e3f1234e84f8420b91217ad71f406b1a4c7f2c438fff7844a097cdf" +
            "b00a2f1a94ee6bdd597756a754681111d66040c13a661b978440b05ba8c16e2a4255ebff56e5adad" +
            "a6fb92292d501d30e351f4fb5b907d9f1510e9801489da0a3cf4997eed6df4fb06b86f81af373578" +
            "1513c6654e030a03e358970fa129fdb8cb49365a86f1cdd1a9b5f966794c8bca163c3af148406c24" +
            "f0e149da338e6a1fb5f365b1a6bf0fe426ec424823588dce11dfe7de3b3aa740d27fac9b6d9c6090" +
            "9b4afe2f88dde858069e330e6f9a7ecc779022d3925ea0bd73e67041945e04691152683453f3126c" +
            "b3699b607dd598af05fd441c157bb3b8d69243705cd1e71442b502b7ea987c8837a3bd896e5bf279" +
            "6052a23d302c70b23a62383278e1f3c878c2bdcb68524c078fc73148f227951566c19248240d972d" +
            "5547350909c63d6f505ad889884fe9710154d2ec05aa15a4f734e5b88480916ec73e1518fbd26059" +
            "54580ec2b0a8f9c4bd4d075461b6b3015c344e83382c36b161e57a6c3933e98209ce308190531f85" +
            "f5d5fd9451d37f40f6f36af830b376ed2d48ab20b2b58b7c6e5956b7d4142b19ccb19a88db70e582" +
            "9047751950e2975bb4d0e9991fc3bbc4fa5adf2d9d1e25e4ded5396731bd2808b8227a30233cc7a1" +
            "ab7759623357547e46060a4c6c54a8d24116680186ca97291afd2be4ffdc9bea1c4b81ad80a3e7be" +
            "17fb5585eeb72dfc030655040993fe12f58d21d3ea499c1ecac70725f2e133450e9c1e75ce657300" +
            "f85ae0f44e470336dbd5df32fbc0a8ffbe3c66058e45ac5fb0ea3889313214b6b2ec98a91e6414b3" +
            "dda04d9c41857707bbdcf4763ee3846c7f4df034e1dc8fefc8a2a5dcda9f91940cdcd1f7b98b93c0" +
            "8bc9f1c198e80fcde8a5effe4ea4363a56cae57de4a7248fd8bde5767f1ae699b5bd998d9f613306" +
            "346472e96954dc32baccd31c3f44b0f11b8e6810bb3f27af7de6a57550288f56015b1b76f1c1e492" +
            "d5b998493b72a38ba4f2619b891aeccfd96c27fe80b958e08728581ca4d6e7da5f2760b48734a049" +
            "e8fb29aa6ff373911d712091ef6bbed204da3b1237c1195654c7f66aa6776e950e7a27aed6c9a4ff" +
            "ebe76671ff1dea7b1ee5d0434c976d2d23bb481b6d80d242a2c942329dbe051396d0a9add0806863" +
            "4f6c7f8aadfd55e4109cde60f693229f605d71f896f5e1c9d3b94fb9a497a9b955960307811296f2" +
            "ed263ad57780c6f2f96b42eb71e817bbed0654000c6bc20d3087f7971b8d517c00cdf9732294285c" +
            "6faa24b405e3b31e6fb856b57aabae81e72a8876f06cc0fbda5ca4479a5cecdee7b5bd0fff8f8c78" +
            "8e2d803dad28ca110f6013672323540b94eb5a7638116cfec790f2d899d7f6bc075cbb78ad925845" +
            "bc75b8086078356b0c6dc722283e774cb7a5ee24a6b976ca6dcc40fef3ea10e77cc50a0523ab4df3" +
            "2971a19c9c6e889edb99ea9d863e7f9922d02303b80a29899397f4abacc5ecf6da98574e2ee2afcd" +
            "18077063d7419e4ee361891085217c905f6cefb2041ee6f49df051511152f45609a0d06d951b0351" +
            "431651bde5b5434c06b146509727cd5b76f182c1353ee94e6ef98901cdba4b6cfc1dda01628ff86b" +
            "21e2be53da4a8c2c9fc1b50b28ada2836959ed1398c70018b5f3c35d9f3c8768af0966a0e8ee6b16" +
            "dd17455acecd377fd259379e7e22f187876db740c3c09a0307891484f9b12da66916d9d4018ac34b" +
            "9d70a094a655ece0282839a9e60b8cea041316803b262a4928d375889017f3da58b9721ac9d7a4e6" +
            "9b06fb26d46a904b062728286ee2e44c18354be39252482e5135fbfd1ea4f85dfc96b63e0fc8815a" +
            "3a0f1be7476e60712a566911663159e74838f27a0068b2131aec8653b5f697ede4dd8c769234ba5d" +
            "018ede320aeace49e263844b93d3c2410af9c5df83c0e7eb617930eea8a272e16a6d58e6ce1ce9a3" +
            "b42ad94436abe73e01b95839038d5430676ef6a7a3c77cd541fe860d40bd9414133a8983280e1391" +
            "61d85dc0c395eac1ffc6fe52d637fd5f327d112f8ed15172925b0a21334d8b5dbeaea1bdbfcf9bcb" +
            "8c9bd72b58aa6745f07fae50343191f90983e4d138a278f46433a8c404565c4d15a55c4d61f39644" +
            "4b639bf7191515c558155bf2a09576e7b3376236a23baeb2f7826e1b5e95e100bae7d0b922686483" +
            "9d04f2145cf0a0c0dbb0194f2224aa63cb144a0038cd63ac6b42bd9c74e7d1eff9cc1419043a8bbe" +
            "c602e5665d45ddfa09c1831c0c04fa116ff8ad7fd93a0d005dedb329407c84b809d4552c6e31174c" +
            "a01f7fe336dd1759b3d4bafdcf5df63f5bca512caf29a4e645e5315c0777478f1640afa2d30f2e8f" +
            "5293571d3b94f65be0cc98f24261633ae9b0a44e6048c35ce44eca75d6362ce6b5698806addecf26" +
            "d1928a2ac14939923ca63d54afa103e7a1c8f23fa9880e381d17ee89b4e65922ebe81d58b3dfe637" +
            "f554f0dbb499073cb4ca9c2ee30a6bd0c5fc56675e0215a8b4577dd6c1490087bf4b2039b5b52ad4" +
            "9c08212a8171d0cb6fb4f84855cf426d36147f5e46fd3f7722481ce26b0511d6603087fbb9d1382a" +
            "69011de7aedadd0d29b7c0ffeae5640c8079acf8818de222664728df79967e54962537fa06bde323" +
            "de630bf63ae92e57e33f242fd508e767da3f2dfa14c0726b12bce09c4310502807ef00b88afbe76f" +
            "f01a941ce512504c25d552e8863afa3c89bdb53d757d09161a5e65b4c1d6f4fe00879d1e247d8a93" +
            "f30b252d5d6d7308676a76c4d9ee11131b2c2f7f8092b0d2e417225f6d7c82a4eff0f7204765a4bc" +
            "d2dafd00000000000000000000000000000a0d1319202b353d";

        private const string MLDsa87PkHex =
            "e45ffc8cc73db885dc662e62a18cd8e3803297117fa5658814a985b5ff1db7b468cfc82bb929f1d8" +
            "6b77ed14f5ae16a65368772ce51912410105e0456975ae91fdb643b512f124d5e60bd68b8c7e31fe" +
            "01c7b0dc65ae470501cc565a6e1dfcfcfd12565433c4afedd511821e2e9610c45275e2836dee35ce" +
            "d69d7efa672fd1e4318bef5eb6e897e8b451aa202ded042b2aaef77a7be3f699146da229a8bdb3ff" +
            "a496445967e75217bfbc9048f9956443d8731f833eb30de10dac96fffe7cf65ea0445c3e31e8601e" +
            "133be6a100764fe3196e267726441f31751fbf9a6f5880644f4e7275e57de2b0f105e4db055d50dd" +
            "1c9c934fddf535b8de28b0c74c0449f222cd2ed0bb8fbc775ccee8c940665b40f712f4f7e00750e9" +
            "e1e4cd9cff25d1945c3e9bca53ccd4f12eee7581856ebd68f26845956e3e7beb761f0fe75bdd31bf" +
            "e2fa018113397b387bd59d62a68b8af7fa245ab932e69f778e2ceefd21304fbb8099ea13d8ea57c1" +
            "813197a2f75ae251075b51dad38f853669e9d5f98a3655098941993a1594860fba71fe530ee5c29f" +
            "58f2978af688ccb75a5838a359c112e98e25a8583ac8dac1f861fd58e2afba5de5a52e020904f5b4" +
            "2bc0874e35befcf3e6119684768f36e008f04712177cebe627607381e56eaaee161c1729b8de51db" +
            "de474d48cc68249ea27162b87993e60c84ed6cc6423cb3676d9eb50b2cab5a3a049ef131381d623f" +
            "a6fbcbc9db1e7cc025ea0418b9dad2cc6ccd4e95fa2cec24feeca70318a751716b7213f63edbf65a" +
            "63338357f838f94ec071822c24851248885107b3d1c4e924678c7614ea1af038104619f2ae372940" +
            "becfa69e29cbb5ff6c3e20a47be4a4f74bac34c133c00a6a706accc6ffd3d8e4fbd69a99704e1283" +
            "c850d8c58d1e5753cd9587b83c4c346cb9a58137213ec10834c66adfe2bb5c501a8ef2ecadd1b677" +
            "a3df1a6deb86ebf0722c4f5030e20f9018dd5b6fc53eea24fd92b7b5b4025feae996d3e48fd4c650" +
            "d82dbad7eaf936639698512f26253d2ef6847c8518e8565cc9a5495c6fff57cde7323882c54a7db4" +
            "70ab2daf8ffd2bf794fa7c692d9e7fbd532eecc1d7880e2ca0b3216128be28b4a9f1d151fac97808" +
            "b0bd98b7b43a612a9ac865812bfeac6f47460277840b52a3b087f916ca7cedc0f768ea2bd19ea211" +
            "55f84b4a04c4000ad2ae0587154d560bc0a477a4f9329a8984dd31eb1f2a05e3d918701d630cfca9" +
            "af61ef088d2c5581acb463e439902e5d425719e956b8d6df7305b28e0ff27d3ad0de2085d292499b" +
            "19a3390d4396fb3bac9a8d8cbead2a7a4290fc9ac6fca045f98a614a45a39cbe24360f84d14f8e47" +
            "2712aceb74dbf45b53d49a0e4737e476ffc4d5b2f7cd247aa186d3b764ad9e9cfeee456a73c291d8" +
            "de3912414ac43911c372173ad7b472af35c6853ced2fe7b5fe0a89565ab33baa6f65cdd928319d70" +
            "65e040e7a5e84f9aa903f7648094bad07136b16927b8ec6dbc2bef0cc2856de1e795923e1412c49f" +
            "24deeb6c21f6c8a9765c9c7986e0da4b4c67d8e0d0c8d466824fb923d8573148990cd2ef133c78ce" +
            "ecab72ed9dd285c5a3766852d54534207ffd34027f6c76ede8fd1a32d72c30048bbaa797d5df6fde" +
            "27d087de5721ad7b7fa3e8d3f70d6bfc3ab2e252335368bbfa15acb5cb37d4694e8b23cebe25de9c" +
            "925a221a183b904d3f85df9929a919c54d6f87457373a0d6ecc1403e4cbbe620999435e80696634c" +
            "d1a8e4747e9825bfa336e5bbad14f73640f1b9febe800dbaefe1630c61fae635b074c564eaa9db18" +
            "9c9e7302873fc64e6d497bc5c29080987a07a21d4af210703a4fa07f2fd816f12fd1e29b4c0f44af" +
            "e9bd4a1eaa8a7ae6f02a5b4258f52caf6127f62632a67cf4e8310be56a7c28c86b2e277600c3e92c" +
            "8d23d42586244c571e90568df202f2f6d81f860a565f9eb91a3c78372e2a8b1be61c5418cf49bf2d" +
            "6c8955d4a482a9919b7660b3f9a4404ffc454ea073e1e4b2689ab2cca4e46bd7004a6c491fa26ee7" +
            "a57d60f35edb2b821e6266442c8f335d452d524c772e0353724c23c7dd15b7aa155e91442022140c" +
            "5fcb0153147edcf3e8952f6f0399a3c88066a72756c9409915de63f64fa797841c57c796c6fc550e" +
            "f745dfe9f179457f94755ae5a2506a764f327e550be3dc14dd41f3b04b147d454938c63a8d69b2ea" +
            "4c5710ec0b36e3a6c72571fa5d59dde036c42033df35af056966ff0cd1204008971aa6ba9fb97b68" +
            "5ab9ffa2a9d1778104cd2c3b326de1fcbc242e94d0311c3275b12850ed30ceead3a2ee6d06050841" +
            "1d4396f5421d8b6d067cf7cb5e826785fbe119e05e21bd879b64f57cb0cd1972c2815f20abe7ce6a" +
            "b34d0f471af44baad179e90644122f5f33288e689ddddc5ce833e9755df1e73c65c5a201c4ede2ff" +
            "a6b19274927719d2d38fdb7a65aa43708b7fa9a94aa7d3210253d78d3b181e1020d0000bd0a1dc05" +
            "d447f9f58ebeb84c65b36c8afcb83727a1508994e826957a663b0b9b8a003325ab6d6d6462ee4e10" +
            "6019c0dffe10323b7bde7d82a38f85fd08786e860ba66c161b64b0708c363de5c6af62d8db3c243d" +
            "1e1b712cb1d59e942b9b6b4295a5a500b182cbd5fd1bc6ce9376d91b47a2284f1fbe0ad1c048cc2c" +
            "fbb4afa3a9eb9697503b69feca990eba7e9441af9ca44cb3ac6b5ed66e591c201fe30efa8a7c471d" +
            "c613d6254c263a8e132104bec47f1aacb3b2fcd4051b69b5e3fcb1c147a65c2f90c4b5188bafc521" +
            "cab03c12a309da50b5a7517727ed41228ed123fe1b152f6a6319cd623bf34ad7b8e064ab993260bc" +
            "bd405f5b7fff9b2fa40ba5ed5630242539e5d96823e89dc818a13d16675ee3079d976f694f5acc97" +
            "60ae789e9b3391b289e0e22a7ef17cc6a4577157b6d95c09baa4fd532e3ee0a290810ed35e56bb19" +
            "d9b61fb98a97c617425b06093d98a5cf0ee2dd127f0eea600b9a0c67fbe761db9b77e5d5bba9701d" +
            "a1b883e521a0cfe88451f57bd36085b67e56f061f84a2e6a152a71bce6e522daab6a0a33ce22e537" +
            "fa9793d28b617e6c0a4176a83aa3be578afac0f2f5547c5516d218984755b7445c7143afa4e551fc" +
            "e0071bdb873b34e6b9e2b9e79ed0c69d288ed6421f237e860a0c6492ebbdd2a44c2c4f368dbe9994" +
            "1b1e8561d859d3859f496cee3d741f252973f8fcc539c409e35cc80a5ed6df23cc3a65601313f5d6" +
            "81fd9540c5291a9e30a72e38c96413c47c61ff84fde78d011b01b4154d1b920af003f7abb1e1999d" +
            "ea6a766cf9fd2702b3ce0ee57af931b62124b0861b163a3b91aa4bea28076c3432df3b29b6c4e1ba" +
            "588def420071fc157de90eb2722ecc9ab00df3c669383a61a91bb67bd287ce349b4745ee7a479dbc" +
            "eef166b9acc412eb579fcd6437307edda253d606b7be7599c38092bc52a8598480edab8b82b1d21c" +
            "565d2137ceae0b6642619b16133d91205d6355029e9cdfeb9a28b373d95916b6b707d4c712c09cf3" +
            "6daf1a511b2bedb1aa70ee58d46a0666bb287784b0a3840c589a7a04d5d6f2216be90aa4a512d563" +
            "2f5c9bfe7b8b13382f999b95d367c7c46b968074ce315197a5ff3545c7b77a804ade56a95b5c24cd" +
            "ece5937b5c0366d93ad03da9bc5db1b551dfb91e9b343d2b57b763439686d4a3";
    }
}
