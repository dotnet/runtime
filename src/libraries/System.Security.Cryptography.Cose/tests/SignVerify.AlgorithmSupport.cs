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
    public class SignVerify
    {
        [ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        public static void DecodeSign1DraftExampleMLDsa44()
        {
            using MLDsa pubKey = MLDsa.ImportMLDsaPublicKey(MLDsaAlgorithm.MLDsa44, MLDsa44PkHex.HexToByteArray());

            CoseSign1Message msg = CoseMessage.DecodeSign1(MLDsa44Sign1Hex.HexToByteArray());
            Assert.NotNull(msg.Content);
            Assert.True(msg.VerifyEmbedded(CreateMLDsaAsymmetricAlgorithm(pubKey)));

            Assert.Equal(2, msg.ProtectedHeaders.Count);
            Assert.Equal(0, msg.UnprotectedHeaders.Count);

            if (msg.ProtectedHeaders.TryGetValue(CoseHeaderLabel.Algorithm, out var alg))
            {
                Assert.Equal(-48 /* MLDsa44 */, alg.GetValueAsInt32());
            }

            if (msg.ProtectedHeaders.TryGetValue(CoseHeaderLabel.KeyIdentifier, out var keyIdentifier))
            {
                Assert.Equal(32 /* can't correlate that with anything on the draft example, it doesn't seem to match kid */, keyIdentifier.GetValueAsBytes().Length);
            }
        }

        [Theory]
        [MemberData(nameof(AllKeysAndSign1Implementations))]
        public static void TestSignVerifySingleSignerAllAlgorithms(CoseTestKeyManager keyManager, string keyId, CoseTestSign1 signerImplementation)
        {
            byte[] payload = Encoding.UTF8.GetBytes("Hello World");
            byte[] signature = signerImplementation.Sign(keyManager, keyId, payload);
            Assert.NotNull(signature);
            Assert.True(signature.Length > 0);
            Assert.True(signerImplementation.Verify(keyManager, keyId, payload, signature));

            // we try different key
            CoseTestKey differentKey = keyManager.GetDifferentKey(keyId);
            Assert.False(signerImplementation.Verify(keyManager, differentKey.Id, payload, signature));

            // we try fake signature
            byte[] fakeSignature = signerImplementation.Sign(keyManager, differentKey.Id, payload);
            Assert.False(signerImplementation.Verify(keyManager, keyId, payload, fakeSignature));

            // we try fake payload
            if (!signerImplementation.IsEmbedded)
            {
                // embedded ignore payload arg
                byte[] fakePayload = Encoding.UTF8.GetBytes("Hello World 2");
                Assert.False(signerImplementation.Verify(keyManager, keyId, fakePayload, signature));
            }
        }

        // [Theory]
        // [MemberData(nameof(AllKeysAndMultiSignImplementations))]
        // public static void TestSignVerifyMultiSignerAllAlgorithms(CoseTestKeyManager keyManager, string[] keyIds, CoseTestMultiSign signerImplementation)
        // {
        //     byte[] payload = Encoding.UTF8.GetBytes("Hello World");
        //     byte[] signature = signerImplementation.Sign(keyManager, keyIds, payload);
        //     Assert.NotNull(signature);
        //     Assert.True(signature.Length > 0);
        //     Assert.True(signerImplementation.Verify(keyManager, keyIds, payload, signature));

        //     // TODO:
        //     //// we try different key
        //     //CoseTestKey differentKey = keyManager.GetDifferentKey(keyIds[0]);
        //     //Assert.False(signerImplementation.Verify(keyManager, new[] { differentKey.Id }, payload, signature));
        //     //// we try fake signature
        //     //byte[] fakeSignature = signerImplementation.Sign(keyManager, new[] { differentKey.Id }, payload);
        //     //Assert.False(signerImplementation.Verify(keyManager, keyIds, payload, fakeSignature));
        //     //// we try fake payload
        //     //byte[] fakePayload = Encoding.UTF8.GetBytes("Hello World 2");
        //     //Assert.False(signerImplementation.Verify(keyManager, keyIds, fakePayload, signature));
        // }

        public static IEnumerable<object[]> AllKeysAndSign1Implementations()
        {
            CoseTestKeyManager keyManager = CoseTestKeyManager.TestKeys;

            foreach (CoseTestKey key in keyManager.AllKeys())
            {
                foreach (CoseTestSign1 sign1 in CoseTestSign1.GetImplementations())
                {
                    yield return [keyManager, key.Id, sign1];
                }
            }
        }

        public static IEnumerable<object[]> AllKeysAndMultiSignImplementations()
        {
            CoseTestKeyManager keyManager = CoseTestKeyManager.TestKeys;

            string[] keyIds = keyManager.AllKeys().Select(key => key.Id).ToArray();
            int[] nrOfKeys = [1, 2, 3, 3, 5];

            for (int i = 0; i < keyIds.Length; i++)
            {
                string[] keysToTest = PickNKeys(nrOfKeys[i % nrOfKeys.Length], i, keyIds);
                foreach (CoseTestMultiSign multiSign in CoseTestMultiSign.GetImplementations())
                {
                    yield return [ keyManager, keysToTest, multiSign ];
                }
            }

            static string[] PickNKeys(int n, int atIndex, string[] keys)
            {
                string[] ret = new string[n];
                for (int i = 0; i < n; i++)
                {
                    ret[i] = keys[(atIndex + i) % keys.Length];
                }

                return ret;
            }
        }

        //[ConditionalFact(typeof(MLDsa), nameof(MLDsa.IsSupported))]
        //public static void SignVerify()
        //{
        //    using MLDsa key = MLDsa.GenerateKey(MLDsaAlgorithm.MLDsa44);
        //    CoseSigner signer = new CoseSigner(key);
        //}

        // We need either new MLDsa API or make MLDsa implement AsymmetricAlgorithm
        // new MLDsa API stands out - no other algorithm needs it - one exception is RSA because it needs to provide padding and only optional if you don't like default
        private static AsymmetricAlgorithm CreateMLDsaAsymmetricAlgorithm(MLDsa key) => new CoseSigner(key).Key;

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
        private const string MLDsa44PkHex = "ba71f9f64e11baeb58fa9c6fbb6e14e61f18643dab495b47539a9166ca0198131c44f826bbd56e34" +
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
    }
}
