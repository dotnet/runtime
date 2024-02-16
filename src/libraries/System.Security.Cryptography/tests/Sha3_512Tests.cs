// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public class SHA3_512Tests : HashAlgorithmTestDriver<SHA3_512Tests.Traits>
    {
        public sealed class Traits : IHashTrait
        {
            public static bool IsSupported => SHA3_512.IsSupported;
            public static int HashSizeInBytes => SHA3_512.HashSizeInBytes;
        }

        protected override HashAlgorithm Create() => SHA3_512.Create();
        protected override HashAlgorithmName HashAlgorithm => HashAlgorithmName.SHA3_512;

        protected override bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            return SHA3_512.TryHashData(source, destination, out bytesWritten);
        }

        protected override byte[] HashData(byte[] source) => SHA3_512.HashData(source);

        protected override byte[] HashData(ReadOnlySpan<byte> source) => SHA3_512.HashData(source);

        protected override int HashData(ReadOnlySpan<byte> source, Span<byte> destination) =>
            SHA3_512.HashData(source, destination);

        protected override int HashData(Stream source, Span<byte> destination) =>
            SHA3_512.HashData(source, destination);

        protected override byte[] HashData(Stream source) => SHA3_512.HashData(source);

        protected override ValueTask<int> HashDataAsync(Stream source, Memory<byte> destination, CancellationToken cancellationToken) =>
            SHA3_512.HashDataAsync(source, destination, cancellationToken);

        protected override ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken) =>
            SHA3_512.HashDataAsync(source, cancellationToken);

        [ConditionalFact(nameof(IsSupported))]
        public void SHA3_512_Kats()
        {
            foreach ((string Msg, string MD) kat in Fips202Kats)
            {
                Verify(Convert.FromHexString(kat.Msg), kat.MD);
            }
        }

        [ConditionalFact(nameof(IsSupported))]
        public void SHA3_512_Empty_Stream()
        {
            VerifyRepeating(
                "",
                0,
                "a69f73cca23a9ac5c8b567dc185a756e97c982164fe25859e0d1dcc1475c80a615b2123af1f5f94c11e3e9402c3ac558f500199d95b6d3e301758586281dcd26");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task SHA3_512_Empty_Stream_Async()
        {
            await VerifyRepeatingAsync(
                "",
                0,
                "a69f73cca23a9ac5c8b567dc185a756e97c982164fe25859e0d1dcc1475c80a615b2123af1f5f94c11e3e9402c3ac558f500199d95b6d3e301758586281dcd26");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void SHA3_512_VerifyLargeStream_MultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -sha3-512
            VerifyRepeating(
                "0102030405060708",
                1024,
                "b5ec7fe7061c944b65f42a3193ebafcc3b35f063dc2ac7a5af05140b2439c425e4d9e63bc97103f704a7b6849a1986cec743ac288ca2f123e82c0ce60b714615");
        }

        [ConditionalFact(nameof(IsSupported))]
        public void SHA3_512_VerifyLargeStream_NotMultipleOf4096()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -sha3-512
            VerifyRepeating(
                "0102030405060708",
                1025,
                "ea418b3d279a9b25ddc6f8a294006c63068cbd4b872163365f7d11f6f287c8291adc0e3b77999db9606a40c989d7eca405247162104feec1d5a46e59404692a2");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task SHA3_512_VerifyLargeStream_NotMultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1025}; do echo -n "0102030405060708"; done | openssl dgst -sha3-512
            await VerifyRepeatingAsync(
                "0102030405060708",
                1025,
                "ea418b3d279a9b25ddc6f8a294006c63068cbd4b872163365f7d11f6f287c8291adc0e3b77999db9606a40c989d7eca405247162104feec1d5a46e59404692a2");
        }

        [ConditionalFact(nameof(IsSupported))]
        public async Task SHA3_512_VerifyLargeStream_MultipleOf4096_Async()
        {
            // Verfied with:
            // for _ in {1..1024}; do echo -n "0102030405060708"; done | openssl dgst -sha3-512
            await VerifyRepeatingAsync(
                "0102030405060708",
                1024,
                "b5ec7fe7061c944b65f42a3193ebafcc3b35f063dc2ac7a5af05140b2439c425e4d9e63bc97103f704a7b6849a1986cec743ac288ca2f123e82c0ce60b714615");
        }

        [Fact]
        public void SHA3_512_HashSizes()
        {
            Assert.Equal(512, SHA3_512.HashSizeInBits);
            Assert.Equal(64, SHA3_512.HashSizeInBytes);
        }

        [Fact]
        public void SHA3_512_IsSupported_AgreesWithPlatformVersion()
        {
            Assert.Equal(PlatformDetection.SupportsSha3, SHA3_512.IsSupported);
        }

        private static IEnumerable<(string Msg, string MD)> Fips202Kats
        {
            get
            {
                // Generated from SHA3_512ShortMsg.rsp
                yield return (Msg: "", MD: "a69f73cca23a9ac5c8b567dc185a756e97c982164fe25859e0d1dcc1475c80a615b2123af1f5f94c11e3e9402c3ac558f500199d95b6d3e301758586281dcd26");
                yield return (Msg: "e5", MD: "150240baf95fb36f8ccb87a19a41767e7aed95125075a2b2dbba6e565e1ce8575f2b042b62e29a04e9440314a821c6224182964d8b557b16a492b3806f4c39c1");
                yield return (Msg: "ef26", MD: "809b4124d2b174731db14585c253194c8619a68294c8c48947879316fef249b1575da81ab72aad8fae08d24ece75ca1be46d0634143705d79d2f5177856a0437");
                yield return (Msg: "37d518", MD: "4aa96b1547e6402c0eee781acaa660797efe26ec00b4f2e0aec4a6d10688dd64cbd7f12b3b6c7f802e2096c041208b9289aec380d1a748fdfcd4128553d781e3");
                yield return (Msg: "fc7b8cda", MD: "58a5422d6b15eb1f223ebe4f4a5281bc6824d1599d979f4c6fe45695ca89014260b859a2d46ebf75f51ff204927932c79270dd7aef975657bb48fe09d8ea008e");
                yield return (Msg: "4775c86b1c", MD: "ce96da8bcd6bc9d81419f0dd3308e3ef541bc7b030eee1339cf8b3c4e8420cd303180f8da77037c8c1ae375cab81ee475710923b9519adbddedb36db0c199f70");
                yield return (Msg: "71a986d2f662", MD: "def6aac2b08c98d56a0501a8cb93f5b47d6322daf99e03255457c303326395f765576930f8571d89c01e727cc79c2d4497f85c45691b554e20da810c2bc865ef");
                yield return (Msg: "ec83d707a1414a", MD: "84fd3775bac5b87e550d03ec6fe4905cc60e851a4c33a61858d4e7d8a34d471f05008b9a1d63044445df5a9fce958cb012a6ac778ecf45104b0fcb979aa4692d");
                yield return (Msg: "af53fa3ff8a3cfb2", MD: "03c2ac02de1765497a0a6af466fb64758e3283ed83d02c0edb3904fd3cf296442e790018d4bf4ce55bc869cebb4aa1a799afc9d987e776fef5dfe6628e24de97");
                yield return (Msg: "3d6093966950abd846", MD: "53e30da8b74ae76abf1f65761653ebfbe87882e9ea0ea564addd7cfd5a6524578ad6be014d7799799ef5e15c679582b791159add823b95c91e26de62dcb74cfa");
                yield return (Msg: "1ca984dcc913344370cf", MD: "6915ea0eeffb99b9b246a0e34daf3947852684c3d618260119a22835659e4f23d4eb66a15d0affb8e93771578f5e8f25b7a5f2a55f511fb8b96325ba2cd14816");
                yield return (Msg: "fc7b8cdadebe48588f6851", MD: "c8439bb1285120b3c43631a00a3b5ac0badb4113586a3dd4f7c66c5d81012f7412617b169fa6d70f8e0a19e5e258e99a0ed2dcfa774c864c62a010e9b90ca00d");
                yield return (Msg: "ecb907adfb85f9154a3c23e8", MD: "94ae34fed2ef51a383fb853296e4b797e48e00cad27f094d2f411c400c4960ca4c610bf3dc40e94ecfd0c7a18e418877e182ca3ae5ca5136e2856a5531710f48");
                yield return (Msg: "d91a9c324ece84b072d0753618", MD: "fb1f06c4d1c0d066bdd850ab1a78b83296eba0ca423bb174d74283f46628e6095539214adfd82b462e8e9204a397a83c6842b721a32e8bb030927a568f3c29e6");
                yield return (Msg: "c61a9188812ae73994bc0d6d4021", MD: "069e6ab1675fed8d44105f3b62bbf5b8ff7ae804098986879b11e0d7d9b1b4cb7bc47aeb74201f509ddc92e5633abd2cbe0ddca2480e9908afa632c8c8d5af2a");
                yield return (Msg: "a6e7b218449840d134b566290dc896", MD: "3605a21ce00b289022193b70b535e6626f324739542978f5b307194fcf0a5988f542c0838a0443bb9bb8ff922a6a177fdbd12cf805f3ed809c48e9769c8bbd91");
                yield return (Msg: "054095ba531eec22113cc345e83795c7", MD: "f3adf5ccf2830cd621958021ef998252f2b6bc4c135096839586d5064a2978154ea076c600a97364bce0e9aab43b7f1f2da93537089de950557674ae6251ca4d");
                yield return (Msg: "5b1ec1c4e920f5b995b6a788b6e989ac29", MD: "135eea17ca4785482c19cd668b8dd2913216903311fa21f6b670b9b573264f8875b5d3c071d92d63556549e523b2af1f1a508bd1f105d29a436f455cd2ca1604");
                yield return (Msg: "133b497b00932773a53ba9bf8e61d59f05f4", MD: "783964a1cf41d6d210a8d7c81ce6970aa62c9053cb89e15f88053957ecf607f42af08804e76f2fbdbb31809c9eefc60e233d6624367a3b9c30f8ee5f65be56ac");
                yield return (Msg: "88c050ea6b66b01256bda299f399398e1e3162", MD: "6bf7fc8e9014f35c4bde6a2c7ce1965d9c1793f25c141021cc1c697d111363b3854953c2b4009df41878b5558e78a9a9092c22b8baa0ed6baca005455c6cca70");
                yield return (Msg: "d7d5363350709e96939e6b68b3bbdef6999ac8d9", MD: "7a46beca553fffa8021b0989f40a6563a8afb641e8133090bc034ab6763e96d7b7a0da4de3abd5a67d8085f7c28b21a24aefb359c37fac61d3a5374b4b1fb6bb");
                yield return (Msg: "54746a7ba28b5f263d2496bd0080d83520cd2dc503", MD: "d77048df60e20d03d336bfa634bc9931c2d3c1e1065d3a07f14ae01a085fe7e7fe6a89dc4c7880f1038938aa8fcd99d2a782d1bbe5eec790858173c7830c87a2");
                yield return (Msg: "73df7885830633fc66c9eb16940b017e9c6f9f871978", MD: "0edee1ea019a5c004fd8ae9dc8c2dd38d4331abe2968e1e9e0c128d2506db981a307c0f19bc2e62487a92992af77588d3ab7854fe1b68302f796b9dcd9f336df");
                yield return (Msg: "14cb35fa933e49b0d0a400183cbbea099c44995fae1163", MD: "af2ef4b0c01e381b4c382208b66ad95d759ec91e386e953984aa5f07774632d53b581eba32ed1d369c46b0a57fee64a02a0e5107c22f14f2227b1d11424becb5");
                yield return (Msg: "75a06869ca2a6ea857e26e78bb78a139a671ccb098d8205a", MD: "88be1934385522ae1d739666f395f1d7f99978d62883a261adf5d618d012dfab5224575634446876b86b3e5f7609d397d338a784b4311027b1024ddfd4995a0a");
                yield return (Msg: "b413ab364dd410573b53f4c2f28982ca07061726e5d999f3c2", MD: "289e889b25f9f38facfccf3bdbceea06ef3baad6e9612b7232cd553f4884a7a642f6583a1a589d4dcb2dc771f1ff6d711b85f731145a89b100680f9a55dcbb3f");
                yield return (Msg: "d7f9053984213ebabc842fd8ce483609a9af5dc140ecdbe63336", MD: "f167cb30e4bacbdc5ed53bc615f8c9ea19ad4f6bd85ca0ff5fb1f1cbe5b576bda49276aa5814291a7e320f1d687b16ba8d7daab2b3d7e9af3cd9f84a1e9979a1");
                yield return (Msg: "9b7f9d11be48e786a11a472ab2344c57adf62f7c1d4e6d282074b6", MD: "82fa525d5efaa3cce39bffef8eee01afb52067097f8965cde71703345322645eae59dbaebed0805693104dfb0c5811c5828da9a75d812e5562615248c03ff880");
                yield return (Msg: "115784b1fccfabca457c4e27a24a7832280b7e7d6a123ffce5fdab72", MD: "ec12c4ed5ae84808883c5351003f7e26e1eaf509c866b357f97472e5e19c84f99f16dbbb8bfff060d6c0fe0ca9c34a210c909b05f6a81f441627ce8e666f6dc7");
                yield return (Msg: "c3b1ad16b2877def8d080477d8b59152fe5e84f3f3380d55182f36eb5f", MD: "4b9144edeeec28fd52ba4176a78e080e57782d2329b67d8ac8780bb6e8c2057583172af1d068922feaaff759be5a6ea548f5db51f4c34dfe7236ca09a67921c7");
                yield return (Msg: "4c66ca7a01129eaca1d99a08dd7226a5824b840d06d0059c60e97d291dc4", MD: "567c46f2f636223bd5ed3dc98c3f7a739b42898e70886f132eac43c2a6fadabe0dd9f1b6bc4a9365e5232295ac1ac34701b0fb181d2f7f07a79d033dd426d5a2");
                yield return (Msg: "481041c2f56662316ee85a10b98e103c8d48804f6f9502cf1b51cfa525cec1", MD: "46f0058abe678195b576df5c7eb8d739468cad1908f7953ea39c93fa1d96845c38a2934d23804864a8368dae38191d983053ccd045a9ab87ef2619e9dd50c8c1");
                yield return (Msg: "7c1688217b313278b9eae8edcf8aa4271614296d0c1e8916f9e0e940d28b88c5", MD: "627ba4de74d05bb6df8991112e4d373bfced37acde1304e0f664f29fa126cb497c8a1b717b9929120883ec8898968e4649013b760a2180a9dc0fc9b27f5b7f3b");
                yield return (Msg: "785f6513fcd92b674c450e85da22257b8e85bfa65e5d9b1b1ffc5c469ad337d1e3", MD: "5c11d6e4c5c5f76d26876c5976b6f555c255c785b2f28b6700ca2d8b3b3fa585636239277773330f4cf8c5d5203bcc091b8d47e7743bbc0b5a2c54444ee2acce");
                yield return (Msg: "34f4468e2d567b1e326c0942970efa32c5ca2e95d42c98eb5d3cab2889490ea16ee5", MD: "49adfa335e183c94b3160154d6698e318c8b5dd100b0227e3e34cabea1fe0f745326220f64263961349996bbe1aae9054de6406e8b350408ab0b9f656bb8daf7");
                yield return (Msg: "53a0121c8993b6f6eec921d2445035dd90654add1298c6727a2aed9b59bafb7dd62070", MD: "918b4d92e1fcb65a4c1fa0bd75c562ac9d83186bb2fbfae5c4784de31a14654546e107df0e79076b8687bb3841c83ba9181f9956cd43428ba72f603881b33a71");
                yield return (Msg: "d30fa4b40c9f84ac9bcbb535e86989ec6d1bec9b1b22e9b0f97370ed0f0d566082899d96", MD: "39f104c1da4af314d6bceb34eca1dfe4e67484519eb76ba38e4701e113e6cbc0200df86e4439d674b0f42c72233360478ba5244384d28e388c87aaa817007c69");
                yield return (Msg: "f34d100269aee3ead156895e8644d4749464d5921d6157dffcbbadf7a719aee35ae0fd4872", MD: "565a1dd9d49f8ddefb79a3c7a209f53f0bc9f5396269b1ce2a2b283a3cb45ee3ae652e4ca10b26ced7e5236227006c94a37553db1b6fe5c0c2eded756c896bb1");
                yield return (Msg: "12529769fe5191d3fce860f434ab1130ce389d340fca232cc50b7536e62ad617742e022ea38a", MD: "daee10e815fff0f0985d208886e22f9bf20a3643eb9a29fda469b6a7dcd54b5213c851d6f19338d63688fe1f02936c5dae1b7c6d5906a13a9eeb934400b6fe8c");
                yield return (Msg: "b2e3a0eb36bf16afb618bfd42a56789179147effecc684d8e39f037ec7b2d23f3f57f6d7a7d0bb", MD: "04029d6d9e8e394afa387f1d03ab6b8a0a6cbab4b6b3c86ef62f7142ab3c108388d42cb87258b9e6d36e5814d8a662657cf717b35a5708365e8ec0396ec5546b");
                yield return (Msg: "25c4a5f4a07f2b81e0533313664bf615c73257e6b2930e752fe5050e25ff02731fd2872f4f56f727", MD: "ec2d38e5bb5d7b18438d5f2029c86d05a03510db0e66aa299c28635abd0988c58be203f04b7e0cc25451d18f2341cd46f8705d46c2066dafab30d90d63bf3d2c");
                yield return (Msg: "134bb8e7ea5ff9edb69e8f6bbd498eb4537580b7fba7ad31d0a09921237acd7d66f4da23480b9c1222", MD: "8f966aef96831a1499d63560b2578021ad970bf7557b8bf8078b3e12cefab122fe71b1212dc704f7094a40b36b71d3ad7ce2d30f72c1baa4d4bbccb3251198ac");
                yield return (Msg: "f793256f039fad11af24cee4d223cd2a771598289995ab802b5930ba5c666a24188453dcd2f0842b8152", MD: "22c3d9712535153a3e206b1033929c0fd9d937c39ba13cf1a6544dfbd68ebc94867b15fda3f1d30b00bf47f2c4bf41dabdeaa5c397dae901c57db9cd77ddbcc0");
                yield return (Msg: "23cc7f9052d5e22e6712fab88e8dfaa928b6e015ca589c3b89cb745b756ca7c7634a503bf0228e71c28ee2", MD: "6ecf3ad6064218ee101a555d20fab6cbeb6b145b4eeb9c8c971fc7ce05581a34b3c52179590e8a134be2e88c7e549875f4ff89b96374c6995960de3a5098cced");
                yield return (Msg: "a60b7b3df15b3f1b19db15d480388b0f3b00837369aa2cc7c3d7315775d7309a2d6f6d1371d9c875350dec0a", MD: "8d651605c6b32bf022ea06ce6306b2ca6b5ba2781af87ca2375860315c83ad88743030d148ed8d73194c461ec1e84c045fc914705747614c04c8865b51da94f7");
                yield return (Msg: "2745dd2f1b215ea509a912e5761cccc4f19fa93ba38445c528cb2f099de99ab9fac955baa211fd8539a671cdb6", MD: "4af918eb676ce278c730212ef79d818773a76a43c74d643f238e9b61acaf4030c617c4d6b3b7514c59b3e5e95d82e1e1e35443e851718b13b63e70b123d1b72c");
                yield return (Msg: "88adee4b46d2a109c36fcfb660f17f48062f7a74679fb07e86cad84f79fd57c86d426356ec8e68c65b3caa5bc7ba", MD: "6257acb9f589c919c93c0adc4e907fe011bef6018fbb18e618ba6fcc8cbc5e40641be589e86dbb0cf7d7d6bf33b98d8458cce0af7857f5a7c7647cf350e25af0");
                yield return (Msg: "7d40f2dc4af3cfa12b00d64940dc32a22d66d81cb628be2b8dda47ed6728020d55b695e75260f4ec18c6d74839086a", MD: "5c46c84a0a02d898ed5885ce99c47c77afd29ae015d027f2485d630f9b41d00b7c1f1faf6ce57a08b604b35021f7f79600381994b731bd8e6a5b010aeb90e1eb");
                yield return (Msg: "3689d8836af0dc132f85b212eb670b41ecf9d4aba141092a0a8eca2e6d5eb0ba4b7e61af9273624d14192df7388a8436", MD: "17355e61d66e40f750d0a9a8e8a88cd6f9bf6070b7efa76442698740b4487ea6c644d1654ef16a265204e03084a14cafdccf8ff298cd54c0b4009967b6dd47cc");
                yield return (Msg: "58ff23dee2298c2ca7146227789c1d4093551047192d862fc34c1112d13f1f744456cecc4d4a02410523b4b15e598df75a", MD: "aca89aa547c46173b4b2a380ba980da6f9ac084f46ac9ddea5e4164aeef31a9955b814a45aec1d8ce340bd37680952c5d68226dda1cac2677f73c9fd9174fd13");
                yield return (Msg: "67f3f23df3bd8ebeb0096452fe4775fd9cc71fbb6e72fdcc7eb8094f42c903121d0817a927bcbabd3109d5a70420253deab2", MD: "f4207cc565f266a245f29bf20b95b5d9a83e1bb68ad988edc91faa25f25286c8398bac7dd6628259bff98f28360f263dfc54c4228bc437c5691de1219b758d9f");
                yield return (Msg: "a225070c2cb122c3354c74a254fc7b84061cba33005cab88c409fbd3738ff67ce23c41ebef46c7a61610f5b93fa92a5bda9569", MD: "e815a9a4e4887be014635e97958341e0519314b3a3289e1835121b153b462272b0aca418be96d60e5ab355d3eb463697c0191eb522b60b8463d89f4c3f1bf142");
                yield return (Msg: "6aa0886777e99c9acd5f1db6e12bda59a807f92411ae99c9d490b5656acb4b115c57beb3c1807a1b029ad64be1f03e15bafd91ec", MD: "241f2ebaf7ad09e173b184244e69acd7ebc94774d0fa3902cbf267d4806063b044131bcf4af4cf180eb7bd4e7960ce5fe3dc6aebfc6b90eec461f414f79a67d9");
                yield return (Msg: "6a06092a3cd221ae86b286b31f326248270472c5ea510cb9064d6024d10efee7f59e98785d4f09da554e97cdec7b75429d788c112f", MD: "d14a1a47f2bef9e0d4b3e90a6be9ab5893e1110b12db38d33ffb9a61e1661aecc4ea100839cfee58a1c5aff72915c14170dd99e13f71b0a5fc1985bf43415cb0");
                yield return (Msg: "dfc3fa61f7fffc7c88ed90e51dfc39a4f288b50d58ac83385b58a3b2a3a39d729862c40fcaf9bc308f713a43eecb0b72bb9458d204ba", MD: "947bc873dc41df195f8045deb6ea1b840f633917e79c70a88d38b8862197dc2ab0cc6314e974fb5ba7e1703b22b1309e37bd430879056bdc166573075a9c5e04");
                yield return (Msg: "52958b1ff0049efa5d050ab381ec99732e554dcd03725da991a37a80bd4756cf65d367c54721e93f1e0a22f70d36e9f841336956d3c523", MD: "9cc5aad0f529f4bac491d733537b69c8ec700fe38ab423d815e0927c8657f9cb8f4207762d816ab697580122066bc2b68f4177335d0a6e9081540779e572c41f");
                yield return (Msg: "302fa84fdaa82081b1192b847b81ddea10a9f05a0f04138fd1da84a39ba5e18e18bc3cea062e6df92ff1ace89b3c5f55043130108abf631e", MD: "8c8eaae9a445643a37df34cfa6a7f09deccab2a222c421d2fc574bbc5641e504354391e81eb5130280b1226812556d474e951bb78dbdd9b77d19f647e2e7d7be");
                yield return (Msg: "b82f500d6bc2dddcdc162d46cbfaa5ae64025d5c1cd72472dcd2c42161c9871ce329f94df445f0c8aceecafd0344f6317ecbb62f0ec2223a35", MD: "55c69d7accd179d5d9fcc522f794e7af5f0eec7198ffa39f80fb55b866c0857ff3e7aeef33e130d9c74ef90606ca821d20b7608b12e6e561f9e6c7122ace3db0");
                yield return (Msg: "86da9107ca3e16a2b58950e656a15c085b88033e79313e2c0f92f99f06fa187efba5b8fea08eb7145f8476304180dd280f36a072b7eac197f085", MD: "0d3b1a0459b4eca801e0737ff9ea4a12b9a483a73a8a92742a93c297b7149326bd92c1643c8177c8924482ab3bbd916c417580cc75d3d3ae096de531bc5dc355");
                yield return (Msg: "141a6eafe157053e780ac7a57b97990616ce1759ed132cb453bcdfcabdbb70b3767da4eb94125d9c2a8d6d20bfaeacc1ffbe49c4b1bb5da7e9b5c6", MD: "bdbdd5b94cdc89466e7670c63ba6a55b58294e93b351261a5457bf5a40f1b5b2e0acc7fceb1bfb4c8872777eeeaff7927fd3635ca18c996d870bf86b12b89ba5");
                yield return (Msg: "6e0c65ee0943e34d9bbd27a8547690f2291f5a86d713c2be258e6ac16919fe9c4d491895d3a961bb97f5fac255891a0eaa18f80e1fa1ebcb639fcfc1", MD: "39ebb992b8d39daae973e3813a50e9e79a67d8458a6f17f97a6dd30dd7d11d95701a11129ffeaf7d45781b21cac0c4c034e389d7590df5beeb9805072d0183b9");
                yield return (Msg: "57780b1c79e67fc3beaabead4a67a8cc98b83fa7647eae50c8798b96a516597b448851e93d1a62a098c4767333fcf7b463ce91edde2f3ad0d98f70716d", MD: "3ef36c3effad6eb5ad2d0a67780f80d1b90efcb74db20410c2261a3ab0f784429df874814748dc1b6efaab3d06dd0a41ba54fce59b67d45838eaa4aa1fadfa0f");
                yield return (Msg: "bcc9849da4091d0edfe908e7c3386b0cadadb2859829c9dfee3d8ecf9dec86196eb2ceb093c5551f7e9a4927faabcfaa7478f7c899cbef4727417738fc06", MD: "1fcd8a2c7b4fd98fcdc5fa665bab49bde3f9f556aa66b3646638f5a2d3806192f8a33145d8d0c535c85adff3cc0ea3c2715b33cec9f8886e9f4377b3632e9055");
                yield return (Msg: "05a32829642ed4808d6554d16b9b8023353ce65a935d126602970dba791623004dede90b52ac7f0d4335130a63cba68c656c139989614de20913e83db320db", MD: "49d8747bb53ddde6d1485965208670d1130bf35619d7506a2f2040d1129fcf0320207e5b36fea083e84ffc98755e691ad8bd5dc66f8972cb9857389344e11aad");
                yield return (Msg: "56ac4f6845a451dac3e8886f97f7024b64b1b1e9c5181c059b5755b9a6042be653a2a0d5d56a9e1e774be5c9312f48b4798019345beae2ffcc63554a3c69862e", MD: "5fde5c57a31febb98061f27e4506fa5c245506336ee90d595c91d791a5975c712b3ab9b3b5868f941db0aeb4c6d2837c4447442f8402e0e150a9dc0ef178dca8");
                yield return (Msg: "8a229f8d0294fe90d4cc8c875460d5d623f93287f905a999a2ab0f9a47046f78ef88b09445c671189c59388b3017cca2af8bdf59f8a6f04322b1701ec08624ab63", MD: "16b0fd239cc632842c443e1b92d286dd519cfc616a41f2456dd5cddebd10703c3e9cb669004b7f169bb4f99f350ec96904b0e8dd4de8e6be9953dc892c65099f");
                yield return (Msg: "87d6aa9979025b2437ea8159ea1d3e5d6f17f0a5b913b56970212f56de7884840c0da9a72865e1892aa780b8b8f5f57b46fc070b81ca5f00eee0470ace89b1e1466a", MD: "d816acf1797decfe34f4cc49e52aa505cc59bd17fe69dc9543fad82e9cf96298183021f704054d3d06adde2bf54e82a090a57b239e88daa04cb76c4fc9127843");
                yield return (Msg: "0823616ab87e4904308628c2226e721bb4169b7d34e8744a0700b721e38fe05e3f813fe4075d4c1a936d3a33da20cfb3e3ac722e7df7865330b8f62a73d9119a1f2199", MD: "e1da6be4403a4fd784c59be4e71c658a78bb8c5d7d571c5e816fbb3e218a4162f62de1c285f3779781cb5506e29c94e1b7c7d65af2aa71ea5c96d9585b5e45d5");
                yield return (Msg: "7d2d913c2460c09898b20366ae34775b1564f10edea49c073cebe41989bb93f38a533af1f425d3382f8aa40159b567358ee5a73b67df6d0dc09c1c92bf3f9a28124ab07f", MD: "3aa1e19a52b86cf414d977768bb535b7e5817117d436b4425ec8d775e8cb0e0b538072213884c7ff1bb9ca9984c82d65cb0115cc07332b0ea903e3b38650e88e");
                yield return (Msg: "fca5f68fd2d3a52187b349a8d2726b608fccea7db42e906b8718e85a0ec654fac70f5a839a8d3ff90cfed7aeb5ea9b08f487fc84e1d9f7fb831dea254468a65ba18cc5a126", MD: "2c74f846ecc722ea4a1eb1162e231b6903291fffa95dd5e1d17dbc2c2be7dfe549a80dd34487d714130ddc9924aed904ad55f49c91c80ceb05c0c034dae0a0a4");
                yield return (Msg: "881ff70ca34a3e1a0e864fd2615ca2a0e63def254e688c37a20ef6297cb3ae4c76d746b5e3d6bb41bd0d05d7df3eeded74351f4eb0ac801abe6dc10ef9b635055ee1dfbf4144", MD: "9a10a7ce23c0497fe8783927f833232ae664f1e1b91302266b6ace25a9c253d1ecab1aaaa62f865469480b2145ed0e489ae3f3f9f7e6da27492c81b07e606fb6");
                yield return (Msg: "b0de0430c200d74bf41ea0c92f8f28e11b68006a884e0d4b0d884533ee58b38a438cc1a75750b6434f467e2d0cd9aa4052ceb793291b93ef83fd5d8620456ce1aff2941b3605a4", MD: "9e9e469ca9226cd012f5c9cc39c96adc22f420030fcee305a0ed27974e3c802701603dac873ae4476e9c3d57e55524483fc01adaef87daa9e304078c59802757");
                yield return (Msg: "0ce9f8c3a990c268f34efd9befdb0f7c4ef8466cfdb01171f8de70dc5fefa92acbe93d29e2ac1a5c2979129f1ab08c0e77de7924ddf68a209cdfa0adc62f85c18637d9c6b33f4ff8", MD: "b018a20fcf831dde290e4fb18c56342efe138472cbe142da6b77eea4fce52588c04c808eb32912faa345245a850346faec46c3a16d39bd2e1ddb1816bc57d2da");

                // First 5 from SHA3_512LongMsg.rsp
                yield return (
                    Msg:
                        "664ef2e3a7059daf1c58caf52008c5227e85cdcb83b4c59457f02c508d4f4f69f826bd82c0cffc5cb6a97af6e561c6f96970005285e58f21ef6511d26e709889a7e513c434c90a3c" +
                        "f7448f0caeec7114c747b2a0758a3b4503a7cf0c69873ed31d94dbef2b7b2f168830ef7da3322c3d3e10cafb7c2c33c83bbf4c46a31da90cff3bfd4ccc6ed4b310758491eeba603a" +
                        "76",
                    MD: "e5825ff1a3c070d5a52fbbe711854a440554295ffb7a7969a17908d10163bfbe8f1d52a676e8a0137b56a11cdf0ffbb456bc899fc727d14bd8882232549d914e");

                yield return (
                    Msg:
                        "991c4e7402c7da689dd5525af76fcc58fe9cc1451308c0c4600363586ccc83c9ec10a8c9ddaec3d7cfbd206484d09634b9780108440bf27a5fa4a428446b3214fa17084b6eb197c5" +
                        "c59a4e8df1cfc521826c3b1cbf6f4212f6bfb9bc106dfb5568395643de58bffa2774c31e67f5c1e7017f57caadbb1a56cc5b8a5cf9584552e17e7af9542ba13e9c54695e0dc8f24e" +
                        "ddb93d5a3678e10c8a80ff4f27b677d40bef5cb5f9b3a659cc4127970cd2c11ebf22d514812dfefdd73600dfc10efba38e93e5bff47736126043e50f8b9b941e4ec3083fb762dbf1" +
                        "5c86",
                    MD: "cd0f2a48e9aa8cc700d3f64efb013f3600ebdbb524930c682d21025eab990eb6d7c52e611f884031fafd9360e5225ab7e4ec24cbe97f3af6dbe4a86a4f068ba7");

                yield return (
                    Msg:
                        "22e1df25c30d6e7806cae35cd4317e5f94db028741a76838bfb7d5576fbccab001749a95897122c8d51bb49cfef854563e2b27d9013b28833f161d520856ca4b61c2641c4e184800" +
                        "300aede3518617c7be3a4e6655588f181e9641f8df7a6a42ead423003a8c4ae6be9d767af5623078bb116074638505c10540299219b0155f45b1c18a74548e4328de37a911140531" +
                        "deb6434c534af2449c1abe67e18030681a61240225f87ede15d519b7ce2500bccf33e1364e2fbe6a8a2fe6c15d73242610ed36b0740080812e8902ee531c88e0359020797cbdd1fb" +
                        "78848ae6b5105961d05cdddb8af5fef21b02db94c9810464b8d3ea5f047b94bf0d23931f12df37e102b603cd8e5f5ffa83488df257ddde110106262e0ef16d7ef213e7b49c69276d" +
                        "4d048f",
                    MD: "a6375ff04af0a18fb4c8175f671181b4cf79653a3d70847c6d99694b3f5d41601f1dbef809675c63cac4ec83153b1c78131a7b61024ce36244f320ab8740cb7e");

                yield return (
                    Msg:
                        "8237ce9396ccde3a616754414cdf7b5a958c1eb7f25a48c2781b4e0dba220f8c350d7b02ece252b94f5e2e766189c4ac1a8e67f00acacead402316196a9b0a673e24a33f18b7cb6b" +
                        "e4a066d33e1c93abd8252feb1c8d9cff134ac0c0861150a463264e316172d0b8e7d6043f2bbf71bf97fa7f9070ca3a21b93853ec55ab67a96db884c2113bea0822a70ea46f9ae550" +
                        "1eb55ec74eaa3179fa96d7842092d9e023844ed96f3c9fc35bbc8ee953d677c636fdd578fd5507719e0c55702fed2eaf4f32b35ec29a7a515bbc8bf61f9baf89a77aeb8bc6f24770" +
                        "6c41d398cae5ec80b76abc3a5380001aea500eb31b10160139d5a8e8f1a976dd2dde5ce439a29dba24d370536a14bb87cf201e088e5e3397b3b61477c6a41e22a98af53cc34bc8c5" +
                        "5f15d7924e7e32fed4d3c3ddc2ac8eb1dfc438218c08c6a6a8eea888b208f6092dd9f9df49e7ede8bf11051afd23b0b983a81bcc8d00f7d1f2b27cb04c03aeee59c7df23a17775ae" +
                        "5984eda7",
                    MD: "f08819ec3a9a9806a1f55be4f0e56bce084e66fa271784974bf80e1bed7b2be559ebf5b6396ce52f7db7ef45543965f83064095a70328489178718b491a4100d");

                yield return (
                    Msg:
                        "cfa6c0413dfc1a619417ac3f80fd38247b56941da8c2adf3ff70cc5dabed1875b0395d69d1200b73b1c7820b38868c5b38f52bf3514a96be12e27e34601d95d21c6f51c700b4edf1" +
                        "cac4b2079d487418a4cc5f34f815f469c4b44ef1a7dbaaa9597026c59260c9c22736c49d76ecf7430500b74866cbcfdb5e0fc4fa46cf5ee2b06363ca4ecba6d0104440348d191ec4" +
                        "a4bcbc9763152ffe271a69b805a0b9656970913dfd9e8c02cd16af33a878f083c926f48ab79b1db969fec493aef6c31accc1378867808440a5d5990490b07568bc66e9872904a0f4" +
                        "6ae25ef4077b85ea217bdd12541a9472e2a9840e0d6ab55cc4a523f782f8c19774efbd41dad506bbafc90c438c14c780cab9fab9e74eb9452a0b29438a21878bcd4c6be4edac4e77" +
                        "bfd14a83d6152253a62e826de503880d37bf82d10924fab6bd23f04308a9660499bb223afcc5afd1bd2fa592d0322a9a30eab90bc7ac22018e99d2c8f573554c85b019d0c4cd75e3" +
                        "59e5e9907082a8d660b353588b5f085486d89bd97bb32335cbd8b9adf7d57c72c078d9d08d9c09a70e43da1f1fe5b398ef08d2e06111d9a9b25a893a5d84cd643b0ffab8ef2755f7" +
                        "81c1d6ca49",
                    MD: "3a4c2c9284c90515cb34a0895d0374e87467ffbbc7c1dda3239893a12aeae3b9951169fe85605ef7aa2c483662f3a65c72ff12becde50c23ec6a2bc8864c27c1");
            }
        }
    }
}
