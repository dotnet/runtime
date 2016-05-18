// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;

namespace System.Security.Cryptography.EcDsa.Tests
{
    // Note to contributors:
    //   Keys contained in this file should be randomly generated for the purpose of inclusion here,
    //   or obtained from some fixed set of test data. (Please) DO NOT use any key that has ever been
    //   used for any real purpose.
    //
    // Note to readers:
    //   The keys contained in this file should all be treated as compromised. That means that you
    //   absolutely SHOULD NOT use these keys on anything that you actually want to be protected.
    internal static class ECDsaTestData
    {
        internal static readonly byte[] s_hashSha512 =
            ("a232cec7be26319e53db0d48470232d37793b06b99e8ed82fac1996b3d1596449087769927d64af657cce62d853c4cf7ff4c"
           + "d069eda230d1c524d225756ffbaf").HexToByteArray();

        internal static ECParameters GetNistP256ExplicitTestData()
        {
            // explicit values for s_ECDsa256Key (nistP256)
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
    }
}
