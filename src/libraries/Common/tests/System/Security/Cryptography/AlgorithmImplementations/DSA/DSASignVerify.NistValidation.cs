// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    public partial class DSASignVerify
    {
        [Fact]
        public static void Fips186_2_1()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-2dsatestvectors.zip
            // SigGen.txt, first case
            const string p =
                "f5422387a66acb198173f466e987ca692fd2337af0ed1ec7aa5f2e2088d0742c" +
                "2d41ded76317001ca4044115f00aff09ad59d49b07c35ec2b25088be17ac391a" +
                "f17575d52c232153df94f0023a0a17ca29d8548dfa08c5f034bad0bd4511ffae" +
                "6b3c504c6f728d31d1e92aad9e88382a8a42b050441a747bb71dd84cb01d9ee7";

            const string q = "f4a9d1750b46e27c3af7587c5d019ffc99f11f25";

            const string g =
                "7400ad91528a6c9e891f3f5fce7496ef4d01bf91a979736547049406ab4a2d2f" +
                "e49fa3730cfb86a5af3ff21f5022f07e4ee0c15a88b8bd7b5f0bf8dea3863afb" +
                "4f1cac16aba490d93f44be79c1cd01ce2e12dfdb75c593d64e5bf97e839526db" +
                "cc0288cd3beb2fd7941f67d138faa88f9de90901efdc752569a4d1afbd193846";

            const string x = "485e8ad4a4e49a85e0397af0bb115df175ead894";

            const string y =
                "ec86482ea1c463198d074bad01790283fb8866e53ab5e821219f0f4a25e7d047" +
                "3f9cbd2ab7348625d322ea7f09ec9a15bbcc5a9ff1f3692392768970e9e86554" +
                "5d3aa2934148f6d0a6ec410a16d5059c58ce428912f532cbc8f9bbbcf3657367" +
                "d159212c11afd856587b1b092ab1bdae3c443661e6ba27078d03eb31e63e5922";

            const string msg =
                "96452f7f94b9cc004931df8f8118be7e56f16a1502e00934f16c96391b83d724" +
                "90be8ffa54e7f6676eb966a63ce657a6095f8d65e1cf90a0a4685daf5ae35bab" +
                "c6c290d13ed9152bba0cc76d2a5a401d0d1b06f63f85018f12753338a16da324" +
                "61d89acef996129554b46ca9f47b612b89ad3b90c20b4547631a809b982797da";

            const string r = "ed4715b8d218d31b7adf0bea5165777a7414315e";
            const string s = "29c70a036aa83eb0742f1fa3f56ccead0fc0f61d";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA1);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L1024_N160_SHA256_1()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=1024, N=160, SHA-256], first case
            const string p =
                "cba13e533637c37c0e80d9fcd052c1e41a88ac325c4ebe13b7170088d54eef48" +
                "81f3d35eae47c210385a8485d2423a64da3ffda63a26f92cf5a304f39260384a" +
                "9b7759d8ac1adc81d3f8bfc5e6cb10efb4e0f75867f4e848d1a338586dd0648f" +
                "eeb163647ffe7176174370540ee8a8f588da8cc143d939f70b114a7f981b8483";

            const string q = "95031b8aa71f29d525b773ef8b7c6701ad8a5d99";

            const string g =
                "45bcaa443d4cd1602d27aaf84126edc73bd773de6ece15e97e7fef46f13072b7" +
                "adcaf7b0053cf4706944df8c4568f26c997ee7753000fbe477a37766a4e970ff" +
                "40008eb900b9de4b5f9ae06e06db6106e78711f3a67feca74dd5bddcdf675ae4" +
                "014ee9489a42917fbee3bb9f2a24df67512c1c35c97bfbf2308eaacd28368c5c";

            const string x = "2eac4f4196fedb3e651b3b00040184cfd6da2ab4";

            const string y =
                "4cd6178637d0f0de1488515c3b12e203a3c0ca652f2fe30d088dc7278a87affa" +
                "634a727a721932d671994a958a0f89223c286c3a9b10a96560542e2626b72e0c" +
                "d28e5133fb57dc238b7fab2de2a49863ecf998751861ae668bf7cad136e6933f" +
                "57dfdba544e3147ce0e7370fa6e8ff1de690c51b4aeedf0485183889205591e8";

            const string msg =
                "812172f09cbae62517804885754125fc6066e9a902f9db2041eeddd7e8da67e4" +
                "a2e65d0029c45ecacea6002f9540eb1004c883a8f900fd84a98b5c449ac49c56" +
                "f3a91d8bed3f08f427935fbe437ce46f75cd666a0707265c61a096698dc2f36b" +
                "28c65ec7b6e475c8b67ddfb444b2ee6a984e9d6d15233e25e44bd8d7924d129d";

            const string r = "76683a085d6742eadf95a61af75f881276cfd26a";
            const string s = "3b9da7f9926eaaad0bebd4845c67fcdb64d12453";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA256);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L1024_N160_SHA384_1()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=1024, N=160, SHA-384], first case
            const string p =
                "f24a4afc72c7e373a3c30962332fe5405c45930963909418c30792aaf135ddea" +
                "561e94f24726716b75a18828982e4ce44c1fddcb746487b6b77a9a5a17f868ab" +
                "50cd621b5bc9da470880b287d7398190a42a5ee22ed8d1ff147e2019810c8298" +
                "ed68e1ca69d41d555f249e649fb1725ddb075c17b37beff467fdd1609243373f";

            const string q = "da065a078ddb56ee5d2ad06cafab20820d2c4755";

            const string g =
                "47b5591b79043e4e03ca78a0e277c9a21e2a6b543bf4f044104cd9ac93eff8e1" +
                "01bb6031efc8c596d5d2f92e3a3d0f1f74702dd54f77d3cd46c04dee7a5de9f0" +
                "0ad317691fddcefe4a220a2651acae7fcedda92bfcca855db6705e8d864f8192" +
                "bf6bf860c00f08ad6493ecc1872e0028d5c86d44505db57422515c3825a6f78a";

            const string x = "649820168eb594f59cd9b28b9aefe8cc106a6c4f";

            const string y =
                "43a27b740f422cb2dc3eaa232315883a2f6a22927f997d024f5a638b507b17d3" +
                "b1cbd3ec691cc674470960a0146efdecb95bb5fe249749e3c806cd5cc3e7f7ba" +
                "b845dadbe1f50b3366fb827a942ce6246dda7bd2c13e1b4a926c0c82c8846395" +
                "52d9d46036f9a4bc2a9e51c2d76e3074d1f53a63224c4279e0fa460474d4ffde";

            const string msg =
                "b0dbbf4a421ba5c5b0e52f09629801c113258c252f29898c3354706e39ec5824" +
                "be523d0e2f8cfe022cd61165301274d5d621a59755f50404d8b802371ce616de" +
                "fa962e3636ae934ec34e4bcf77a16c7eff8cf4cc08a0f4849d6ad4307e9f8df8" +
                "3f24ad16ab46d1a61d2d7d4e21681eb2ae281a1a5f9bca8573a3f5281d308a5a";

            const string r = "77c4d99f62b3ad7dd1fe6498db45a5da73ce7bde";
            const string s = "23871a002ae503fdabaa6a84dcc8f38769737f01";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA384);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L1024_N160_SHA384_4()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=1024, N=160, SHA-384], fourth case (s=00....)
            const string p =
                "f24a4afc72c7e373a3c30962332fe5405c45930963909418c30792aaf135ddea" +
                "561e94f24726716b75a18828982e4ce44c1fddcb746487b6b77a9a5a17f868ab" +
                "50cd621b5bc9da470880b287d7398190a42a5ee22ed8d1ff147e2019810c8298" +
                "ed68e1ca69d41d555f249e649fb1725ddb075c17b37beff467fdd1609243373f";

            const string q = "da065a078ddb56ee5d2ad06cafab20820d2c4755";

            const string g =
                "47b5591b79043e4e03ca78a0e277c9a21e2a6b543bf4f044104cd9ac93eff8e1" +
                "01bb6031efc8c596d5d2f92e3a3d0f1f74702dd54f77d3cd46c04dee7a5de9f0" +
                "0ad317691fddcefe4a220a2651acae7fcedda92bfcca855db6705e8d864f8192" +
                "bf6bf860c00f08ad6493ecc1872e0028d5c86d44505db57422515c3825a6f78a";

            const string x = "bb318987a043158b97fdbbc2707471a38316ce58";

            const string y =
                "c9003995b014afad66de25fc0a2210b1f1b22d275da51a27faacda042fd76456" +
                "86ec8b1b62d58d8af2e1063ab8e146d11e3a07710bc4521228f35f5173443bbf" +
                "d089f642cd16641c57199c9ab6e0d9b0c01931c2d162f5e20dbe7365c93adc62" +
                "fd5a461bea5956d7c11ac67647bedcead5bb311224a496aa155992aee74e45ad";

            const string msg =
                "36a25659a7f1de66b4721b48855cdebe98fe6113241b7beddc2691493ed0add0" +
                "b6a9fbbf9fb870a1bc68a901b932f47ded532f93493b1c081408165807b38efc" +
                "e7acc7dbc216bef74ed59e20973326553cc83779f742e3f469a7278eeb1537dd" +
                "71cd8f15114d84693c2e6bbf62814a08e82ba71539f4cb4bf08c869d7db9dea9";

            const string r = "17cc53b5b9558cc41df946055b8d7e1971be86d7";
            const string s = "003c21503971c03b5ef4edc804d2f7d33f9ea9cc";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA384);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L1024_N160_SHA512_1()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=1024, N=160, SHA-512], first case
            const string p =
                "88d968e9602ecbda6d86f7c970a3ffbeb1da962f28c0afb9270ef05bc330ca98" +
                "c3adf83c072feb05fb2e293b5065bbb0cbcc930c24d8d07869deaecd92a2604c" +
                "0f5dd35c5b431fda6a222c52c3562bf7571c710209be8b3b858818788725fe81" +
                "12b7d6bc82e0ff1cbbf5d6fe94690af2b510e41ad8207dc2c02fb9fa5cefaab5";

            const string q = "a665689b9e5b9ce82fd1676006cf4cf67ecc56b7";

            const string g =
                "267e282857417752113fba3fca7155b5ce89e7c8a33c1a29122e2b720965fc04" +
                "245267ff87fc67a5730fe5b308013aa3266990fbb398185a87e055b443a868ce" +
                "0ce13ae6aee330b9d25d3bbb362665c5881daf0c5aa75e9d4a82e8f04c91a9ad" +
                "294822e33978ab0c13fadc45831f9d37da4efa0fc2c5eb01371fa85b7ddb1f82";

            const string x = "07ce8862e64b7f6c7482046dbfc93907123e5214";

            const string y =
                "60f5341e48ca7a3bc5decee61211dd2727cd8e2fc7635f3aabea262366e458f5" +
                "c51c311afda916cb0dcdc5d5a5729f573a532b594743199bcfa7454903e74b33" +
                "ddfe65896306cec20ebd8427682fa501ee06bc4c5d1425cbe31828ba008b19c9" +
                "da68136cf71840b205919e783a628a5a57cf91cf569b2854ffef7a096eda96c9";

            const string msg =
                "3a84a5314e90fd33bb7cd6ca68720c69058da1da1b359046ae8922cac8afc5e0" +
                "25771635fb4735491521a728441b5cb087d60776ee0ecc2174a41985a82cf46d" +
                "8f8d8b274a0cc439b00971077c745f8cf701cf56bf9914cc57209b555dc87ca8" +
                "c13da063270c60fc2c988e692b75a7f2a669903b93d2e14e8efb6fb9f8694a78";

            const string r = "a53f1f8f20b8d3d4720f14a8bab5226b079d9953";
            const string s = "11f53f6a4e56b51f60e20d4957ae89e162aea616";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA512);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L1024_N160_SHA512_4()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=1024, N=160, SHA-512], fourth case (r=000....)
            const string p =
                "88d968e9602ecbda6d86f7c970a3ffbeb1da962f28c0afb9270ef05bc330ca98" +
                "c3adf83c072feb05fb2e293b5065bbb0cbcc930c24d8d07869deaecd92a2604c" +
                "0f5dd35c5b431fda6a222c52c3562bf7571c710209be8b3b858818788725fe81" +
                "12b7d6bc82e0ff1cbbf5d6fe94690af2b510e41ad8207dc2c02fb9fa5cefaab5";

            const string q = "a665689b9e5b9ce82fd1676006cf4cf67ecc56b7";

            const string g =
                "267e282857417752113fba3fca7155b5ce89e7c8a33c1a29122e2b720965fc04" +
                "245267ff87fc67a5730fe5b308013aa3266990fbb398185a87e055b443a868ce" +
                "0ce13ae6aee330b9d25d3bbb362665c5881daf0c5aa75e9d4a82e8f04c91a9ad" +
                "294822e33978ab0c13fadc45831f9d37da4efa0fc2c5eb01371fa85b7ddb1f82";

            const string msg =
                "16250c74ccb40443625a37c4b7e2b3615255768241f254a506fa819efbb8698a" +
                "de38fc75946b3af09055578f28a181827dda311bd4038fd47f6d86cceb1bbbef" +
                "2df20bf595a0ad77afd39c84877434ade3812f05ec541e0403abadc778d116fd" +
                "077c95c6ec0f47241f4db813f31986b7504c1cd9ddb496ac6ed22b45e7df72cc";

            const string x = "3fee04cc08624f3a7f34c538d87692209dd74797";

            const string y =
                "6e8c85150c5c9ca6dcb04806671db1b672fc1087c995311d7087ad12ab18f2c1" +
                "4b612cea13bf79518d2b570b8b696b3e4efcd0fda522a253bbcb7dbb711d984c" +
                "598fa201c21a8a9e2774bc15020920cd8c27c2875c779b08ef95093caac2c9ce" +
                "a37ec498c23dd24b684abcb467ec952a202cbd2df7960c1ef929cc2b611ca6c8";

            const string r = "00018f0fdc16d914971c8f310f1af7796c6f662a";
            const string s = "62b7aecc75cbc6db00dd0c24339f7bdb5ae966a5";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA512);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L2048_N256_SHA256_1()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=2048, N=256, SHA-256], first case
            const string p =
                "a8adb6c0b4cf9588012e5deff1a871d383e0e2a85b5e8e03d814fe13a059705e" +
                "663230a377bf7323a8fa117100200bfd5adf857393b0bbd67906c081e585410e" +
                "38480ead51684dac3a38f7b64c9eb109f19739a4517cd7d5d6291e8af20a3fbf" +
                "17336c7bf80ee718ee087e322ee41047dabefbcc34d10b66b644ddb3160a28c0" +
                "639563d71993a26543eadb7718f317bf5d9577a6156561b082a10029cd44012b" +
                "18de6844509fe058ba87980792285f2750969fe89c2cd6498db3545638d5379d" +
                "125dccf64e06c1af33a6190841d223da1513333a7c9d78462abaab31b9f96d5f" +
                "34445ceb6309f2f6d2c8dde06441e87980d303ef9a1ff007e8be2f0be06cc15f";

            const string q =
                "e71f8567447f42e75f5ef85ca20fe557ab0343d37ed09edc3f6e68604d6b9dfb";

            const string g =
                "5ba24de9607b8998e66ce6c4f812a314c6935842f7ab54cd82b19fa104abfb5d" +
                "84579a623b2574b37d22ccae9b3e415e48f5c0f9bcbdff8071d63b9bb956e547" +
                "af3a8df99e5d3061979652ff96b765cb3ee493643544c75dbe5bb39834531952" +
                "a0fb4b0378b3fcbb4c8b5800a5330392a2a04e700bb6ed7e0b85795ea38b1b96" +
                "2741b3f33b9dde2f4ec1354f09e2eb78e95f037a5804b6171659f88715ce1a9b" +
                "0cc90c27f35ef2f10ff0c7c7a2bb0154d9b8ebe76a3d764aa879af372f4240de" +
                "8347937e5a90cec9f41ff2f26b8da9a94a225d1a913717d73f10397d2183f1ba" +
                "3b7b45a68f1ff1893caf69a827802f7b6a48d51da6fbefb64fd9a6c5b75c4561";

            const string x =
                "446969025446247f84fdea74d02d7dd13672b2deb7c085be11111441955a377b";

            const string y =
                "5a55dceddd1134ee5f11ed85deb4d634a3643f5f36dc3a70689256469a0b651a" +
                "d22880f14ab85719434f9c0e407e60ea420e2a0cd29422c4899c416359dbb1e5" +
                "92456f2b3cce233259c117542fd05f31ea25b015d9121c890b90e0bad033be13" +
                "68d229985aac7226d1c8c2eab325ef3b2cd59d3b9f7de7dbc94af1a9339eb430" +
                "ca36c26c46ecfa6c5481711496f624e188ad7540ef5df26f8efacb820bd17a1f" +
                "618acb50c9bc197d4cb7ccac45d824a3bf795c234b556b06aeb9291734532520" +
                "84003f69fe98045fe74002ba658f93475622f76791d9b2623d1b5fff2cc16844" +
                "746efd2d30a6a8134bfc4c8cc80a46107901fb973c28fc553130f3286c1489da";

            const string msg =
                "4e3a28bcf90d1d2e75f075d9fbe55b36c5529b17bc3a9ccaba6935c9e2054825" +
                "5b3dfae0f91db030c12f2c344b3a29c4151c5b209f5e319fdf1c23b190f64f1f" +
                "e5b330cb7c8fa952f9d90f13aff1cb11d63181da9efc6f7e15bfed4862d1a62c" +
                "7dcf3ba8bf1ff304b102b1ec3f1497dddf09712cf323f5610a9d10c3d9132659";

            const string r =
                "633055e055f237c38999d81c397848c38cce80a55b649d9e7905c298e2a51447";

            const string s =
                "2bbf68317660ec1e4b154915027b0bc00ee19cfc0bf75d01930504f2ce10a8b0";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA256);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L2048_N256_SHA384_1()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=2048, N=256, SHA-384], first case
            const string p =
                "a6167c16fff74e29342b8586aed3cd896f7b1635a2286ff16fdff41a06317ca6" +
                "b05ca2ba7c060ad6db1561621ccb0c40b86a03619bfff32e204cbd90b79dcb5f" +
                "86ebb493e3bd1988d8097fa23fa4d78fb3cddcb00c466423d8fa719873c37645" +
                "fe4eecc57171bbedfe56fa9474c96385b8ba378c79972d7aaae69a2ba64cde8e" +
                "5654f0f7b74550cd3447e7a472a33b4037db468dde31c348aa25e82b7fc41b83" +
                "7f7fc226a6103966ecd8f9d14c2d3149556d43829f137451b8d20f8520b0ce8e" +
                "3d705f74d0a57ea872c2bdee9714e0b63906cddfdc28b6777d19325000f8ed52" +
                "78ec5d912d102109319cba3b6469d4672909b4f0dbeec0bbb634b551ba0cf213";

            const string q =
                "8427529044d214c07574f7b359c2e01c23fd97701b328ac8c1385b81c5373895";

            const string g =
                "6fc232415c31200cf523af3483f8e26ace808d2f1c6a8b863ab042cc7f6b7144" +
                "b2d39472c3cb4c7681d0732843503d8f858cbe476e6740324aaa295950105978" +
                "c335069b919ff9a6ff4b410581b80712fe5d3e04ddb4dfd26d5e7fbca2b0c52d" +
                "8d404343d57b2f9b2a26daa7ece30ceab9e1789f9751aaa9387049965af32650" +
                "c6ca5b374a5ae70b3f98e053f51857d6bbb17a670e6eaaf89844d641e1e13d5a" +
                "1b24d053dc6b8fd101c624786951927e426310aba9498a0042b3dc7bbc59d705" +
                "f80d9b807de415f7e94c5cf9d789992d3bb8336d1d808cb86b56dde09d934bb5" +
                "27033922de14bf307376ab7d22fbcd616f9eda479ab214a17850bdd0802a871c";

            const string x =
                "459eb1588e9f7dd4f286677a7415cb25a1b46e7a7cfadc8a45100383e20da69d";

            const string y =
                "5ca7151bca0e457bbc46f59f71d81ab16688dc0eb7e4d17b166c3326c5b12c5b" +
                "debb3613224d1a754023c50b83cb5ecc139096cef28933b3b12ca31038e40893" +
                "83597c59cc27b902be5da62cae7da5f4af90e9410ed1604082e2e38e25eb0b78" +
                "dfac0aeb2ad3b19dc23539d2bcd755db1cc6c9805a7dd109e1c98667a5b9d52b" +
                "21c2772121b8d0d2b246e5fd3da80728e85bbf0d7067d1c6baa64394a29e7fcb" +
                "f80842bd4ab02b35d83f59805a104e0bd69d0079a065f59e3e6f21573a00da99" +
                "0b72ea537fa98caaa0a58800a7e7a0623e263d4fca65ebb8eded46efdfe7db92" +
                "c9ebd38062d8f12534f015b186186ee2361d62c24e4f22b3e95da0f9062ce04d";

            const string msg =
                "8c78cffdcf25d8230b835b30512684c9b252115870b603d1b4ba2eb5d35b33f2" +
                "6d96b684126ec34fff67dfe5c8c856acfe3a9ff45ae11d415f30449bcdc3bf9a" +
                "9fb5a7e48afeaba6d0b0fc9bce0197eb2bf7a840249d4e550c5a25dc1c71370e" +
                "67933edad2362fae6fad1efba5c08dc1931ca2841b44b78c0c63a1665ffac860";

            const string r =
                "4fd8f25c059030027381d4167c3174b6be0088c15f0a573d7ebd05960f5a1eb2";

            const string s =
                "5f56869cee7bf64fec5d5d6ea15bb1fa1169003a87eccc1621b90a1b892226f2";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA384);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L2048_N256_SHA384_3()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=2048, N=256, SHA-384], third case (r=00...)
            const string p =
                "a6167c16fff74e29342b8586aed3cd896f7b1635a2286ff16fdff41a06317ca6" +
                "b05ca2ba7c060ad6db1561621ccb0c40b86a03619bfff32e204cbd90b79dcb5f" +
                "86ebb493e3bd1988d8097fa23fa4d78fb3cddcb00c466423d8fa719873c37645" +
                "fe4eecc57171bbedfe56fa9474c96385b8ba378c79972d7aaae69a2ba64cde8e" +
                "5654f0f7b74550cd3447e7a472a33b4037db468dde31c348aa25e82b7fc41b83" +
                "7f7fc226a6103966ecd8f9d14c2d3149556d43829f137451b8d20f8520b0ce8e" +
                "3d705f74d0a57ea872c2bdee9714e0b63906cddfdc28b6777d19325000f8ed52" +
                "78ec5d912d102109319cba3b6469d4672909b4f0dbeec0bbb634b551ba0cf213";

            const string q =
                "8427529044d214c07574f7b359c2e01c23fd97701b328ac8c1385b81c5373895";

            const string g =
                "6fc232415c31200cf523af3483f8e26ace808d2f1c6a8b863ab042cc7f6b7144" +
                "b2d39472c3cb4c7681d0732843503d8f858cbe476e6740324aaa295950105978" +
                "c335069b919ff9a6ff4b410581b80712fe5d3e04ddb4dfd26d5e7fbca2b0c52d" +
                "8d404343d57b2f9b2a26daa7ece30ceab9e1789f9751aaa9387049965af32650" +
                "c6ca5b374a5ae70b3f98e053f51857d6bbb17a670e6eaaf89844d641e1e13d5a" +
                "1b24d053dc6b8fd101c624786951927e426310aba9498a0042b3dc7bbc59d705" +
                "f80d9b807de415f7e94c5cf9d789992d3bb8336d1d808cb86b56dde09d934bb5" +
                "27033922de14bf307376ab7d22fbcd616f9eda479ab214a17850bdd0802a871c";

            const string x =
                "6ba8f6638316dd804a24b7390f31023cd8b26e9325be90941b90d5fd3155115a";

            const string y =
                "10e6f50fd6dbb1ca16f2df5132a4a4eabc51da4a58fe619b2225d7adab0cea3a" +
                "fc2db90b158b6231c8b0774e0f0d9074517f336ca053ae115671aee3c1de0f85" +
                "728cff99deebc07ffc9a63631989a9277e64c54d9c25a7e739ae92f706ee237b" +
                "98b8700a9df0de12d2124e2cfd81d9ec7b0469ee3a718ab15305de099d9a2f8c" +
                "ecb79527d016447c8f6fe4905c3718ce5234d13bf4edd7169b9d0db9a6b0fc77" +
                "b7d53bdd32b07dc15bc829620db085114581608ac9e0937752095951d289855d" +
                "0bcc9d421b945cc4f37f80b0cb25f1ffee9c61e567f49d21f889ecbc3f4ed337" +
                "bca666ba3ba684874c883fe228ac44952a8513e12d9f0c4ed43c9b60f35225b2";

            const string msg =
                "4f1c0053984ab55a491f3618db1be2379174a4385974825fcbe584e2b6d0702a" +
                "bb8298dd9184eef1740b90a5eae850e9452b4e4ab219e187860f0fb4ad2be390" +
                "ef2ba7d76cdedcaf10aeaf4f25e497b4da951375b687a8d67012d3f99c7b5ca8" +
                "2e9bd0630dffcd635ecd8209cddb872da5bf4736309783345a35376b4fce4b91";

            const string r =
                "006b759fb718c34f1a6e518f834053b9f1825dd3eb8d719465c7bcc830322f4b";

            const string s =
                "47fa59852c9ae5e181381e3457a33b25420011d6f911efa90f3eaced1dee1329";
            
            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA384);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L3072_N256_SHA256_1()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=3072, N=256, SHA-256], first case
            const string p =
                "c7b86d7044218e367453d210e76433e4e27a983db1c560bb9755a8fb7d819912" +
                "c56cfe002ab1ff3f72165b943c0b28ed46039a07de507d7a29f738603decd127" +
                "0380a41f971f2592661a64ba2f351d9a69e51a888a05156b7fe1563c4b77ee93" +
                "a44949138438a2ab8bdcfc49b4e78d1cde766e54984760057d76cd740c94a4dd" +
                "25a46aa77b18e9d707d6738497d4eac364f4792d9766a16a0e234807e96b8c64" +
                "d404bbdb876e39b5799ef53fe6cb9bab62ef19fdcc2bdd905beda13b9ef7ac35" +
                "f1f557cb0dc458c019e2bc19a9f5dfc1e4eca9e6d466564124304a31f038605a" +
                "3e342da01be1c2b545610edd2c1397a3c8396588c6329efeb4e165af5b368a39" +
                "a88e4888e39f40bb3de4eb1416672f999fead37aef1ca9643ff32cdbc0fcebe6" +
                "28d7e46d281a989d43dd21432151af68be3f6d56acfbdb6c97d87fcb5e6291bf" +
                "8b4ee1275ae0eb4383cc753903c8d29f4adb6a547e405decdff288c5f6c7aa30" +
                "dcb12f84d392493a70933317c0f5e6552601fae18f17e6e5bb6bf396d32d8ab9";

            const string q =
                "876fa09e1dc62b236ce1c3155ba48b0ccfda29f3ac5a97f7ffa1bd87b68d2a4b";

            const string g =
                "110afebb12c7f862b6de03d47fdbc3326e0d4d31b12a8ca95b2dee2123bcc667" +
                "d4f72c1e7209767d2721f95fbd9a4d03236d54174fbfaff2c4ff7deae4738b20" +
                "d9f37bf0a1134c288b420af0b5792e47a92513c0413f346a4edbab2c45bdca13" +
                "f5341c2b55b8ba54932b9217b5a859e553f14bb8c120fbb9d99909dff5ea68e1" +
                "4b379964fd3f3861e5ba5cc970c4a180eef54428703961021e7bd68cb637927b" +
                "8cbee6805fa27285bfee4d1ef70e02c1a18a7cd78bef1dd9cdad45dde9cd6907" +
                "55050fc4662937ee1d6f4db12807ccc95bc435f11b71e7086048b1dab5913c60" +
                "55012de82e43a4e50cf93feff5dcab814abc224c5e0025bd868c3fc592041bba" +
                "04747c10af513fc36e4d91c63ee5253422cf4063398d77c52fcb011427cbfcfa" +
                "67b1b2c2d1aa4a3da72645cb1c767036054e2f31f88665a54461c885fb3219d5" +
                "ad8748a01158f6c7c0df5a8c908ba8c3e536822428886c7b500bbc15b49df746" +
                "b9de5a78fe3b4f6991d0110c3cbff458039dc36261cf46af4bc2515368f4abb7";

            const string x =
                "3470832055dade94e14cd8777171d18e5d06f66aeff4c61471e4eba74ee56164";

            const string y =
                "456a105c713566234838bc070b8a751a0b57767cb75e99114a1a46641e11da1f" +
                "a9f22914d808ad7148612c1ea55d25301781e9ae0c9ae36a69d87ba039ec7cd8" +
                "64c3ad094873e6e56709fd10d966853d611b1cff15d37fdee424506c184d62c7" +
                "033358be78c2250943b6f6d043d63b317de56e5ad8d1fd97dd355abe96452f8e" +
                "435485fb3b907b51900aa3f24418df50b4fcdafbf6137548c39373b8bc4ba3da" +
                "bb4746ebd17b87fcd6a2f197c107b18ec5b465e6e4cb430d9c0ce78da5988441" +
                "054a370792b730da9aba41a3169af26176f74e6f7c0c9c9b55b62bbe7ce38d46" +
                "95d48157e660c2acb63f482f55418150e5fee43ace84c540c3ba7662ae80835c" +
                "1a2d51890ea96ba206427c41ef8c38aa07d2a365e7e58380d8f4782e22ac2101" +
                "af732ee22758337b253637838e16f50f56d313d07981880d685557f7d79a6db8" +
                "23c61f1bb3dbc5d50421a4843a6f29690e78aa0f0cff304231818b81fc4a243f" +
                "c00f09a54c466d6a8c73d32a55e1abd5ec8b4e1afa32a79b01df85a81f3f5cfe";

            const string msg =
                "cb06e02234263c22b80e832d6dc5a1bee5ea8af3bc2da752441c04027f176158" +
                "bfe68372bd67f84d489c0d49b07d4025962976be60437be1a2d01d3be0992afa" +
                "5abe0980e26a9da4ae72f827b423665195cc4eed6fe85c335b32d9c03c945a86" +
                "e7fa99373f0a30c6eca938b3afb6dff67adb8bece6f8cfec4b6a12ea281e2323";

            const string r =
                "53bae6c6f336e2eb311c1e92d95fc449a929444ef81ec4279660b200d59433de";

            const string s =
                "49f3a74e953e77a7941af3aefeef4ed499be209976a0edb3fa5e7cb961b0c112";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA256);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L3072_N256_SHA384_1()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=3072, N=256, SHA-384], first case
            const string p =
                "a410d23ed9ad9964d3e401cb9317a25213f75712acbc5c12191abf3f1c0e723e" +
                "2333b49eb1f95b0f9748d952f04a5ae358859d384403ce364aa3f58dd9769909" +
                "b45048548c55872a6afbb3b15c54882f96c20df1b2df164f0bac849ca17ad2df" +
                "63abd75c881922e79a5009f00b7d631622e90e7fa4e980618575e1d6bd1a72d5" +
                "b6a50f4f6a68b793937c4af95fc11541759a1736577d9448b87792dff0723241" +
                "5512e933755e12250d466e9cc8df150727d747e51fea7964158326b1365d580c" +
                "b190f4518291598221fdf36c6305c8b8a8ed05663dd7b006e945f592abbecae4" +
                "60f77c71b6ec649d3fd5394202ed7bbbd040f7b8fd57cb06a99be254fa25d71a" +
                "3760734046c2a0db383e02397913ae67ce65870d9f6c6f67a9d00497be1d763b" +
                "21937cf9cbf9a24ef97bbcaa07916f8894e5b7fb03258821ac46140965b23c54" +
                "09ca49026efb2bf95bce025c4183a5f659bf6aaeef56d7933bb29697d7d54134" +
                "8c871fa01f869678b2e34506f6dc0a4c132b689a0ed27dc3c8d53702aa584877";

            const string q =
                "abc67417725cf28fc7640d5de43825f416ebfa80e191c42ee886303338f56045";

            const string g =
                "867d5fb72f5936d1a14ed3b60499662f3124686ef108c5b3da6663a0e86197ec" +
                "2cc4c9460193a74ff16028ac9441b0c7d27c2272d483ac7cd794d598416c4ff9" +
                "099a61679d417d478ce5dd974bf349a14575afe74a88b12dd5f6d1cbd3f91ddd" +
                "597ed68e79eba402613130c224b94ac28714a1f1c552475a5d29cfcdd8e08a6b" +
                "1d65661e28ef313514d1408f5abd3e06ebe3a7d814d1ede316bf495273ca1d57" +
                "4f42b482eea30db53466f454b51a175a0b89b3c05dda006e719a2e6371669080" +
                "d768cc038cdfb8098e9aad9b8d83d4b759f43ac9d22b353ed88a33723550150d" +
                "e0361b7a376f37b45d437f71cb711f2847de671ad1059516a1d45755224a15d3" +
                "7b4aeada3f58c69a136daef0636fe38e3752064afe598433e80089fda24b144a" +
                "462734bef8f77638845b00e59ce7fa4f1daf487a2cada11eaba72bb23e1df6b6" +
                "6a183edd226c440272dd9b06bec0e57f1a0822d2e00212064b6dba64562085f5" +
                "a75929afa5fe509e0b78e630aaf12f91e4980c9b0d6f7e059a2ea3e23479d930";

            const string x =
                "6d4c934391b7f6fb6e19e3141f8c0018ef5726118a11064358c7d35b37737377";

            const string y =
                "1f0a5c75e7985d6e70e4fbfda51a10b925f6accb600d7c6510db90ec367b93bb" +
                "069bd286e8f979b22ef0702f717a8755c18309c87dae3fe82cc3dc8f4b7aa3d5" +
                "f3876f4d4b3eb68bfe910c43076d6cd0d39fc88dde78f09480db55234e6c8ca5" +
                "9fe2700efec04feee6b4e8ee2413721858be7190dbe905f456edcab55b2dc291" +
                "6dc1e8731988d9ef8b619abcf8955aa960ef02b3f02a8dc649369222af50f133" +
                "8ed28d667f3f10cae2a3c28a3c1d08df639c81ada13c8fd198c6dae3d62a3fe9" +
                "f04c985c65f610c06cb8faea68edb80de6cf07a8e89c00218185a952b23572e3" +
                "4df07ce5b4261e5de427eb503ee1baf5992db6d438b47434c40c22657bc163e7" +
                "953fa33eff39dc2734607039aadd6ac27e4367131041f845ffa1a13f556bfba2" +
                "307a5c78f2ccf11298c762e08871968e48dc3d1569d09965cd09da43cf0309a1" +
                "6af1e20fee7da3dc21b364c4615cd5123fa5f9b23cfc4ffd9cfdcea670623840" +
                "b062d4648d2eba786ad3f7ae337a4284324ace236f9f7174fbf442b99043002f";

            const string msg =
                "ed9a64d3109ef8a9292956b946873ca4bd887ce624b81be81b82c69c67aaddf5" +
                "655f70fe4768114db2834c71787f858e5165da1a7fa961d855ad7e5bc4b7be31" +
                "b97dbe770798ef7966152b14b86ae35625a28aee5663b9ef3067cbdfbabd8719" +
                "7e5c842d3092eb88dca57c6c8ad4c00a19ddf2e1967b59bd06ccaef933bc28e7";

            const string r =
                "7695698a14755db4206e850b4f5f19c540b07d07e08aac591e20081646e6eedc";

            const string s =
                "3dae01154ecff7b19007a953f185f0663ef7f2537f0b15e04fb343c961f36de2";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA384);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L3072_N256_SHA512_1()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=3072, N=256, SHA-512], first case
            const string p =
                "c1d0a6d0b5ed615dee76ac5a60dd35ecb000a202063018b1ba0a06fe7a00f765" +
                "db1c59a680cecfe3ad41475badb5ad50b6147e2596b88d34656052aca79486ea" +
                "6f6ec90b23e363f3ab8cdc8b93b62a070e02688ea877843a4685c2ba6db111e9" +
                "addbd7ca4bce65bb10c9ceb69bf806e2ebd7e54edeb7f996a65c907b50efdf8e" +
                "575bae462a219c302fef2ae81d73cee75274625b5fc29c6d60c057ed9e7b0d46" +
                "ad2f57fe01f823230f31422722319ce0abf1f141f326c00fbc2be4cdb8944b6f" +
                "d050bd300bdb1c5f4da72537e553e01d51239c4d461860f1fb4fd8fa79f5d526" +
                "3ff62fed7008e2e0a2d36bf7b9062d0d75db226c3464b67ba24101b085f2c670" +
                "c0f87ae530d98ee60c5472f4aa15fb25041e19106354da06bc2b1d322d40ed97" +
                "b21fd1cdad3025c69da6ce9c7ddf3dcf1ea4d56577bfdec23071c1f05ee4077b" +
                "5391e9a404eaffe12d1ea62d06acd6bf19e91a158d2066b4cd20e4c4e52ffb1d" +
                "5204cd022bc7108f2c799fb468866ef1cb09bce09dfd49e4740ff8140497be61";

            const string q =
                "bf65441c987b7737385eadec158dd01614da6f15386248e59f3cddbefc8e9dd1";

            const string g =
                "c02ac85375fab80ba2a784b94e4d145b3be0f92090eba17bd12358cf3e03f437" +
                "9584f8742252f76b1ede3fc37281420e74a963e4c088796ff2bab8db6e9a4530" +
                "fc67d51f88b905ab43995aab46364cb40c1256f0466f3dbce36203ef228b35e9" +
                "0247e95e5115e831b126b628ee984f349911d30ffb9d613b50a84dfa1f042ba5" +
                "36b82d5101e711c629f9f2096dc834deec63b70f2a2315a6d27323b995aa20d3" +
                "d0737075186f5049af6f512a0c38a9da06817f4b619b94520edfac85c4a6e2e1" +
                "86225c95a04ec3c3422b8deb284e98d24b31465802008a097c25969e826c2baa" +
                "59d2cba33d6c1d9f3962330c1fcda7cfb18508fea7d0555e3a169daed353f3ee" +
                "6f4bb30244319161dff6438a37ca793b24bbb1b1bc2194fc6e6ef60278157899" +
                "cb03c5dd6fc91a836eb20a25c09945643d95f7bd50d206684d6ffc14d16d82d5" +
                "f781225bff908392a5793b803f9b70b4dfcb394f9ed81c18e391a09eb3f93a03" +
                "2d81ba670cabfd6f64aa5e3374cb7c2029f45200e4f0bfd820c8bd58dc5eeb34";

            const string x =
                "150b5c51ea6402276bc912322f0404f6d57ff7d32afcaa83b6dfde11abb48181";

            const string y =
                "6da54f2b0ddb4dcce2da1edfa16ba84953d8429ce60cd111a5c65edcf7ba5b8d" +
                "9387ab6881c24880b2afbdb437e9ed7ffb8e96beca7ea80d1d90f24d54611262" +
                "9df5c9e9661742cc872fdb3d409bc77b75b17c7e6cfff86261071c4b5c9f9898" +
                "be1e9e27349b933c34fb345685f8fc6c12470d124cecf51b5d5adbf5e7a2490f" +
                "8d67aac53a82ed6a2110686cf631c348bcbc4cf156f3a6980163e2feca72a45f" +
                "6b3d68c10e5a2283b470b7292674490383f75fa26ccf93c0e1c8d0628ca35f2f" +
                "3d9b6876505d118988957237a2fc8051cb47b410e8b7a619e73b1350a9f6a260" +
                "c5f16841e7c4db53d8eaa0b4708d62f95b2a72e2f04ca14647bca6b5e3ee707f" +
                "cdf758b925eb8d4e6ace4fc7443c9bc5819ff9e555be098aa055066828e21b81" +
                "8fedc3aac517a0ee8f9060bd86e0d4cce212ab6a3a243c5ec0274563353ca710" +
                "3af085e8f41be524fbb75cda88903907df94bfd69373e288949bd0626d85c139" +
                "8b3073a139d5c747d24afdae7a3e745437335d0ee993eef36a3041c912f7eb58";

            const string msg =
                "494180eed0951371bbaf0a850ef13679df49c1f13fe3770b6c13285bf3ad93dc" +
                "4ab018aab9139d74200808e9c55bf88300324cc697efeaa641d37f3acf72d8c9" +
                "7bff0182a35b940150c98a03ef41a3e1487440c923a988e53ca3ce883a2fb532" +
                "bb7441c122f1dc2f9d0b0bc07f26ba29a35cdf0da846a9d8eab405cbf8c8e77f";

            const string r =
                "a40a6c905654c55fc58e99c7d1a3feea2c5be64823d4086ce811f334cfdc448d";

            const string s =
                "6478050977ec585980454e0a2f26a03037b921ca588a78a4daff7e84d49a8a6c";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA512);
        }

        [ConditionalFact(nameof(SupportsFips186_3))]
        public static void Fips186_3_L3072_N256_SHA512_12()
        {
            // http://csrc.nist.gov/groups/STM/cavp/documents/dss/186-3dsatestvectors.zip
            // SigGen.txt
            // [mod = L=3072, N=256, SHA-512], twelfth case (y=00...)
            const string p =
                "c1d0a6d0b5ed615dee76ac5a60dd35ecb000a202063018b1ba0a06fe7a00f765" +
                "db1c59a680cecfe3ad41475badb5ad50b6147e2596b88d34656052aca79486ea" +
                "6f6ec90b23e363f3ab8cdc8b93b62a070e02688ea877843a4685c2ba6db111e9" +
                "addbd7ca4bce65bb10c9ceb69bf806e2ebd7e54edeb7f996a65c907b50efdf8e" +
                "575bae462a219c302fef2ae81d73cee75274625b5fc29c6d60c057ed9e7b0d46" +
                "ad2f57fe01f823230f31422722319ce0abf1f141f326c00fbc2be4cdb8944b6f" +
                "d050bd300bdb1c5f4da72537e553e01d51239c4d461860f1fb4fd8fa79f5d526" +
                "3ff62fed7008e2e0a2d36bf7b9062d0d75db226c3464b67ba24101b085f2c670" +
                "c0f87ae530d98ee60c5472f4aa15fb25041e19106354da06bc2b1d322d40ed97" +
                "b21fd1cdad3025c69da6ce9c7ddf3dcf1ea4d56577bfdec23071c1f05ee4077b" +
                "5391e9a404eaffe12d1ea62d06acd6bf19e91a158d2066b4cd20e4c4e52ffb1d" +
                "5204cd022bc7108f2c799fb468866ef1cb09bce09dfd49e4740ff8140497be61";

            const string q =
                "bf65441c987b7737385eadec158dd01614da6f15386248e59f3cddbefc8e9dd1";

            const string g =
                "c02ac85375fab80ba2a784b94e4d145b3be0f92090eba17bd12358cf3e03f437" +
                "9584f8742252f76b1ede3fc37281420e74a963e4c088796ff2bab8db6e9a4530" +
                "fc67d51f88b905ab43995aab46364cb40c1256f0466f3dbce36203ef228b35e9" +
                "0247e95e5115e831b126b628ee984f349911d30ffb9d613b50a84dfa1f042ba5" +
                "36b82d5101e711c629f9f2096dc834deec63b70f2a2315a6d27323b995aa20d3" +
                "d0737075186f5049af6f512a0c38a9da06817f4b619b94520edfac85c4a6e2e1" +
                "86225c95a04ec3c3422b8deb284e98d24b31465802008a097c25969e826c2baa" +
                "59d2cba33d6c1d9f3962330c1fcda7cfb18508fea7d0555e3a169daed353f3ee" +
                "6f4bb30244319161dff6438a37ca793b24bbb1b1bc2194fc6e6ef60278157899" +
                "cb03c5dd6fc91a836eb20a25c09945643d95f7bd50d206684d6ffc14d16d82d5" +
                "f781225bff908392a5793b803f9b70b4dfcb394f9ed81c18e391a09eb3f93a03" +
                "2d81ba670cabfd6f64aa5e3374cb7c2029f45200e4f0bfd820c8bd58dc5eeb34";

            const string x =
                "bd3006cf5d3ac04a8a5128140df6025d9942d78544e9b27efe28b2ca1f79e313";

            const string y =
                "00728e23e74bb82de0e1315d58164a5cecc8951d89e88da702f5b878020fd8d2" +
                "a1791b3e8ab770e084ac2397d297971ca8708a30a4097d86740153ee2db6ab63" +
                "43c5b6cc2c8a7fa59082a8d659931cc48a0433a033dbb2fff3aa545686f922c7" +
                "063da1d52d9688142ec64a1002948e5da89165d9df8eed9aa469b61ee0210b40" +
                "33562333097ba8659944e5f7924e04a21bc3edc6d551e202e4c543e97518f91e" +
                "0cab49111029b29c3aa1bed5f35e5c90feb9d3c745953dbf859defce4537b4a0" +
                "9801fdc8fe6999fbde39908079811b4b992c2e8333b9f800ea0d9f0a5f53607e" +
                "308942e68efef01e03d7cca6f196872bf01f436d4a8e05fc59d8fbc6b88a166f" +
                "57a4e99d67ddaece844653be77819747dd2e07d581c518cb9779e9f7960c17ff" +
                "0bae710ecf575b09591b013b4805c88b235df262e61a4c94f46bf9a08284611d" +
                "f44eadd94f44cef6225a808e211e4d3af5e96bce64a90f8013874f10749a8382" +
                "a6026a855d90853440bfce31f258b3a258f7b5e659b43e702dee7c24c02d2284";

            const string msg =
                "baeb12a1ebd8057a99a0137ee60f60eed10d26f1eab22ae2d9adbc3e5ffc3252" +
                "abf62b614707ad2546141bed779f0cfad9544a74e562da549e2f7b286efb6154" +
                "49b0946dc7c498d8f12150b2eacbd27157966f592ad5f3e43a24c60b7e06630b" +
                "82a4fdb699119dbd878b13a98bf22a7b3dc7efdd992ce6b8a950e61299c5663b";

            const string r =
                "8d357b0b956fb90e8e0b9ff284cedc88a04d171a90c5997d8ee1e9bc4d0b35ff";

            const string s =
                "ab37329c50145d146505015704fdc4fb0fd7207e0b11d8becbad934e6255c30c";

            Validate(p, q, g, x, y, msg, r, s, HashAlgorithmName.SHA512);
        }

        private static void Validate(
            string p,
            string q,
            string g,
            string x,
            string y,
            string msg,
            string r,
            string s,
            HashAlgorithmName hashAlgorithm)
        {
            // Public+Private key
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(
                    new DSAParameters
                    {
                        P = p.HexToByteArray(),
                        Q = q.HexToByteArray(),
                        G = g.HexToByteArray(),
                        X = x.HexToByteArray(),
                        Y = y.HexToByteArray(),
                    });

                byte[] message = msg.HexToByteArray();
                byte[] signature = (r + s).HexToByteArray();

                Assert.True(dsa.VerifyData(message, signature, hashAlgorithm), "Public+Private Valid Signature");

                signature[0] ^= 0xFF;
                Assert.False(dsa.VerifyData(message, signature, hashAlgorithm), "Public+Private Tampered Signature");
            }

            // Public only
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.ImportParameters(
                    new DSAParameters
                    {
                        P = p.HexToByteArray(),
                        Q = q.HexToByteArray(),
                        G = g.HexToByteArray(),
                        X = null,
                        Y = y.HexToByteArray(),
                    });

                byte[] message = msg.HexToByteArray();
                byte[] signature = (r + s).HexToByteArray();

                Assert.True(dsa.VerifyData(message, signature, hashAlgorithm), "Public-Only Valid Signature");

                signature[0] ^= 0xFF;
                Assert.False(dsa.VerifyData(message, signature, hashAlgorithm), "Public-Only Tampered Signature");
            }
        }
    }
}
