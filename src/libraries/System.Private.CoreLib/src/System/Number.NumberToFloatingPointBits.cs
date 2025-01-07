// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System
{
    internal unsafe partial class Number
    {
        private static ReadOnlySpan<double> Pow10DoubleTable =>
        [
            1e0,    // 10^0
            1e1,    // 10^1
            1e2,    // 10^2
            1e3,    // 10^3
            1e4,    // 10^4
            1e5,    // 10^5
            1e6,    // 10^6
            1e7,    // 10^7
            1e8,    // 10^8
            1e9,    // 10^9
            1e10,   // 10^10
            1e11,   // 10^11
            1e12,   // 10^12
            1e13,   // 10^13
            1e14,   // 10^14
            1e15,   // 10^15
            1e16,   // 10^16
            1e17,   // 10^17
            1e18,   // 10^18
            1e19,   // 10^19
            1e20,   // 10^20
            1e21,   // 10^21
            1e22,   // 10^22
        ];

        /// <summary>
        /// Normalized 128 bits values for powers of 5^q for q in range [-342, 308]
        /// stored as 2 64-bits integers for convenience
        /// </summary>
        private static ReadOnlySpan<ulong> Pow5128Table =>
        [
            0xeef453d6923bd65a, 0x113faa2906a13b3f,
            0x9558b4661b6565f8, 0x4ac7ca59a424c507,
            0xbaaee17fa23ebf76, 0x5d79bcf00d2df649,
            0xe95a99df8ace6f53, 0xf4d82c2c107973dc,
            0x91d8a02bb6c10594, 0x79071b9b8a4be869,
            0xb64ec836a47146f9, 0x9748e2826cdee284,
            0xe3e27a444d8d98b7, 0xfd1b1b2308169b25,
            0x8e6d8c6ab0787f72, 0xfe30f0f5e50e20f7,
            0xb208ef855c969f4f, 0xbdbd2d335e51a935,
            0xde8b2b66b3bc4723, 0xad2c788035e61382,
            0x8b16fb203055ac76, 0x4c3bcb5021afcc31,
            0xaddcb9e83c6b1793, 0xdf4abe242a1bbf3d,
            0xd953e8624b85dd78, 0xd71d6dad34a2af0d,
            0x87d4713d6f33aa6b, 0x8672648c40e5ad68,
            0xa9c98d8ccb009506, 0x680efdaf511f18c2,
            0xd43bf0effdc0ba48, 0x212bd1b2566def2,
            0x84a57695fe98746d, 0x14bb630f7604b57,
            0xa5ced43b7e3e9188, 0x419ea3bd35385e2d,
            0xcf42894a5dce35ea, 0x52064cac828675b9,
            0x818995ce7aa0e1b2, 0x7343efebd1940993,
            0xa1ebfb4219491a1f, 0x1014ebe6c5f90bf8,
            0xca66fa129f9b60a6, 0xd41a26e077774ef6,
            0xfd00b897478238d0, 0x8920b098955522b4,
            0x9e20735e8cb16382, 0x55b46e5f5d5535b0,
            0xc5a890362fddbc62, 0xeb2189f734aa831d,
            0xf712b443bbd52b7b, 0xa5e9ec7501d523e4,
            0x9a6bb0aa55653b2d, 0x47b233c92125366e,
            0xc1069cd4eabe89f8, 0x999ec0bb696e840a,
            0xf148440a256e2c76, 0xc00670ea43ca250d,
            0x96cd2a865764dbca, 0x380406926a5e5728,
            0xbc807527ed3e12bc, 0xc605083704f5ecf2,
            0xeba09271e88d976b, 0xf7864a44c633682e,
            0x93445b8731587ea3, 0x7ab3ee6afbe0211d,
            0xb8157268fdae9e4c, 0x5960ea05bad82964,
            0xe61acf033d1a45df, 0x6fb92487298e33bd,
            0x8fd0c16206306bab, 0xa5d3b6d479f8e056,
            0xb3c4f1ba87bc8696, 0x8f48a4899877186c,
            0xe0b62e2929aba83c, 0x331acdabfe94de87,
            0x8c71dcd9ba0b4925, 0x9ff0c08b7f1d0b14,
            0xaf8e5410288e1b6f, 0x7ecf0ae5ee44dd9,
            0xdb71e91432b1a24a, 0xc9e82cd9f69d6150,
            0x892731ac9faf056e, 0xbe311c083a225cd2,
            0xab70fe17c79ac6ca, 0x6dbd630a48aaf406,
            0xd64d3d9db981787d, 0x92cbbccdad5b108,
            0x85f0468293f0eb4e, 0x25bbf56008c58ea5,
            0xa76c582338ed2621, 0xaf2af2b80af6f24e,
            0xd1476e2c07286faa, 0x1af5af660db4aee1,
            0x82cca4db847945ca, 0x50d98d9fc890ed4d,
            0xa37fce126597973c, 0xe50ff107bab528a0,
            0xcc5fc196fefd7d0c, 0x1e53ed49a96272c8,
            0xff77b1fcbebcdc4f, 0x25e8e89c13bb0f7a,
            0x9faacf3df73609b1, 0x77b191618c54e9ac,
            0xc795830d75038c1d, 0xd59df5b9ef6a2417,
            0xf97ae3d0d2446f25, 0x4b0573286b44ad1d,
            0x9becce62836ac577, 0x4ee367f9430aec32,
            0xc2e801fb244576d5, 0x229c41f793cda73f,
            0xf3a20279ed56d48a, 0x6b43527578c1110f,
            0x9845418c345644d6, 0x830a13896b78aaa9,
            0xbe5691ef416bd60c, 0x23cc986bc656d553,
            0xedec366b11c6cb8f, 0x2cbfbe86b7ec8aa8,
            0x94b3a202eb1c3f39, 0x7bf7d71432f3d6a9,
            0xb9e08a83a5e34f07, 0xdaf5ccd93fb0cc53,
            0xe858ad248f5c22c9, 0xd1b3400f8f9cff68,
            0x91376c36d99995be, 0x23100809b9c21fa1,
            0xb58547448ffffb2d, 0xabd40a0c2832a78a,
            0xe2e69915b3fff9f9, 0x16c90c8f323f516c,
            0x8dd01fad907ffc3b, 0xae3da7d97f6792e3,
            0xb1442798f49ffb4a, 0x99cd11cfdf41779c,
            0xdd95317f31c7fa1d, 0x40405643d711d583,
            0x8a7d3eef7f1cfc52, 0x482835ea666b2572,
            0xad1c8eab5ee43b66, 0xda3243650005eecf,
            0xd863b256369d4a40, 0x90bed43e40076a82,
            0x873e4f75e2224e68, 0x5a7744a6e804a291,
            0xa90de3535aaae202, 0x711515d0a205cb36,
            0xd3515c2831559a83, 0xd5a5b44ca873e03,
            0x8412d9991ed58091, 0xe858790afe9486c2,
            0xa5178fff668ae0b6, 0x626e974dbe39a872,
            0xce5d73ff402d98e3, 0xfb0a3d212dc8128f,
            0x80fa687f881c7f8e, 0x7ce66634bc9d0b99,
            0xa139029f6a239f72, 0x1c1fffc1ebc44e80,
            0xc987434744ac874e, 0xa327ffb266b56220,
            0xfbe9141915d7a922, 0x4bf1ff9f0062baa8,
            0x9d71ac8fada6c9b5, 0x6f773fc3603db4a9,
            0xc4ce17b399107c22, 0xcb550fb4384d21d3,
            0xf6019da07f549b2b, 0x7e2a53a146606a48,
            0x99c102844f94e0fb, 0x2eda7444cbfc426d,
            0xc0314325637a1939, 0xfa911155fefb5308,
            0xf03d93eebc589f88, 0x793555ab7eba27ca,
            0x96267c7535b763b5, 0x4bc1558b2f3458de,
            0xbbb01b9283253ca2, 0x9eb1aaedfb016f16,
            0xea9c227723ee8bcb, 0x465e15a979c1cadc,
            0x92a1958a7675175f, 0xbfacd89ec191ec9,
            0xb749faed14125d36, 0xcef980ec671f667b,
            0xe51c79a85916f484, 0x82b7e12780e7401a,
            0x8f31cc0937ae58d2, 0xd1b2ecb8b0908810,
            0xb2fe3f0b8599ef07, 0x861fa7e6dcb4aa15,
            0xdfbdcece67006ac9, 0x67a791e093e1d49a,
            0x8bd6a141006042bd, 0xe0c8bb2c5c6d24e0,
            0xaecc49914078536d, 0x58fae9f773886e18,
            0xda7f5bf590966848, 0xaf39a475506a899e,
            0x888f99797a5e012d, 0x6d8406c952429603,
            0xaab37fd7d8f58178, 0xc8e5087ba6d33b83,
            0xd5605fcdcf32e1d6, 0xfb1e4a9a90880a64,
            0x855c3be0a17fcd26, 0x5cf2eea09a55067f,
            0xa6b34ad8c9dfc06f, 0xf42faa48c0ea481e,
            0xd0601d8efc57b08b, 0xf13b94daf124da26,
            0x823c12795db6ce57, 0x76c53d08d6b70858,
            0xa2cb1717b52481ed, 0x54768c4b0c64ca6e,
            0xcb7ddcdda26da268, 0xa9942f5dcf7dfd09,
            0xfe5d54150b090b02, 0xd3f93b35435d7c4c,
            0x9efa548d26e5a6e1, 0xc47bc5014a1a6daf,
            0xc6b8e9b0709f109a, 0x359ab6419ca1091b,
            0xf867241c8cc6d4c0, 0xc30163d203c94b62,
            0x9b407691d7fc44f8, 0x79e0de63425dcf1d,
            0xc21094364dfb5636, 0x985915fc12f542e4,
            0xf294b943e17a2bc4, 0x3e6f5b7b17b2939d,
            0x979cf3ca6cec5b5a, 0xa705992ceecf9c42,
            0xbd8430bd08277231, 0x50c6ff782a838353,
            0xece53cec4a314ebd, 0xa4f8bf5635246428,
            0x940f4613ae5ed136, 0x871b7795e136be99,
            0xb913179899f68584, 0x28e2557b59846e3f,
            0xe757dd7ec07426e5, 0x331aeada2fe589cf,
            0x9096ea6f3848984f, 0x3ff0d2c85def7621,
            0xb4bca50b065abe63, 0xfed077a756b53a9,
            0xe1ebce4dc7f16dfb, 0xd3e8495912c62894,
            0x8d3360f09cf6e4bd, 0x64712dd7abbbd95c,
            0xb080392cc4349dec, 0xbd8d794d96aacfb3,
            0xdca04777f541c567, 0xecf0d7a0fc5583a0,
            0x89e42caaf9491b60, 0xf41686c49db57244,
            0xac5d37d5b79b6239, 0x311c2875c522ced5,
            0xd77485cb25823ac7, 0x7d633293366b828b,
            0x86a8d39ef77164bc, 0xae5dff9c02033197,
            0xa8530886b54dbdeb, 0xd9f57f830283fdfc,
            0xd267caa862a12d66, 0xd072df63c324fd7b,
            0x8380dea93da4bc60, 0x4247cb9e59f71e6d,
            0xa46116538d0deb78, 0x52d9be85f074e608,
            0xcd795be870516656, 0x67902e276c921f8b,
            0x806bd9714632dff6, 0xba1cd8a3db53b6,
            0xa086cfcd97bf97f3, 0x80e8a40eccd228a4,
            0xc8a883c0fdaf7df0, 0x6122cd128006b2cd,
            0xfad2a4b13d1b5d6c, 0x796b805720085f81,
            0x9cc3a6eec6311a63, 0xcbe3303674053bb0,
            0xc3f490aa77bd60fc, 0xbedbfc4411068a9c,
            0xf4f1b4d515acb93b, 0xee92fb5515482d44,
            0x991711052d8bf3c5, 0x751bdd152d4d1c4a,
            0xbf5cd54678eef0b6, 0xd262d45a78a0635d,
            0xef340a98172aace4, 0x86fb897116c87c34,
            0x9580869f0e7aac0e, 0xd45d35e6ae3d4da0,
            0xbae0a846d2195712, 0x8974836059cca109,
            0xe998d258869facd7, 0x2bd1a438703fc94b,
            0x91ff83775423cc06, 0x7b6306a34627ddcf,
            0xb67f6455292cbf08, 0x1a3bc84c17b1d542,
            0xe41f3d6a7377eeca, 0x20caba5f1d9e4a93,
            0x8e938662882af53e, 0x547eb47b7282ee9c,
            0xb23867fb2a35b28d, 0xe99e619a4f23aa43,
            0xdec681f9f4c31f31, 0x6405fa00e2ec94d4,
            0x8b3c113c38f9f37e, 0xde83bc408dd3dd04,
            0xae0b158b4738705e, 0x9624ab50b148d445,
            0xd98ddaee19068c76, 0x3badd624dd9b0957,
            0x87f8a8d4cfa417c9, 0xe54ca5d70a80e5d6,
            0xa9f6d30a038d1dbc, 0x5e9fcf4ccd211f4c,
            0xd47487cc8470652b, 0x7647c3200069671f,
            0x84c8d4dfd2c63f3b, 0x29ecd9f40041e073,
            0xa5fb0a17c777cf09, 0xf468107100525890,
            0xcf79cc9db955c2cc, 0x7182148d4066eeb4,
            0x81ac1fe293d599bf, 0xc6f14cd848405530,
            0xa21727db38cb002f, 0xb8ada00e5a506a7c,
            0xca9cf1d206fdc03b, 0xa6d90811f0e4851c,
            0xfd442e4688bd304a, 0x908f4a166d1da663,
            0x9e4a9cec15763e2e, 0x9a598e4e043287fe,
            0xc5dd44271ad3cdba, 0x40eff1e1853f29fd,
            0xf7549530e188c128, 0xd12bee59e68ef47c,
            0x9a94dd3e8cf578b9, 0x82bb74f8301958ce,
            0xc13a148e3032d6e7, 0xe36a52363c1faf01,
            0xf18899b1bc3f8ca1, 0xdc44e6c3cb279ac1,
            0x96f5600f15a7b7e5, 0x29ab103a5ef8c0b9,
            0xbcb2b812db11a5de, 0x7415d448f6b6f0e7,
            0xebdf661791d60f56, 0x111b495b3464ad21,
            0x936b9fcebb25c995, 0xcab10dd900beec34,
            0xb84687c269ef3bfb, 0x3d5d514f40eea742,
            0xe65829b3046b0afa, 0xcb4a5a3112a5112,
            0x8ff71a0fe2c2e6dc, 0x47f0e785eaba72ab,
            0xb3f4e093db73a093, 0x59ed216765690f56,
            0xe0f218b8d25088b8, 0x306869c13ec3532c,
            0x8c974f7383725573, 0x1e414218c73a13fb,
            0xafbd2350644eeacf, 0xe5d1929ef90898fa,
            0xdbac6c247d62a583, 0xdf45f746b74abf39,
            0x894bc396ce5da772, 0x6b8bba8c328eb783,
            0xab9eb47c81f5114f, 0x66ea92f3f326564,
            0xd686619ba27255a2, 0xc80a537b0efefebd,
            0x8613fd0145877585, 0xbd06742ce95f5f36,
            0xa798fc4196e952e7, 0x2c48113823b73704,
            0xd17f3b51fca3a7a0, 0xf75a15862ca504c5,
            0x82ef85133de648c4, 0x9a984d73dbe722fb,
            0xa3ab66580d5fdaf5, 0xc13e60d0d2e0ebba,
            0xcc963fee10b7d1b3, 0x318df905079926a8,
            0xffbbcfe994e5c61f, 0xfdf17746497f7052,
            0x9fd561f1fd0f9bd3, 0xfeb6ea8bedefa633,
            0xc7caba6e7c5382c8, 0xfe64a52ee96b8fc0,
            0xf9bd690a1b68637b, 0x3dfdce7aa3c673b0,
            0x9c1661a651213e2d, 0x6bea10ca65c084e,
            0xc31bfa0fe5698db8, 0x486e494fcff30a62,
            0xf3e2f893dec3f126, 0x5a89dba3c3efccfa,
            0x986ddb5c6b3a76b7, 0xf89629465a75e01c,
            0xbe89523386091465, 0xf6bbb397f1135823,
            0xee2ba6c0678b597f, 0x746aa07ded582e2c,
            0x94db483840b717ef, 0xa8c2a44eb4571cdc,
            0xba121a4650e4ddeb, 0x92f34d62616ce413,
            0xe896a0d7e51e1566, 0x77b020baf9c81d17,
            0x915e2486ef32cd60, 0xace1474dc1d122e,
            0xb5b5ada8aaff80b8, 0xd819992132456ba,
            0xe3231912d5bf60e6, 0x10e1fff697ed6c69,
            0x8df5efabc5979c8f, 0xca8d3ffa1ef463c1,
            0xb1736b96b6fd83b3, 0xbd308ff8a6b17cb2,
            0xddd0467c64bce4a0, 0xac7cb3f6d05ddbde,
            0x8aa22c0dbef60ee4, 0x6bcdf07a423aa96b,
            0xad4ab7112eb3929d, 0x86c16c98d2c953c6,
            0xd89d64d57a607744, 0xe871c7bf077ba8b7,
            0x87625f056c7c4a8b, 0x11471cd764ad4972,
            0xa93af6c6c79b5d2d, 0xd598e40d3dd89bcf,
            0xd389b47879823479, 0x4aff1d108d4ec2c3,
            0x843610cb4bf160cb, 0xcedf722a585139ba,
            0xa54394fe1eedb8fe, 0xc2974eb4ee658828,
            0xce947a3da6a9273e, 0x733d226229feea32,
            0x811ccc668829b887, 0x806357d5a3f525f,
            0xa163ff802a3426a8, 0xca07c2dcb0cf26f7,
            0xc9bcff6034c13052, 0xfc89b393dd02f0b5,
            0xfc2c3f3841f17c67, 0xbbac2078d443ace2,
            0x9d9ba7832936edc0, 0xd54b944b84aa4c0d,
            0xc5029163f384a931, 0xa9e795e65d4df11,
            0xf64335bcf065d37d, 0x4d4617b5ff4a16d5,
            0x99ea0196163fa42e, 0x504bced1bf8e4e45,
            0xc06481fb9bcf8d39, 0xe45ec2862f71e1d6,
            0xf07da27a82c37088, 0x5d767327bb4e5a4c,
            0x964e858c91ba2655, 0x3a6a07f8d510f86f,
            0xbbe226efb628afea, 0x890489f70a55368b,
            0xeadab0aba3b2dbe5, 0x2b45ac74ccea842e,
            0x92c8ae6b464fc96f, 0x3b0b8bc90012929d,
            0xb77ada0617e3bbcb, 0x9ce6ebb40173744,
            0xe55990879ddcaabd, 0xcc420a6a101d0515,
            0x8f57fa54c2a9eab6, 0x9fa946824a12232d,
            0xb32df8e9f3546564, 0x47939822dc96abf9,
            0xdff9772470297ebd, 0x59787e2b93bc56f7,
            0x8bfbea76c619ef36, 0x57eb4edb3c55b65a,
            0xaefae51477a06b03, 0xede622920b6b23f1,
            0xdab99e59958885c4, 0xe95fab368e45eced,
            0x88b402f7fd75539b, 0x11dbcb0218ebb414,
            0xaae103b5fcd2a881, 0xd652bdc29f26a119,
            0xd59944a37c0752a2, 0x4be76d3346f0495f,
            0x857fcae62d8493a5, 0x6f70a4400c562ddb,
            0xa6dfbd9fb8e5b88e, 0xcb4ccd500f6bb952,
            0xd097ad07a71f26b2, 0x7e2000a41346a7a7,
            0x825ecc24c873782f, 0x8ed400668c0c28c8,
            0xa2f67f2dfa90563b, 0x728900802f0f32fa,
            0xcbb41ef979346bca, 0x4f2b40a03ad2ffb9,
            0xfea126b7d78186bc, 0xe2f610c84987bfa8,
            0x9f24b832e6b0f436, 0xdd9ca7d2df4d7c9,
            0xc6ede63fa05d3143, 0x91503d1c79720dbb,
            0xf8a95fcf88747d94, 0x75a44c6397ce912a,
            0x9b69dbe1b548ce7c, 0xc986afbe3ee11aba,
            0xc24452da229b021b, 0xfbe85badce996168,
            0xf2d56790ab41c2a2, 0xfae27299423fb9c3,
            0x97c560ba6b0919a5, 0xdccd879fc967d41a,
            0xbdb6b8e905cb600f, 0x5400e987bbc1c920,
            0xed246723473e3813, 0x290123e9aab23b68,
            0x9436c0760c86e30b, 0xf9a0b6720aaf6521,
            0xb94470938fa89bce, 0xf808e40e8d5b3e69,
            0xe7958cb87392c2c2, 0xb60b1d1230b20e04,
            0x90bd77f3483bb9b9, 0xb1c6f22b5e6f48c2,
            0xb4ecd5f01a4aa828, 0x1e38aeb6360b1af3,
            0xe2280b6c20dd5232, 0x25c6da63c38de1b0,
            0x8d590723948a535f, 0x579c487e5a38ad0e,
            0xb0af48ec79ace837, 0x2d835a9df0c6d851,
            0xdcdb1b2798182244, 0xf8e431456cf88e65,
            0x8a08f0f8bf0f156b, 0x1b8e9ecb641b58ff,
            0xac8b2d36eed2dac5, 0xe272467e3d222f3f,
            0xd7adf884aa879177, 0x5b0ed81dcc6abb0f,
            0x86ccbb52ea94baea, 0x98e947129fc2b4e9,
            0xa87fea27a539e9a5, 0x3f2398d747b36224,
            0xd29fe4b18e88640e, 0x8eec7f0d19a03aad,
            0x83a3eeeef9153e89, 0x1953cf68300424ac,
            0xa48ceaaab75a8e2b, 0x5fa8c3423c052dd7,
            0xcdb02555653131b6, 0x3792f412cb06794d,
            0x808e17555f3ebf11, 0xe2bbd88bbee40bd0,
            0xa0b19d2ab70e6ed6, 0x5b6aceaeae9d0ec4,
            0xc8de047564d20a8b, 0xf245825a5a445275,
            0xfb158592be068d2e, 0xeed6e2f0f0d56712,
            0x9ced737bb6c4183d, 0x55464dd69685606b,
            0xc428d05aa4751e4c, 0xaa97e14c3c26b886,
            0xf53304714d9265df, 0xd53dd99f4b3066a8,
            0x993fe2c6d07b7fab, 0xe546a8038efe4029,
            0xbf8fdb78849a5f96, 0xde98520472bdd033,
            0xef73d256a5c0f77c, 0x963e66858f6d4440,
            0x95a8637627989aad, 0xdde7001379a44aa8,
            0xbb127c53b17ec159, 0x5560c018580d5d52,
            0xe9d71b689dde71af, 0xaab8f01e6e10b4a6,
            0x9226712162ab070d, 0xcab3961304ca70e8,
            0xb6b00d69bb55c8d1, 0x3d607b97c5fd0d22,
            0xe45c10c42a2b3b05, 0x8cb89a7db77c506a,
            0x8eb98a7a9a5b04e3, 0x77f3608e92adb242,
            0xb267ed1940f1c61c, 0x55f038b237591ed3,
            0xdf01e85f912e37a3, 0x6b6c46dec52f6688,
            0x8b61313bbabce2c6, 0x2323ac4b3b3da015,
            0xae397d8aa96c1b77, 0xabec975e0a0d081a,
            0xd9c7dced53c72255, 0x96e7bd358c904a21,
            0x881cea14545c7575, 0x7e50d64177da2e54,
            0xaa242499697392d2, 0xdde50bd1d5d0b9e9,
            0xd4ad2dbfc3d07787, 0x955e4ec64b44e864,
            0x84ec3c97da624ab4, 0xbd5af13bef0b113e,
            0xa6274bbdd0fadd61, 0xecb1ad8aeacdd58e,
            0xcfb11ead453994ba, 0x67de18eda5814af2,
            0x81ceb32c4b43fcf4, 0x80eacf948770ced7,
            0xa2425ff75e14fc31, 0xa1258379a94d028d,
            0xcad2f7f5359a3b3e, 0x96ee45813a04330,
            0xfd87b5f28300ca0d, 0x8bca9d6e188853fc,
            0x9e74d1b791e07e48, 0x775ea264cf55347e,
            0xc612062576589dda, 0x95364afe032a819e,
            0xf79687aed3eec551, 0x3a83ddbd83f52205,
            0x9abe14cd44753b52, 0xc4926a9672793543,
            0xc16d9a0095928a27, 0x75b7053c0f178294,
            0xf1c90080baf72cb1, 0x5324c68b12dd6339,
            0x971da05074da7bee, 0xd3f6fc16ebca5e04,
            0xbce5086492111aea, 0x88f4bb1ca6bcf585,
            0xec1e4a7db69561a5, 0x2b31e9e3d06c32e6,
            0x9392ee8e921d5d07, 0x3aff322e62439fd0,
            0xb877aa3236a4b449, 0x9befeb9fad487c3,
            0xe69594bec44de15b, 0x4c2ebe687989a9b4,
            0x901d7cf73ab0acd9, 0xf9d37014bf60a11,
            0xb424dc35095cd80f, 0x538484c19ef38c95,
            0xe12e13424bb40e13, 0x2865a5f206b06fba,
            0x8cbccc096f5088cb, 0xf93f87b7442e45d4,
            0xafebff0bcb24aafe, 0xf78f69a51539d749,
            0xdbe6fecebdedd5be, 0xb573440e5a884d1c,
            0x89705f4136b4a597, 0x31680a88f8953031,
            0xabcc77118461cefc, 0xfdc20d2b36ba7c3e,
            0xd6bf94d5e57a42bc, 0x3d32907604691b4d,
            0x8637bd05af6c69b5, 0xa63f9a49c2c1b110,
            0xa7c5ac471b478423, 0xfcf80dc33721d54,
            0xd1b71758e219652b, 0xd3c36113404ea4a9,
            0x83126e978d4fdf3b, 0x645a1cac083126ea,
            0xa3d70a3d70a3d70a, 0x3d70a3d70a3d70a4,
            0xcccccccccccccccc, 0xcccccccccccccccd,
            0x8000000000000000, 0x0,
            0xa000000000000000, 0x0,
            0xc800000000000000, 0x0,
            0xfa00000000000000, 0x0,
            0x9c40000000000000, 0x0,
            0xc350000000000000, 0x0,
            0xf424000000000000, 0x0,
            0x9896800000000000, 0x0,
            0xbebc200000000000, 0x0,
            0xee6b280000000000, 0x0,
            0x9502f90000000000, 0x0,
            0xba43b74000000000, 0x0,
            0xe8d4a51000000000, 0x0,
            0x9184e72a00000000, 0x0,
            0xb5e620f480000000, 0x0,
            0xe35fa931a0000000, 0x0,
            0x8e1bc9bf04000000, 0x0,
            0xb1a2bc2ec5000000, 0x0,
            0xde0b6b3a76400000, 0x0,
            0x8ac7230489e80000, 0x0,
            0xad78ebc5ac620000, 0x0,
            0xd8d726b7177a8000, 0x0,
            0x878678326eac9000, 0x0,
            0xa968163f0a57b400, 0x0,
            0xd3c21bcecceda100, 0x0,
            0x84595161401484a0, 0x0,
            0xa56fa5b99019a5c8, 0x0,
            0xcecb8f27f4200f3a, 0x0,
            0x813f3978f8940984, 0x4000000000000000,
            0xa18f07d736b90be5, 0x5000000000000000,
            0xc9f2c9cd04674ede, 0xa400000000000000,
            0xfc6f7c4045812296, 0x4d00000000000000,
            0x9dc5ada82b70b59d, 0xf020000000000000,
            0xc5371912364ce305, 0x6c28000000000000,
            0xf684df56c3e01bc6, 0xc732000000000000,
            0x9a130b963a6c115c, 0x3c7f400000000000,
            0xc097ce7bc90715b3, 0x4b9f100000000000,
            0xf0bdc21abb48db20, 0x1e86d40000000000,
            0x96769950b50d88f4, 0x1314448000000000,
            0xbc143fa4e250eb31, 0x17d955a000000000,
            0xeb194f8e1ae525fd, 0x5dcfab0800000000,
            0x92efd1b8d0cf37be, 0x5aa1cae500000000,
            0xb7abc627050305ad, 0xf14a3d9e40000000,
            0xe596b7b0c643c719, 0x6d9ccd05d0000000,
            0x8f7e32ce7bea5c6f, 0xe4820023a2000000,
            0xb35dbf821ae4f38b, 0xdda2802c8a800000,
            0xe0352f62a19e306e, 0xd50b2037ad200000,
            0x8c213d9da502de45, 0x4526f422cc340000,
            0xaf298d050e4395d6, 0x9670b12b7f410000,
            0xdaf3f04651d47b4c, 0x3c0cdd765f114000,
            0x88d8762bf324cd0f, 0xa5880a69fb6ac800,
            0xab0e93b6efee0053, 0x8eea0d047a457a00,
            0xd5d238a4abe98068, 0x72a4904598d6d880,
            0x85a36366eb71f041, 0x47a6da2b7f864750,
            0xa70c3c40a64e6c51, 0x999090b65f67d924,
            0xd0cf4b50cfe20765, 0xfff4b4e3f741cf6d,
            0x82818f1281ed449f, 0xbff8f10e7a8921a4,
            0xa321f2d7226895c7, 0xaff72d52192b6a0d,
            0xcbea6f8ceb02bb39, 0x9bf4f8a69f764490,
            0xfee50b7025c36a08, 0x2f236d04753d5b4,
            0x9f4f2726179a2245, 0x1d762422c946590,
            0xc722f0ef9d80aad6, 0x424d3ad2b7b97ef5,
            0xf8ebad2b84e0d58b, 0xd2e0898765a7deb2,
            0x9b934c3b330c8577, 0x63cc55f49f88eb2f,
            0xc2781f49ffcfa6d5, 0x3cbf6b71c76b25fb,
            0xf316271c7fc3908a, 0x8bef464e3945ef7a,
            0x97edd871cfda3a56, 0x97758bf0e3cbb5ac,
            0xbde94e8e43d0c8ec, 0x3d52eeed1cbea317,
            0xed63a231d4c4fb27, 0x4ca7aaa863ee4bdd,
            0x945e455f24fb1cf8, 0x8fe8caa93e74ef6a,
            0xb975d6b6ee39e436, 0xb3e2fd538e122b44,
            0xe7d34c64a9c85d44, 0x60dbbca87196b616,
            0x90e40fbeea1d3a4a, 0xbc8955e946fe31cd,
            0xb51d13aea4a488dd, 0x6babab6398bdbe41,
            0xe264589a4dcdab14, 0xc696963c7eed2dd1,
            0x8d7eb76070a08aec, 0xfc1e1de5cf543ca2,
            0xb0de65388cc8ada8, 0x3b25a55f43294bcb,
            0xdd15fe86affad912, 0x49ef0eb713f39ebe,
            0x8a2dbf142dfcc7ab, 0x6e3569326c784337,
            0xacb92ed9397bf996, 0x49c2c37f07965404,
            0xd7e77a8f87daf7fb, 0xdc33745ec97be906,
            0x86f0ac99b4e8dafd, 0x69a028bb3ded71a3,
            0xa8acd7c0222311bc, 0xc40832ea0d68ce0c,
            0xd2d80db02aabd62b, 0xf50a3fa490c30190,
            0x83c7088e1aab65db, 0x792667c6da79e0fa,
            0xa4b8cab1a1563f52, 0x577001b891185938,
            0xcde6fd5e09abcf26, 0xed4c0226b55e6f86,
            0x80b05e5ac60b6178, 0x544f8158315b05b4,
            0xa0dc75f1778e39d6, 0x696361ae3db1c721,
            0xc913936dd571c84c, 0x3bc3a19cd1e38e9,
            0xfb5878494ace3a5f, 0x4ab48a04065c723,
            0x9d174b2dcec0e47b, 0x62eb0d64283f9c76,
            0xc45d1df942711d9a, 0x3ba5d0bd324f8394,
            0xf5746577930d6500, 0xca8f44ec7ee36479,
            0x9968bf6abbe85f20, 0x7e998b13cf4e1ecb,
            0xbfc2ef456ae276e8, 0x9e3fedd8c321a67e,
            0xefb3ab16c59b14a2, 0xc5cfe94ef3ea101e,
            0x95d04aee3b80ece5, 0xbba1f1d158724a12,
            0xbb445da9ca61281f, 0x2a8a6e45ae8edc97,
            0xea1575143cf97226, 0xf52d09d71a3293bd,
            0x924d692ca61be758, 0x593c2626705f9c56,
            0xb6e0c377cfa2e12e, 0x6f8b2fb00c77836c,
            0xe498f455c38b997a, 0xb6dfb9c0f956447,
            0x8edf98b59a373fec, 0x4724bd4189bd5eac,
            0xb2977ee300c50fe7, 0x58edec91ec2cb657,
            0xdf3d5e9bc0f653e1, 0x2f2967b66737e3ed,
            0x8b865b215899f46c, 0xbd79e0d20082ee74,
            0xae67f1e9aec07187, 0xecd8590680a3aa11,
            0xda01ee641a708de9, 0xe80e6f4820cc9495,
            0x884134fe908658b2, 0x3109058d147fdcdd,
            0xaa51823e34a7eede, 0xbd4b46f0599fd415,
            0xd4e5e2cdc1d1ea96, 0x6c9e18ac7007c91a,
            0x850fadc09923329e, 0x3e2cf6bc604ddb0,
            0xa6539930bf6bff45, 0x84db8346b786151c,
            0xcfe87f7cef46ff16, 0xe612641865679a63,
            0x81f14fae158c5f6e, 0x4fcb7e8f3f60c07e,
            0xa26da3999aef7749, 0xe3be5e330f38f09d,
            0xcb090c8001ab551c, 0x5cadf5bfd3072cc5,
            0xfdcb4fa002162a63, 0x73d9732fc7c8f7f6,
            0x9e9f11c4014dda7e, 0x2867e7fddcdd9afa,
            0xc646d63501a1511d, 0xb281e1fd541501b8,
            0xf7d88bc24209a565, 0x1f225a7ca91a4226,
            0x9ae757596946075f, 0x3375788de9b06958,
            0xc1a12d2fc3978937, 0x52d6b1641c83ae,
            0xf209787bb47d6b84, 0xc0678c5dbd23a49a,
            0x9745eb4d50ce6332, 0xf840b7ba963646e0,
            0xbd176620a501fbff, 0xb650e5a93bc3d898,
            0xec5d3fa8ce427aff, 0xa3e51f138ab4cebe,
            0x93ba47c980e98cdf, 0xc66f336c36b10137,
            0xb8a8d9bbe123f017, 0xb80b0047445d4184,
            0xe6d3102ad96cec1d, 0xa60dc059157491e5,
            0x9043ea1ac7e41392, 0x87c89837ad68db2f,
            0xb454e4a179dd1877, 0x29babe4598c311fb,
            0xe16a1dc9d8545e94, 0xf4296dd6fef3d67a,
            0x8ce2529e2734bb1d, 0x1899e4a65f58660c,
            0xb01ae745b101e9e4, 0x5ec05dcff72e7f8f,
            0xdc21a1171d42645d, 0x76707543f4fa1f73,
            0x899504ae72497eba, 0x6a06494a791c53a8,
            0xabfa45da0edbde69, 0x487db9d17636892,
            0xd6f8d7509292d603, 0x45a9d2845d3c42b6,
            0x865b86925b9bc5c2, 0xb8a2392ba45a9b2,
            0xa7f26836f282b732, 0x8e6cac7768d7141e,
            0xd1ef0244af2364ff, 0x3207d795430cd926,
            0x8335616aed761f1f, 0x7f44e6bd49e807b8,
            0xa402b9c5a8d3a6e7, 0x5f16206c9c6209a6,
            0xcd036837130890a1, 0x36dba887c37a8c0f,
            0x802221226be55a64, 0xc2494954da2c9789,
            0xa02aa96b06deb0fd, 0xf2db9baa10b7bd6c,
            0xc83553c5c8965d3d, 0x6f92829494e5acc7,
            0xfa42a8b73abbf48c, 0xcb772339ba1f17f9,
            0x9c69a97284b578d7, 0xff2a760414536efb,
            0xc38413cf25e2d70d, 0xfef5138519684aba,
            0xf46518c2ef5b8cd1, 0x7eb258665fc25d69,
            0x98bf2f79d5993802, 0xef2f773ffbd97a61,
            0xbeeefb584aff8603, 0xaafb550ffacfd8fa,
            0xeeaaba2e5dbf6784, 0x95ba2a53f983cf38,
            0x952ab45cfa97a0b2, 0xdd945a747bf26183,
            0xba756174393d88df, 0x94f971119aeef9e4,
            0xe912b9d1478ceb17, 0x7a37cd5601aab85d,
            0x91abb422ccb812ee, 0xac62e055c10ab33a,
            0xb616a12b7fe617aa, 0x577b986b314d6009,
            0xe39c49765fdf9d94, 0xed5a7e85fda0b80b,
            0x8e41ade9fbebc27d, 0x14588f13be847307,
            0xb1d219647ae6b31c, 0x596eb2d8ae258fc8,
            0xde469fbd99a05fe3, 0x6fca5f8ed9aef3bb,
            0x8aec23d680043bee, 0x25de7bb9480d5854,
            0xada72ccc20054ae9, 0xaf561aa79a10ae6a,
            0xd910f7ff28069da4, 0x1b2ba1518094da04,
            0x87aa9aff79042286, 0x90fb44d2f05d0842,
            0xa99541bf57452b28, 0x353a1607ac744a53,
            0xd3fa922f2d1675f2, 0x42889b8997915ce8,
            0x847c9b5d7c2e09b7, 0x69956135febada11,
            0xa59bc234db398c25, 0x43fab9837e699095,
            0xcf02b2c21207ef2e, 0x94f967e45e03f4bb,
            0x8161afb94b44f57d, 0x1d1be0eebac278f5,
            0xa1ba1ba79e1632dc, 0x6462d92a69731732,
            0xca28a291859bbf93, 0x7d7b8f7503cfdcfe,
            0xfcb2cb35e702af78, 0x5cda735244c3d43e,
            0x9defbf01b061adab, 0x3a0888136afa64a7,
            0xc56baec21c7a1916, 0x88aaa1845b8fdd0,
            0xf6c69a72a3989f5b, 0x8aad549e57273d45,
            0x9a3c2087a63f6399, 0x36ac54e2f678864b,
            0xc0cb28a98fcf3c7f, 0x84576a1bb416a7dd,
            0xf0fdf2d3f3c30b9f, 0x656d44a2a11c51d5,
            0x969eb7c47859e743, 0x9f644ae5a4b1b325,
            0xbc4665b596706114, 0x873d5d9f0dde1fee,
            0xeb57ff22fc0c7959, 0xa90cb506d155a7ea,
            0x9316ff75dd87cbd8, 0x9a7f12442d588f2,
            0xb7dcbf5354e9bece, 0xc11ed6d538aeb2f,
            0xe5d3ef282a242e81, 0x8f1668c8a86da5fa,
            0x8fa475791a569d10, 0xf96e017d694487bc,
            0xb38d92d760ec4455, 0x37c981dcc395a9ac,
            0xe070f78d3927556a, 0x85bbe253f47b1417,
            0x8c469ab843b89562, 0x93956d7478ccec8e,
            0xaf58416654a6babb, 0x387ac8d1970027b2,
            0xdb2e51bfe9d0696a, 0x6997b05fcc0319e,
            0x88fcf317f22241e2, 0x441fece3bdf81f03,
            0xab3c2fddeeaad25a, 0xd527e81cad7626c3,
            0xd60b3bd56a5586f1, 0x8a71e223d8d3b074,
            0x85c7056562757456, 0xf6872d5667844e49,
            0xa738c6bebb12d16c, 0xb428f8ac016561db,
            0xd106f86e69d785c7, 0xe13336d701beba52,
            0x82a45b450226b39c, 0xecc0024661173473,
            0xa34d721642b06084, 0x27f002d7f95d0190,
            0xcc20ce9bd35c78a5, 0x31ec038df7b441f4,
            0xff290242c83396ce, 0x7e67047175a15271,
            0x9f79a169bd203e41, 0xf0062c6e984d386,
            0xc75809c42c684dd1, 0x52c07b78a3e60868,
            0xf92e0c3537826145, 0xa7709a56ccdf8a82,
            0x9bbcc7a142b17ccb, 0x88a66076400bb691,
            0xc2abf989935ddbfe, 0x6acff893d00ea435,
            0xf356f7ebf83552fe, 0x583f6b8c4124d43,
            0x98165af37b2153de, 0xc3727a337a8b704a,
            0xbe1bf1b059e9a8d6, 0x744f18c0592e4c5c,
            0xeda2ee1c7064130c, 0x1162def06f79df73,
            0x9485d4d1c63e8be7, 0x8addcb5645ac2ba8,
            0xb9a74a0637ce2ee1, 0x6d953e2bd7173692,
            0xe8111c87c5c1ba99, 0xc8fa8db6ccdd0437,
            0x910ab1d4db9914a0, 0x1d9c9892400a22a2,
            0xb54d5e4a127f59c8, 0x2503beb6d00cab4b,
            0xe2a0b5dc971f303a, 0x2e44ae64840fd61d,
            0x8da471a9de737e24, 0x5ceaecfed289e5d2,
            0xb10d8e1456105dad, 0x7425a83e872c5f47,
            0xdd50f1996b947518, 0xd12f124e28f77719,
            0x8a5296ffe33cc92f, 0x82bd6b70d99aaa6f,
            0xace73cbfdc0bfb7b, 0x636cc64d1001550b,
            0xd8210befd30efa5a, 0x3c47f7e05401aa4e,
            0x8714a775e3e95c78, 0x65acfaec34810a71,
            0xa8d9d1535ce3b396, 0x7f1839a741a14d0d,
            0xd31045a8341ca07c, 0x1ede48111209a050,
            0x83ea2b892091e44d, 0x934aed0aab460432,
            0xa4e4b66b68b65d60, 0xf81da84d5617853f,
            0xce1de40642e3f4b9, 0x36251260ab9d668e,
            0x80d2ae83e9ce78f3, 0xc1d72b7c6b426019,
            0xa1075a24e4421730, 0xb24cf65b8612f81f,
            0xc94930ae1d529cfc, 0xdee033f26797b627,
            0xfb9b7cd9a4a7443c, 0x169840ef017da3b1,
            0x9d412e0806e88aa5, 0x8e1f289560ee864e,
            0xc491798a08a2ad4e, 0xf1a6f2bab92a27e2,
            0xf5b5d7ec8acb58a2, 0xae10af696774b1db,
            0x9991a6f3d6bf1765, 0xacca6da1e0a8ef29,
            0xbff610b0cc6edd3f, 0x17fd090a58d32af3,
            0xeff394dcff8a948e, 0xddfc4b4cef07f5b0,
            0x95f83d0a1fb69cd9, 0x4abdaf101564f98e,
            0xbb764c4ca7a4440f, 0x9d6d1ad41abe37f1,
            0xea53df5fd18d5513, 0x84c86189216dc5ed,
            0x92746b9be2f8552c, 0x32fd3cf5b4e49bb4,
            0xb7118682dbb66a77, 0x3fbc8c33221dc2a1,
            0xe4d5e82392a40515, 0xfabaf3feaa5334a,
            0x8f05b1163ba6832d, 0x29cb4d87f2a7400e,
            0xb2c71d5bca9023f8, 0x743e20e9ef511012,
            0xdf78e4b2bd342cf6, 0x914da9246b255416,
            0x8bab8eefb6409c1a, 0x1ad089b6c2f7548e,
            0xae9672aba3d0c320, 0xa184ac2473b529b1,
            0xda3c0f568cc4f3e8, 0xc9e5d72d90a2741e,
            0x8865899617fb1871, 0x7e2fa67c7a658892,
            0xaa7eebfb9df9de8d, 0xddbb901b98feeab7,
            0xd51ea6fa85785631, 0x552a74227f3ea565,
            0x8533285c936b35de, 0xd53a88958f87275f,
            0xa67ff273b8460356, 0x8a892abaf368f137,
            0xd01fef10a657842c, 0x2d2b7569b0432d85,
            0x8213f56a67f6b29b, 0x9c3b29620e29fc73,
            0xa298f2c501f45f42, 0x8349f3ba91b47b8f,
            0xcb3f2f7642717713, 0x241c70a936219a73,
            0xfe0efb53d30dd4d7, 0xed238cd383aa0110,
            0x9ec95d1463e8a506, 0xf4363804324a40aa,
            0xc67bb4597ce2ce48, 0xb143c6053edcd0d5,
            0xf81aa16fdc1b81da, 0xdd94b7868e94050a,
            0x9b10a4e5e9913128, 0xca7cf2b4191c8326,
            0xc1d4ce1f63f57d72, 0xfd1c2f611f63a3f0,
            0xf24a01a73cf2dccf, 0xbc633b39673c8cec,
            0x976e41088617ca01, 0xd5be0503e085d813,
            0xbd49d14aa79dbc82, 0x4b2d8644d8a74e18,
            0xec9c459d51852ba2, 0xddf8e7d60ed1219e,
            0x93e1ab8252f33b45, 0xcabb90e5c942b503,
            0xb8da1662e7b00a17, 0x3d6a751f3b936243,
            0xe7109bfba19c0c9d, 0xcc512670a783ad4,
            0x906a617d450187e2, 0x27fb2b80668b24c5,
            0xb484f9dc9641e9da, 0xb1f9f660802dedf6,
            0xe1a63853bbd26451, 0x5e7873f8a0396973,
            0x8d07e33455637eb2, 0xdb0b487b6423e1e8,
            0xb049dc016abc5e5f, 0x91ce1a9a3d2cda62,
            0xdc5c5301c56b75f7, 0x7641a140cc7810fb,
            0x89b9b3e11b6329ba, 0xa9e904c87fcb0a9d,
            0xac2820d9623bf429, 0x546345fa9fbdcd44,
            0xd732290fbacaf133, 0xa97c177947ad4095,
            0x867f59a9d4bed6c0, 0x49ed8eabcccc485d,
            0xa81f301449ee8c70, 0x5c68f256bfff5a74,
            0xd226fc195c6a2f8c, 0x73832eec6fff3111,
            0x83585d8fd9c25db7, 0xc831fd53c5ff7eab,
            0xa42e74f3d032f525, 0xba3e7ca8b77f5e55,
            0xcd3a1230c43fb26f, 0x28ce1bd2e55f35eb,
            0x80444b5e7aa7cf85, 0x7980d163cf5b81b3,
            0xa0555e361951c366, 0xd7e105bcc332621f,
            0xc86ab5c39fa63440, 0x8dd9472bf3fefaa7,
            0xfa856334878fc150, 0xb14f98f6f0feb951,
            0x9c935e00d4b9d8d2, 0x6ed1bf9a569f33d3,
            0xc3b8358109e84f07, 0xa862f80ec4700c8,
            0xf4a642e14c6262c8, 0xcd27bb612758c0fa,
            0x98e7e9cccfbd7dbd, 0x8038d51cb897789c,
            0xbf21e44003acdd2c, 0xe0470a63e6bd56c3,
            0xeeea5d5004981478, 0x1858ccfce06cac74,
            0x95527a5202df0ccb, 0xf37801e0c43ebc8,
            0xbaa718e68396cffd, 0xd30560258f54e6ba,
            0xe950df20247c83fd, 0x47c6b82ef32a2069,
            0x91d28b7416cdd27e, 0x4cdc331d57fa5441,
            0xb6472e511c81471d, 0xe0133fe4adf8e952,
            0xe3d8f9e563a198e5, 0x58180fddd97723a6,
            0x8e679c2f5e44ff8f, 0x570f09eaa7ea7648
        ];

        private static void AccumulateDecimalDigitsIntoBigInteger(scoped ref NumberBuffer number, uint firstIndex, uint lastIndex, out BigInteger result)
        {
            BigInteger.SetZero(out result);

            byte* src = number.DigitsPtr + firstIndex;
            uint remaining = lastIndex - firstIndex;

            while (remaining != 0)
            {
                uint count = Math.Min(remaining, 9);
                uint value = DigitsToUInt32(src, (int)(count));

                result.MultiplyPow10(count);
                result.Add(value);

                src += count;
                remaining -= count;
            }
        }

        private static ulong AssembleFloatingPointBits<TFloat>(ulong initialMantissa, int initialExponent, bool hasZeroTail)
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
        {
            // number of bits by which we must adjust the mantissa to shift it into the
            // correct position, and compute the resulting base two exponent for the
            // normalized mantissa:
            uint initialMantissaBits = BigInteger.CountSignificantBits(initialMantissa);
            int normalMantissaShift = TFloat.NormalMantissaBits - (int)(initialMantissaBits);
            int normalExponent = initialExponent - normalMantissaShift;

            ulong mantissa = initialMantissa;
            int exponent = normalExponent;

            if (normalExponent > TFloat.MaxBinaryExponent)
            {
                // The exponent is too large to be represented by the floating point
                // type; report the overflow condition:
                return TFloat.InfinityBits;
            }
            else if (normalExponent < TFloat.MinBinaryExponent)
            {
                // The exponent is too small to be represented by the floating point
                // type as a normal value, but it may be representable as a denormal
                // value.  Compute the number of bits by which we need to shift the
                // mantissa in order to form a denormal number.  (The subtraction of
                // an extra 1 is to account for the hidden bit of the mantissa that
                // is not available for use when representing a denormal.)
                int denormalMantissaShift = normalMantissaShift + normalExponent + TFloat.ExponentBias - 1;

                // Denormal values have an exponent of zero, so the debiased exponent is
                // the negation of the exponent bias:
                exponent = -TFloat.ExponentBias;

                if (denormalMantissaShift < 0)
                {
                    // Use two steps for right shifts:  for a shift of N bits, we first
                    // shift by N-1 bits, then shift the last bit and use its value to
                    // round the mantissa.
                    mantissa = RightShiftWithRounding(mantissa, -denormalMantissaShift, hasZeroTail);

                    // If the mantissa is now zero, we have underflowed:
                    if (mantissa == 0)
                    {
                        return TFloat.ZeroBits;
                    }

                    // When we round the mantissa, the result may be so large that the
                    // number becomes a normal value.  For example, consider the single
                    // precision case where the mantissa is 0x01ffffff and a right shift
                    // of 2 is required to shift the value into position. We perform the
                    // shift in two steps:  we shift by one bit, then we shift again and
                    // round using the dropped bit.  The initial shift yields 0x00ffffff.
                    // The rounding shift then yields 0x007fffff and because the least
                    // significant bit was 1, we add 1 to this number to round it.  The
                    // final result is 0x00800000.
                    //
                    // 0x00800000 is 24 bits, which is more than the 23 bits available
                    // in the mantissa.  Thus, we have rounded our denormal number into
                    // a normal number.
                    //
                    // We detect this case here and re-adjust the mantissa and exponent
                    // appropriately, to form a normal number:
                    if (mantissa > TFloat.DenormalMantissaMask)
                    {
                        // We add one to the denormalMantissaShift to account for the
                        // hidden mantissa bit (we subtracted one to account for this bit
                        // when we computed the denormalMantissaShift above).
                        exponent = initialExponent - (denormalMantissaShift + 1) - normalMantissaShift;
                    }
                }
                else
                {
                    mantissa <<= denormalMantissaShift;
                }
            }
            else
            {
                if (normalMantissaShift < 0)
                {
                    // Use two steps for right shifts:  for a shift of N bits, we first
                    // shift by N-1 bits, then shift the last bit and use its value to
                    // round the mantissa.
                    mantissa = RightShiftWithRounding(mantissa, -normalMantissaShift, hasZeroTail);

                    // When we round the mantissa, it may produce a result that is too
                    // large.  In this case, we divide the mantissa by two and increment
                    // the exponent (this does not change the value).
                    if (mantissa > TFloat.NormalMantissaMask)
                    {
                        mantissa >>= 1;
                        exponent++;

                        // The increment of the exponent may have generated a value too
                        // large to be represented.  In this case, report the overflow:
                        if (exponent > TFloat.MaxBinaryExponent)
                        {
                            return TFloat.InfinityBits;
                        }
                    }
                }
                else if (normalMantissaShift > 0)
                {
                    mantissa <<= normalMantissaShift;
                }
            }

            // Unset the hidden bit in the mantissa and assemble the floating point value
            // from the computed components:
            mantissa &= TFloat.DenormalMantissaMask;

            Debug.Assert((TFloat.DenormalMantissaMask & (1UL << TFloat.DenormalMantissaBits)) == 0);
            ulong shiftedExponent = ((ulong)(exponent + TFloat.ExponentBias)) << TFloat.DenormalMantissaBits;
            Debug.Assert((shiftedExponent & TFloat.DenormalMantissaMask) == 0);
            Debug.Assert((mantissa & ~TFloat.DenormalMantissaMask) == 0);
            Debug.Assert((shiftedExponent & ~(((1UL << TFloat.ExponentBits) - 1) << TFloat.DenormalMantissaBits)) == 0); // exponent fits in its place

            return shiftedExponent | mantissa;
        }

        private static ulong ConvertBigIntegerToFloatingPointBits<TFloat>(ref BigInteger value, uint integerBitsOfPrecision, bool hasNonZeroFractionalPart)
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
        {
            int baseExponent = TFloat.DenormalMantissaBits;

            // When we have 64-bits or less of precision, we can just get the mantissa directly
            if (integerBitsOfPrecision <= 64)
            {
                return AssembleFloatingPointBits<TFloat>(value.ToUInt64(), baseExponent, !hasNonZeroFractionalPart);
            }

            (uint topBlockIndex, uint topBlockBits) = Math.DivRem(integerBitsOfPrecision, 32);
            uint middleBlockIndex = topBlockIndex - 1;
            uint bottomBlockIndex = middleBlockIndex - 1;

            ulong mantissa;
            int exponent = baseExponent + ((int)(bottomBlockIndex) * 32);
            bool hasZeroTail = !hasNonZeroFractionalPart;

            // When the top 64-bits perfectly span two blocks, we can get those blocks directly
            if (topBlockBits == 0)
            {
                mantissa = ((ulong)(value.GetBlock(middleBlockIndex)) << 32) + value.GetBlock(bottomBlockIndex);
            }
            else
            {
                // Otherwise, we need to read three blocks and combine them into a 64-bit mantissa

                int bottomBlockShift = (int)(topBlockBits);
                int topBlockShift = 64 - bottomBlockShift;
                int middleBlockShift = topBlockShift - 32;

                exponent += (int)(topBlockBits);

                uint bottomBlock = value.GetBlock(bottomBlockIndex);
                uint bottomBits = bottomBlock >> bottomBlockShift;

                ulong middleBits = (ulong)(value.GetBlock(middleBlockIndex)) << middleBlockShift;
                ulong topBits = (ulong)(value.GetBlock(topBlockIndex)) << topBlockShift;

                mantissa = topBits + middleBits + bottomBits;

                uint unusedBottomBlockBitsMask = (1u << (int)(topBlockBits)) - 1;
                hasZeroTail &= (bottomBlock & unusedBottomBlockBitsMask) == 0;
            }

            for (uint i = 0; i != bottomBlockIndex; i++)
            {
                hasZeroTail &= (value.GetBlock(i) == 0);
            }

            return AssembleFloatingPointBits<TFloat>(mantissa, exponent, hasZeroTail);
        }

        // get 32-bit integer from at most 9 digits
        private static uint DigitsToUInt32(byte* p, int count)
        {
            Debug.Assert((1 <= count) && (count <= 9));

            byte* end = (p + count);
            uint res = 0;

            // parse batches of 8 digits with SWAR
            while (p <= end - 8)
            {
                res = (res * 100000000) + ParseEightDigitsUnrolled(p);
                p += 8;
            }

            while (p != end)
            {
                res = (10 * res) + p[0] - '0';
                ++p;
            }

            return res;
        }

        // get 64-bit integer from at most 19 digits
        private static ulong DigitsToUInt64(byte* p, int count)
        {
            Debug.Assert((1 <= count) && (count <= 19));

            byte* end = (p + count);
            ulong res = 0;

            // parse batches of 8 digits with SWAR
            while (end - p >= 8)
            {
                res = (res * 100000000) + ParseEightDigitsUnrolled(p);
                p += 8;
            }

            while (p != end)
            {
                res = (10 * res) + p[0] - '0';
                ++p;
            }

            return res;
        }

        /// <summary>
        /// Parse eight consecutive digits using SWAR
        /// https://lemire.me/blog/2022/01/21/swar-explained-parsing-eight-digits/
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ParseEightDigitsUnrolled(byte* chars)
        {
            // let's take the following value (byte*) 12345678 and read it unaligned :
            // we get a ulong value of 0x3837363534333231
            // 1. Subtract character '0' 0x30 for each byte to get 0x0807060504030201
            // 2. Consider this sequence as bytes sequence : b8b7b6b5b4b3b2b1
            // we need to transform it to b1b2b3b4b5b6b7b8 by computing :
            // 10000 * (100 * (10*b1+b2) + 10*b3+b4) + 100*(10*b5+b6) + 10*b7+b8
            // this is achieved by masking and shifting values
            ulong val = Unsafe.ReadUnaligned<ulong>(chars);

            // With BigEndian system an endianness swap has to be performed
            // before the following operations as if it has been read with LittleEndian system
            if (!BitConverter.IsLittleEndian)
            {
                val = BinaryPrimitives.ReverseEndianness(val);
            }

            const ulong mask = 0x000000FF000000FF;
            const ulong mul1 = 0x000F424000000064; // 100 + (1000000ULL << 32)
            const ulong mul2 = 0x0000271000000001; // 1 + (10000ULL << 32)
            val -= 0x3030303030303030;
            val = (val * 10) + (val >> 8); // val = (val * 2561) >> 8;
            val = (((val & mask) * mul1) + (((val >> 16) & mask) * mul2)) >> 32;
            return (uint)val;
        }

        private static ulong NumberToFloatingPointBits<TFloat>(ref NumberBuffer number)
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
        {
            Debug.Assert(TFloat.DenormalMantissaBits <= FloatingPointMaxDenormalMantissaBits);

            Debug.Assert(number.DigitsPtr[0] != '0');

            Debug.Assert(number.Scale <= FloatingPointMaxExponent);
            Debug.Assert(number.Scale >= FloatingPointMinExponent);

            Debug.Assert(number.DigitsCount != 0);

            // The input is of the form 0.Mantissa x 10^Exponent, where 'Mantissa' are
            // the decimal digits of the mantissa and 'Exponent' is the decimal exponent.
            // We decompose the mantissa into two parts: an integer part and a fractional
            // part.  If the exponent is positive, then the integer part consists of the
            // first 'exponent' digits, or all present digits if there are fewer digits.
            // If the exponent is zero or negative, then the integer part is empty.  In
            // either case, the remaining digits form the fractional part of the mantissa.

            uint totalDigits = (uint)(number.DigitsCount);
            uint positiveExponent = (uint)(Math.Max(0, number.Scale));

            uint integerDigitsPresent = Math.Min(positiveExponent, totalDigits);
            uint fractionalDigitsPresent = totalDigits - integerDigitsPresent;

            // Above 19 digits, we rely on slow path
            if (totalDigits <= 19)
            {
                byte* src = number.DigitsPtr;

                ulong mantissa = DigitsToUInt64(src, (int)(totalDigits));

                int exponent = (int)(number.Scale - integerDigitsPresent - fractionalDigitsPresent);
                int fastExponent = Math.Abs(exponent);

                // When the number of significant digits is less than or equal to MaxMantissaFastPath and the
                // scale is less than or equal to MaxExponentFastPath, we can take some shortcuts and just rely
                // on floating-point arithmetic to compute the correct result. This is
                // because each floating-point precision values allows us to exactly represent
                // different whole integers and certain powers of 10, depending on the underlying
                // formats exact range. Additionally, IEEE operations dictate that the result is
                // computed to the infinitely precise result and then rounded, which means that
                // we can rely on it to produce the correct result when both inputs are exact.
                // This is known as Clinger's fast path

                if ((mantissa <= TFloat.MaxMantissaFastPath) && (fastExponent <= TFloat.MaxExponentFastPath))
                {
                    double mantissa_d = mantissa;
                    double scale = Pow10DoubleTable[fastExponent];

                    if (fractionalDigitsPresent != 0)
                    {
                        mantissa_d /= scale;
                    }
                    else
                    {
                        mantissa_d *= scale;
                    }

                    TFloat result = TFloat.CreateSaturating(mantissa_d);
                    return TFloat.FloatToBits(result);
                }

                // Number Parsing at a Gigabyte per Second, Software: Practice and Experience 51(8), 2021
                // https://arxiv.org/abs/2101.11408
                (int Exponent, ulong Mantissa) am = ComputeFloat<TFloat>(exponent, mantissa);

                // If we called ComputeFloat and we have an invalid power of 2 (Exponent < 0),
                // then we need to go the slow way around again. This is very uncommon.
                if (am.Exponent > 0)
                {
                    ulong word = am.Mantissa;
                    word |= (ulong)(uint)(am.Exponent) << TFloat.DenormalMantissaBits;
                    return word;

                }
            }

            return NumberToFloatingPointBitsSlow<TFloat>(ref number, positiveExponent, integerDigitsPresent, fractionalDigitsPresent);
        }

        private static ulong NumberToFloatingPointBitsSlow<TFloat>(ref NumberBuffer number, uint positiveExponent, uint integerDigitsPresent, uint fractionalDigitsPresent)
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
        {
            // To generate an N bit mantissa we require N + 1 bits of precision.  The
            // extra bit is used to correctly round the mantissa (if there are fewer bits
            // than this available, then that's totally okay; in that case we use what we
            // have and we don't need to round).
            uint requiredBitsOfPrecision = (uint)(TFloat.NormalMantissaBits + 1);

            uint totalDigits = (uint)(number.DigitsCount);
            uint integerDigitsMissing = positiveExponent - integerDigitsPresent;

            const uint IntegerFirstIndex = 0;
            uint integerLastIndex = integerDigitsPresent;

            uint fractionalFirstIndex = integerLastIndex;
            uint fractionalLastIndex = totalDigits;

            // First, we accumulate the integer part of the mantissa into a BigInteger:
            AccumulateDecimalDigitsIntoBigInteger(ref number, IntegerFirstIndex, integerLastIndex, out BigInteger integerValue);

            if (integerDigitsMissing > 0)
            {
                if (integerDigitsMissing > TFloat.OverflowDecimalExponent)
                {
                    return TFloat.InfinityBits;
                }

                integerValue.MultiplyPow10(integerDigitsMissing);
            }

            // At this point, the integerValue contains the value of the integer part
            // of the mantissa.  If either [1] this number has more than the required
            // number of bits of precision or [2] the mantissa has no fractional part,
            // then we can assemble the result immediately:
            uint integerBitsOfPrecision = BigInteger.CountSignificantBits(ref integerValue);

            if ((integerBitsOfPrecision >= requiredBitsOfPrecision) || (fractionalDigitsPresent == 0))
            {
                return ConvertBigIntegerToFloatingPointBits<TFloat>(
                    ref integerValue,
                    integerBitsOfPrecision,
                    fractionalDigitsPresent != 0
                );
            }

            // Otherwise, we did not get enough bits of precision from the integer part,
            // and the mantissa has a fractional part.  We parse the fractional part of
            // the mantissa to obtain more bits of precision.  To do this, we convert
            // the fractional part into an actual fraction N/M, where the numerator N is
            // computed from the digits of the fractional part, and the denominator M is
            // computed as the power of 10 such that N/M is equal to the value of the
            // fractional part of the mantissa.

            uint fractionalDenominatorExponent = fractionalDigitsPresent;

            if (number.Scale < 0)
            {
                fractionalDenominatorExponent += (uint)(-number.Scale);
            }

            if ((integerBitsOfPrecision == 0) && (fractionalDenominatorExponent - (int)(totalDigits)) > TFloat.OverflowDecimalExponent)
            {
                // If there were any digits in the integer part, it is impossible to
                // underflow (because the exponent cannot possibly be small enough),
                // so if we underflow here it is a true underflow and we return zero.
                return TFloat.ZeroBits;
            }

            AccumulateDecimalDigitsIntoBigInteger(ref number, fractionalFirstIndex, fractionalLastIndex, out BigInteger fractionalNumerator);

            if (fractionalNumerator.IsZero())
            {
                return ConvertBigIntegerToFloatingPointBits<TFloat>(
                    ref integerValue,
                    integerBitsOfPrecision,
                    fractionalDigitsPresent != 0
                );
            }

            BigInteger.Pow10(fractionalDenominatorExponent, out BigInteger fractionalDenominator);

            // Because we are using only the fractional part of the mantissa here, the
            // numerator is guaranteed to be smaller than the denominator.  We normalize
            // the fraction such that the most significant bit of the numerator is in
            // the same position as the most significant bit in the denominator.  This
            // ensures that when we later shift the numerator N bits to the left, we
            // will produce N bits of precision.
            uint fractionalNumeratorBits = BigInteger.CountSignificantBits(ref fractionalNumerator);
            uint fractionalDenominatorBits = BigInteger.CountSignificantBits(ref fractionalDenominator);

            uint fractionalShift = 0;

            if (fractionalDenominatorBits > fractionalNumeratorBits)
            {
                fractionalShift = fractionalDenominatorBits - fractionalNumeratorBits;
            }

            if (fractionalShift > 0)
            {
                fractionalNumerator.ShiftLeft(fractionalShift);
            }

            uint requiredFractionalBitsOfPrecision = requiredBitsOfPrecision - integerBitsOfPrecision;
            uint remainingBitsOfPrecisionRequired = requiredFractionalBitsOfPrecision;

            if (integerBitsOfPrecision > 0)
            {
                // If the fractional part of the mantissa provides no bits of precision
                // and cannot affect rounding, we can just take whatever bits we got from
                // the integer part of the mantissa.  This is the case for numbers like
                // 5.0000000000000000000001, where the significant digits of the fractional
                // part start so far to the right that they do not affect the floating
                // point representation.
                //
                // If the fractional shift is exactly equal to the number of bits of
                // precision that we require, then no fractional bits will be part of the
                // result, but the result may affect rounding.  This is e.g. the case for
                // large, odd integers with a fractional part greater than or equal to .5.
                // Thus, we need to do the division to correctly round the result.
                if (fractionalShift > remainingBitsOfPrecisionRequired)
                {
                    return ConvertBigIntegerToFloatingPointBits<TFloat>(
                        ref integerValue,
                        integerBitsOfPrecision,
                        fractionalDigitsPresent != 0
                    );
                }

                remainingBitsOfPrecisionRequired -= fractionalShift;
            }

            // If there was no integer part of the mantissa, we will need to compute the
            // exponent from the fractional part.  The fractional exponent is the power
            // of two by which we must multiply the fractional part to move it into the
            // range [1.0, 2.0).  This will either be the same as the shift we computed
            // earlier, or one greater than that shift:
            uint fractionalExponent = fractionalShift;

            if (BigInteger.Compare(ref fractionalNumerator, ref fractionalDenominator) < 0)
            {
                fractionalExponent++;
            }

            fractionalNumerator.ShiftLeft(remainingBitsOfPrecisionRequired);

            BigInteger.DivRem(ref fractionalNumerator, ref fractionalDenominator, out BigInteger bigFractionalMantissa, out BigInteger fractionalRemainder);
            ulong fractionalMantissa = bigFractionalMantissa.ToUInt64();
            bool hasZeroTail = !number.HasNonZeroTail && fractionalRemainder.IsZero();

            // We may have produced more bits of precision than were required.  Check,
            // and remove any "extra" bits:
            uint fractionalMantissaBits = BigInteger.CountSignificantBits(fractionalMantissa);

            if (fractionalMantissaBits > requiredFractionalBitsOfPrecision)
            {
                int shift = (int)(fractionalMantissaBits - requiredFractionalBitsOfPrecision);
                hasZeroTail = hasZeroTail && (fractionalMantissa & ((1UL << shift) - 1)) == 0;
                fractionalMantissa >>= shift;
            }

            // Compose the mantissa from the integer and fractional parts:
            ulong integerMantissa = integerValue.ToUInt64();
            ulong completeMantissa = (integerMantissa << (int)(requiredFractionalBitsOfPrecision)) + fractionalMantissa;

            // Compute the final exponent:
            // * If the mantissa had an integer part, then the exponent is one less than
            //   the number of bits we obtained from the integer part.  (It's one less
            //   because we are converting to the form 1.11111, with one 1 to the left
            //   of the decimal point.)
            // * If the mantissa had no integer part, then the exponent is the fractional
            //   exponent that we computed.
            // Then, in both cases, we subtract an additional one from the exponent, to
            // account for the fact that we've generated an extra bit of precision, for
            // use in rounding.
            int finalExponent = (integerBitsOfPrecision > 0) ? (int)(integerBitsOfPrecision) - 2 : -(int)(fractionalExponent) - 1;

            return AssembleFloatingPointBits<TFloat>(completeMantissa, finalExponent, hasZeroTail);
        }

        private static ulong RightShiftWithRounding(ulong value, int shift, bool hasZeroTail)
        {
            // If we'd need to shift further than it is possible to shift, the answer
            // is always zero:
            if (shift >= 64)
            {
                return 0;
            }

            ulong extraBitsMask = (1UL << (shift - 1)) - 1;
            ulong roundBitMask = (1UL << (shift - 1));
            ulong lsbBitMask = 1UL << shift;

            bool lsbBit = (value & lsbBitMask) != 0;
            bool roundBit = (value & roundBitMask) != 0;
            bool hasTailBits = !hasZeroTail || (value & extraBitsMask) != 0;

            return (value >> shift) + (ShouldRoundUp(lsbBit, roundBit, hasTailBits) ? 1UL : 0);
        }

        private static bool ShouldRoundUp(bool lsbBit, bool roundBit, bool hasTailBits)
        {
            // If there are insignificant set bits, we need to round to the
            // nearest; there are two cases:
            // we round up if either [1] the value is slightly greater than the midpoint
            // between two exactly representable values or [2] the value is exactly the
            // midpoint between two exactly representable values and the greater of the
            // two is even (this is "round-to-even").
            return roundBit && (hasTailBits || lsbBit);
        }


        /// <summary>
        /// Daniel Lemire's Fast-float algorithm please refer to https://arxiv.org/abs/2101.11408
        /// Ojective is to calculate m and p, adjusted mantissa and power of 2, based on the
        /// following equality : (m x 2^p) =  (w x 10^q)
        /// </summary>
        /// <param name="q">decimal exponent</param>
        /// <param name="w">decimal significant (mantissa)</param>
        /// <returns>Tuple : Exponent (power of 2) and adjusted mantissa </returns>
        internal static (int Exponent, ulong Mantissa) ComputeFloat<TFloat>(long q, ulong w)
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
        {
            int exponent;
            ulong mantissa = 0;

            if ((w == 0) || (q < TFloat.MinFastFloatDecimalExponent))
            {
                // result should be zero
                return default;
            }
            if (q > TFloat.MaxFastFloatDecimalExponent)
            {
                // we want to get infinity:
                exponent = TFloat.InfinityExponent;
                mantissa = 0;
                return (exponent, mantissa);
            }

            // We want the most significant bit of i to be 1. Shift if needed.
            int lz = BitOperations.LeadingZeroCount(w);
            w <<= lz;

            // The required precision is info.DenormalMantissaBits + 3 because
            // 1. We need the implicit bit
            // 2. We need an extra bit for rounding purposes
            // 3. We might lose a bit due to the "upperbit" routine (result too small, requiring a shift)

            var product = ComputeProductApproximation(TFloat.DenormalMantissaBits + 3, q, w);
            if (product.low == 0xFFFFFFFFFFFFFFFF)
            {
                // could guard it further
                // In some very rare cases, this could happen, in which case we might need a more accurate
                // computation that what we can provide cheaply. This is very, very unlikely.
                //
                bool insideSafeExponent = (q >= -27) && (q <= 55); // always good because 5**q <2**128 when q>=0,
                                                                     // and otherwise, for q<0, we have 5**-q<2**64 and the 128-bit reciprocal allows for exact computation.
                if (!insideSafeExponent)
                {
                    exponent = -1; // This (a negative value) indicates an error condition.
                    return (exponent, mantissa);
                }
            }
            // The "ComputeProductApproximation" function can be slightly slower than a branchless approach:
            // but in practice, we can win big with the ComputeProductApproximation if its additional branch
            // is easily predicted. Which is best is data specific.
            int upperBit = (int)(product.high >> 63);

            mantissa = product.high >> (upperBit + 64 - TFloat.DenormalMantissaBits - 3);

            exponent = (int)(CalculatePower((int)(q)) + upperBit - lz - (-TFloat.MaxBinaryExponent));
            if (exponent <= 0)
            {
                // we have a subnormal?
                // Here have that answer.power2 <= 0 so -answer.power2 >= 0
                if (-exponent + 1 >= 64)
                {
                    // if we have more than 64 bits below the minimum exponent, you have a zero for sure.
                    exponent = 0;
                    mantissa = 0;
                    // result should be zero
                    return (exponent, mantissa);
                }
                // next line is safe because -answer.power2 + 1 < 64
                mantissa >>= -exponent + 1;
                // Thankfully, we can't have both "round-to-even" and subnormals because
                // "round-to-even" only occurs for powers close to 0.
                mantissa += (mantissa & 1); // round up
                mantissa >>= 1;
                // There is a weird scenario where we don't have a subnormal but just
                // suppose we start with 2.2250738585072013e-308, we end up
                // with 0x3fffffffffffff x 2^-1023-53 which is technically subnormal
                // whereas 0x40000000000000 x 2^-1023-53  is normal. Now, we need to round
                // up 0x3fffffffffffff x 2^-1023-53  and once we do, we are no longer
                // subnormal, but we can only know this after rounding.
                // So we only declare a subnormal if we are smaller than the threshold.
                exponent = (mantissa < (1UL << TFloat.DenormalMantissaBits)) ? 0 : 1;
                return (exponent, mantissa);
            }

            // usually, we round *up*, but if we fall right in between and and we have an
            // even basis, we need to round down
            // We are only concerned with the cases where 5**q fits in single 64-bit word.
            if ((product.low <= 1) && (q >= TFloat.MinExponentRoundToEven) && (q <= TFloat.MaxExponentRoundToEven) &&
                ((mantissa & 3) == 1))
            {
                // We may fall between two floats!
                // To be in-between two floats we need that in doing
                // answer.mantissa = product.high >> (upperBit + 64 - info.DenormalMantissaBits  - 3);
                // ... we dropped out only zeroes. But if this happened, then we can go back!!!
                if ((mantissa << (upperBit + 64 - TFloat.DenormalMantissaBits - 3)) == product.high)
                {
                    // flip it so that we do not round up
                    mantissa &= ~1UL;
                }
            }

            mantissa += (mantissa & 1); // round up
            mantissa >>= 1;
            if (mantissa >= (2UL << TFloat.DenormalMantissaBits))
            {
                mantissa = (1UL << TFloat.DenormalMantissaBits);
                // undo previous addition
                exponent++;
            }

            mantissa &= ~(1UL << TFloat.DenormalMantissaBits);
            if (exponent >= TFloat.InfinityExponent)
            {
                // infinity
                exponent = TFloat.InfinityExponent;
                mantissa = 0;
            }
            return (exponent, mantissa);
        }
        private static (ulong high, ulong low) ComputeProductApproximation(int bitPrecision, long q, ulong w)
        {
            // -342 being the SmallestPowerOfFive
            int index = 2 * (int)(q - -342);
            // For small values of q, e.g., q in [0,27], the answer is always exact because
            // Math.BigMul gives the exact answer.
            ulong high = Math.BigMul(w, Pow5128Table[index], out ulong low);
            ulong precisionMask = (bitPrecision < 64) ? (0xFFFFFFFFFFFFFFFFUL >> bitPrecision) : 0xFFFFFFFFFFFFFFFFUL;
            if ((high & precisionMask) == precisionMask)
            {
                // could further guard with  (lower + w < lower)
                // regarding the second product, we only need secondproduct.high, but our expectation is that the compiler will optimize this extra work away if needed.
                ulong high2 = Math.BigMul(w, Pow5128Table[index + 1], out ulong _);
                low += high2;
                if (high2 > low)
                {
                    high++;
                }
            }
            return (high, low);
        }

        // For q in (0,350), we have that :
        // f = (((152170 + 65536) * q) >> 16);
        // is equal to
        //   floor(p) + q
        // where
        //   p = log(5**q)/log(2) = q* log(5)/log(2)
        //
        // For negative values of q in (-400,0), we have that
        // f = (((152170 + 65536) * q) >> 16);
        // is equal to :
        //   -ceil(p) + q
        // where
        //   p = log(5**-q)/log(2) = -q* log(5)/log(2)
        //
        internal static int CalculatePower(int q)
            => (((152170 + 65536) * q) >> 16) + 63;
    }
}
