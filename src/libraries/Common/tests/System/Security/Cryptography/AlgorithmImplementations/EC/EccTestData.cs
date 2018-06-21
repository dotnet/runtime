// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;

namespace System.Security.Cryptography.Tests
{
    // Note to contributors:
    //   Keys contained in this file should be randomly generated for the purpose of inclusion here,
    //   or obtained from some fixed set of test data. (Please) DO NOT use any key that has ever been
    //   used for any real purpose.
    //
    // Note to readers:
    //   The keys contained in this file should all be treated as compromised. That means that you
    //   absolutely SHOULD NOT use these keys on anything that you actually want to be protected.
    internal static class EccTestData
    {
        internal static readonly byte[] s_hashSha512 =
            ("a232cec7be26319e53db0d48470232d37793b06b99e8ed82fac1996b3d1596449087769927d64af657cce62d853c4cf7ff4c"
           + "d069eda230d1c524d225756ffbaf").HexToByteArray();

#if netcoreapp
        internal static ECCurve GetNistP256ExplicitCurve()
        {
            // SEC2-Ver-1.0, 2.7.2
            return new ECCurve
            {
                CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass,
                Prime = "FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF".HexToByteArray(),
                A = "FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFC".HexToByteArray(),
                B = "5AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B".HexToByteArray(),
                Seed = "C49D360886E704936A6678E1139D26B7819F7E90".HexToByteArray(),
                Hash = HashAlgorithmName.SHA1,
                G = new ECPoint
                {
                    X = "6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296".HexToByteArray(),
                    Y = "4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5".HexToByteArray(),
                },
                Order = "FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551".HexToByteArray(),
                Cofactor = new byte[] { 0x01 },
            };
        }

        internal static ECCurve GetNistP384ExplicitCurve()
        {
            // SEC2-Ver-1.0, 2.8.1
            return new ECCurve
            {
                CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass,
                Prime = ("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +
                    "FFFFFFFEFFFFFFFF0000000000000000FFFFFFFF").HexToByteArray(),
                A = ("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +
                    "FFFFFFFEFFFFFFFF0000000000000000FFFFFFFC").HexToByteArray(),
                B = ("B3312FA7E23EE7E4988E056BE3F82D19181D9C6EFE8141120314088F" +
                    "5013875AC656398D8A2ED19D2A85C8EDD3EC2AEF").HexToByteArray(),
                Seed = "A335926AA319A27A1D00896A6773A4827ACDAC73".HexToByteArray(),
                Hash = HashAlgorithmName.SHA1,
                G = new ECPoint
                {
                    X = ("AA87CA22BE8B05378EB1C71EF320AD746E1D3B628BA79B9859F741E0" +
                        "82542A385502F25DBF55296C3A545E3872760AB7").HexToByteArray(),
                    Y = ("3617DE4A96262C6F5D9E98BF9292DC29F8F41DBD289A147CE9DA3113" +
                        "B5F0B8C00A60B1CE1D7E819D7A431D7C90EA0E5F").HexToByteArray(),
                },
                Order = ("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFC7634D81" +
                    "F4372DDF581A0DB248B0A77AECEC196ACCC52973").HexToByteArray(),
                Cofactor = new byte[] { 0x01 },
            };
        }

        internal static ECCurve GetNistP521ExplicitCurve()
        {
            // SEC2-Ver-1.0, 2.9.1
            return new ECCurve
            {
                CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass,
                Prime = ("01FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +
                    "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +
                    "FFFFFFFFFFFFFFFFFFFFFFFF").HexToByteArray(),
                A = ("01FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +
                    "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +
                    "FFFFFFFFFFFFFFFFFFFFFFFC").HexToByteArray(),
                B = ("0051953EB9618E1C9A1F929A21A0B68540EEA2DA725B99B315F3" +
                    "B8B489918EF109E156193951EC7E937B1652C0BD3BB1BF073573DF88" +
                    "3D2C34F1EF451FD46B503F00").HexToByteArray(),
                Seed = "D09E8800291CB85396CC6717393284AAA0DA64BA".HexToByteArray(),
                Hash = HashAlgorithmName.SHA1,
                G = new ECPoint
                {
                    // The formatting in the document is the full binary G, with the
                    // last 2 bytes of X sharing a 4 byte clustering with the first
                    // 2 bytes of Y; hence the odd formatting.
                    X = ("00C6858E06B70404E9CD9E3ECB662395B4429C648139053FB521F828" +
                        "AF606B4D3DBAA14B5E77EFE75928FE1DC127A2FFA8DE3348B3C1856A" +
                        "429BF97E7E31C2E5" + "BD66").HexToByteArray(),
                    Y = ("0118" +
                        "39296A789A3BC0045C8A5FB42C7D1BD998F54449579B446817AFBD17" +
                        "273E662C97EE72995EF42640C550B9013FAD0761353C7086A272C240" +
                        "88BE94769FD16650").HexToByteArray(),
                },
                Order = ("01FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" +
                    "FFFFFFFFFFFFFFFA51868783BF2F966B7FCC0148F709A5D03BB5C9B8" +
                    "899C47AEBB6FB71E91386409").HexToByteArray(),
                Cofactor = new byte[] { 0x01 },
            };
        }

        internal static ECParameters GetNistP256ExplicitTestData()
        {
            // explicit values for s_Ecc256Key (nistP256)
            ECParameters p = new ECParameters();
            p.Q = new ECPoint
            {
                X = ("96E476F7473CB17C5B38684DAAE437277AE1EFADCEB380FAD3D7072BE2FFE5F0").HexToByteArray(),
                Y = ("B54A94C2D6951F073BFC25E7B81AC2A4C41317904929D167C3DFC99122175A94").HexToByteArray()
            };
            ECCurve c = p.Curve;
            c.CurveType = ECCurve.ECCurveType.PrimeShortWeierstrass;
            c.A = ("FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFC").HexToByteArray();
            c.B = ("5AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B").HexToByteArray();
            c.G = new ECPoint()
            { 
                X = ("6B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C296").HexToByteArray(),
                Y = ("4FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5").HexToByteArray(),
            };
            c.Cofactor = ("01").HexToByteArray();
            c.Order = ("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551").HexToByteArray();
            c.Prime = ("FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF").HexToByteArray();
            c.Seed = null;
            c.Hash = null;
            p.Curve = c;
            return p;
        }

        internal static ECParameters GetNistP224KeyTestData()
        {
            // key values for nistP224
            ECParameters p = new ECParameters();
            p.Q = new ECPoint
            {
                X = ("00B6C6FA7EFF084CEF007B414690027085173AAB846906C964D900C0").HexToByteArray(),
                Y = ("845213F2A07DDE66EE0B19021E62C721A0FBF41E878FB2A34C40C872").HexToByteArray()
            };
            p.D =   ("CE57F5C60F208819DC5FB994FF4EB4E11D40694B5E79564C2688B756").HexToByteArray();
            p.Curve = ECCurve.CreateFromOid(new Oid("1.3.132.0.33", "nistP224"));

            return p;
        }

        internal static ECParameters GetNistP521DiminishedCoordsParameters()
        {
            return new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,

                // Qx, Qy, and d start with 0x00, which should be preserved.
                Q = new ECPoint
                {
                    X = (
                        "00DCB499D2B8174A2A2E74F23D2EA6C5F8BC8B311574E94B7E590B1EBC28665E5A" +
                        "2C021183F10A0B23E34EC9BED2F59525CC45CFB0E6870FD61EA4FFEAFBD08CDF73").HexToByteArray(),

                    Y = (
                        "008EA45062A8CEF4A4CE10449281D98B74A7EBBA9B5597DF842A9B1FA73B46A0E7" +
                        "22C005FD49C141E43A5C10E77F1185C5233E6BE016998EF5CE09FC3936E3208B87").HexToByteArray(),
                },

                D = (
                    "0029B61CD0B8670DCFA6B2ED44677C23D134C4A802D8E2B4D6FF563BE1F010EDA7" +
                    "956FA22DD3C8682751296C129D55F8F8C15483103D99899446E13285998B7E0F05").HexToByteArray(),
            };
        }

        internal static ECParameters GetNistP521Key2()
        {
            return new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,

                Q =
                {
                    X = (
                        "00751F26324C73CEF5D45EBB32937E3060E351960A57594AD03EB1CA0C5EA747" +
                        "1213366BA991D24406717F86FDEFDE086E09B954A57DC0F5E9459756D60C2AAE" +
                        "6DE0").HexToByteArray(),

                    Y = (
                        "01A8DDBB3F02C8C23524729F548B7E23325233EF67DD30752B924F297459E3C2" +
                        "E12AE8D8F4FE4454E3847C18B255CACEF59737B918E1DE40F95E9FFE4F57B791" +
                        "53A0").HexToByteArray(),
                },

                D = (
                    "01A55F8785A5730BAEE1D6B3D301069F7FD64D8B04CCEA57EFD5961E68F33FAB" +
                    "43545166CA6553CD38FF713D1289C698BD7D086B55E01B5BD5ED27E3630376B1" +
                    "1666").HexToByteArray(),
            };
        }

        internal static ECParameters GetNistP256ReferenceKey()
        {
            // From Suite B Implementers's Guide to FIPS 186-3 (ECDSA)
            // Section D.1.1
            return new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,

                Q =
                {
                    X = "8101ECE47464A6EAD70CF69A6E2BD3D88691A3262D22CBA4F7635EAFF26680A8".HexToByteArray(),
                    Y = "D8A12BA61D599235F67D9CB4D58F1783D3CA43E78F0A5ABAA624079936C0C3A9".HexToByteArray(),
                },

                D = "70A12C2DB16845ED56FF68CFC21A472B3F04D7D6851BF6349F2D7D5B3452B38A".HexToByteArray(),
            };
        }

        internal static readonly ECParameters BrainpoolP160r1Key1 =
            new ECParameters
            {
                Curve = ECCurve.NamedCurves.brainpoolP160r1,
                Q =
                {
                    X = "8E628F0939C7629276CADBFE99C01C36509354E2".HexToByteArray(),
                    Y = "72D2C8EEF1F6CC3205D0057EB9DE6CA9E3105096".HexToByteArray(),
                },
                D = "C5D944547DE115DB2588DC6FDEBA3B473E9C4D96".HexToByteArray(),
            };

        internal static readonly ECParameters Sect283k1Key1 =
            new ECParameters
            {
                Curve = ECCurve.CreateFromValue("1.3.132.0.16"),
                Q =
                {
                    X = "0752770BD33E6A6EE2096CB6B120E7497B47B6C077A147CB57DAF693909D840716EEA7AA".HexToByteArray(),
                    Y = "047DB9995C35603C5E6B2F8CDFF0EB71D37AE3431BA0849EBAB13A4C9003C7969F55CD90".HexToByteArray(),
                },
                D = "00B4F1AE1E7FDCD4B0E82053C08A908852B26231E6C01670FCC6C3EA2C5D3FED40EDF037".HexToByteArray(),
            };
#endif // netcoreapp
    }
}
