// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Test.Cryptography;

namespace System.Security.Cryptography.X509Certificates.Tests.CertificateCreation
{
    public class EccTestData
    {
        internal static bool SupportsBrainpoolCurves { get; } = TestBrainpoolSupport();
        internal const string Secp256r1OidValue = "1.2.840.10045.3.1.7";
        internal const string Secp256r1OidHexValue = "06082A8648CE3D030107";
        internal const string Secp384r1OidValue = "1.3.132.0.34";
        internal const string Secp384r1OidHexValue = "06052B81040022";
        internal const string Secp521r1OidValue = "1.3.132.0.35";
        internal const string Secp521r1OidHexValue = "06052B81040023";
        internal const string BrainpoolP256r1OidValue = "1.3.36.3.3.2.8.1.1.7";
        internal const string BrainpoolP256r1OidHexValue = "06092B2403030208010107";
        internal const string BrainpoolP256t1OidValue = "1.3.36.3.3.2.8.1.1.8";
        internal const string BrainpoolP256t1OidHexValue = "06092B2403030208010108";

        internal ECParameters KeyParameters { get; private set; }
        internal string CurveOid { get; private set; }
        internal string CurveEncodedOidHex { get; private set; }
        internal string Name { get; private set; }

        internal static IEnumerable<EccTestData> EnumerateApplicableTests()
        {
            yield return Secp256r1Data;
            yield return Secp384r1Data;
            yield return Secp521r1Data;

            if (SupportsBrainpoolCurves)
            {
                yield return BrainpoolP256r1Data;
                yield return BrainpoolP256t1Data;
            }
        }

        internal static readonly EccTestData Secp256r1Data = new EccTestData
        {
            Name = nameof(Secp256r1Data),
            CurveOid = Secp256r1OidValue,
            CurveEncodedOidHex = Secp256r1OidHexValue,

            // Suite B Implementers Guide to FIPS 186-3,
            // D.1 Example ECDSA Signature for P-256
            KeyParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,

                Q = new ECPoint
                {
                    X = "8101ECE47464A6EAD70CF69A6E2BD3D88691A3262D22CBA4F7635EAFF26680A8".HexToByteArray(),
                    Y = "D8A12BA61D599235F67D9CB4D58F1783D3CA43E78F0A5ABAA624079936C0C3A9".HexToByteArray(),
                },

                D = "70A12C2DB16845ED56FF68CFC21A472B3F04D7D6851BF6349F2D7D5B3452B38A".HexToByteArray(),
            },
        };

        internal static readonly EccTestData Secp384r1Data = new EccTestData
        {
            Name = nameof(Secp384r1Data),
            CurveOid = Secp384r1OidValue,
            CurveEncodedOidHex = Secp384r1OidHexValue,

            // Suite B Implementers Guide to FIPS 186-3,
            // D.2 Example ECDSA Signature for P-384
            KeyParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP384,

                Q = new ECPoint
                {
                    X = (
                        "1FBAC8EEBD0CBF35640B39EFE0808DD774DEBFF20A2A329E" +
                        "91713BAF7D7F3C3E81546D883730BEE7E48678F857B02CA0").HexToByteArray(),
                    Y = (
                        "EB213103BD68CE343365A8A4C3D4555FA385F5330203BDD7" +
                        "6FFAD1F3AFFB95751C132007E1B240353CB0A4CF1693BDF9").HexToByteArray(),
                },

                D = (
                    "C838B85253EF8DC7394FA5808A5183981C7DEEF5A69BA8F4" +
                    "F2117FFEA39CFCD90E95F6CBC854ABACAB701D50C1F3CF24").HexToByteArray(),
            }
        };

        internal static readonly EccTestData Secp521r1Data = new EccTestData
        {
            Name = nameof(Secp521r1Data),
            CurveOid = Secp521r1OidValue,
            CurveEncodedOidHex = Secp521r1OidHexValue,

            KeyParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,

                Q = new ECPoint
                {
                    X = (
                        "0045A14653054227E6E539A189364397535652EE822C1135E9F9F4D34D7EA795A3" +
                        "B545CCD30312AC34E709042596BB049AB05BEE6C06F1E02BB9CC62A771EEFD8DAA").HexToByteArray(),

                    Y = (
                        "00AE3CEEBFF58625C0DD7A878B5CB883F95CE40DAE62C082418EBB96B59DC76366" +
                        "96ADC04B0201721F9D842C32588909B61F7E2236200E3EB29233A322E661A2EA05").HexToByteArray(),
                },

                D = (
                     "01CD352B44ED263B4BE0D8CDCB78B2CF9283FA8C74D27CA7629E600F0A15514278" +
                     "5769022DEEA3FDFC5C9830E546D7321A1080E574473A81D6DAF45E1D437926E9AF").HexToByteArray(),
            }
        };

        internal static readonly EccTestData Secp521r1_DiminishedPublic_Data = new EccTestData
        {
            Name = nameof(Secp521r1Data),
            CurveOid = Secp521r1OidValue,
            CurveEncodedOidHex = Secp521r1OidHexValue,

            KeyParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP521,

                Q = new ECPoint
                {
                    X = (
                        "00D27056ECECBF471448D9590845953B2B7CB4BA4D5F6B2DB6A28A37C1B0FEFFF4" +
                        "C45E9A0BE7D372C75E16A60BBF30942C4E2085DA53E03BDF282B652CE17477E52C").HexToByteArray(),

                    Y = (
                        "0053E0828044670D349F10D3673A1F50A19A24E2B5D7154339A2F77FB31F91A5CA" +
                        "FE2C13349B3680855D8CA303DA540D5C78C8FDD7DF08963B9C855BB6B199730A32").HexToByteArray(),
                },

                D = (
                     "01E262CBEA07EF60695D05D79257B6A57A175AB4243CEC99C60AD7BC5058666AB4" +
                     "C9DCF18443DCDEFDA5E08A7158D404BF328C7CECCD447F064FC5D681400509BCD8").HexToByteArray(),
            }
        };

        internal static readonly EccTestData BrainpoolP256r1Data = new EccTestData
        {
            Name = nameof(BrainpoolP256r1Data),
            CurveOid = BrainpoolP256r1OidValue,
            CurveEncodedOidHex = BrainpoolP256r1OidHexValue,

            KeyParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.brainpoolP256r1,

                Q = new ECPoint
                {
                    X = "419A7BB2B7CEBDD62A6BD3483FDE3985BFC2F3E0CD2AF362B430BC4A737BAC38".HexToByteArray(),
                    Y = "631730AF824E760DA912EB012893BE5692A7F3BFDD156CB9F7313DE2ECA3C184".HexToByteArray(),
                },

                D = "67DE7D6322530549BEC961149582C2EA9532B3641D273F13CD881D0D735EA531".HexToByteArray(),
            },
        };

        internal static readonly EccTestData BrainpoolP256t1Data = new EccTestData
        {
            Name = nameof(BrainpoolP256t1Data),
            CurveOid = BrainpoolP256t1OidValue,
            CurveEncodedOidHex = BrainpoolP256t1OidHexValue,

            KeyParameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.brainpoolP256t1,

                Q = new ECPoint
                {
                    X = "9A34B1EF0077DBCECC2034A101229B5D1B8DDF191D8064B4662C452414B508C1".HexToByteArray(),
                    Y = "96BA5E99FDF30EC25F3DA1A1F572C07D8810F63F7B8E8740ED666426C2E70836".HexToByteArray(),
                },

                D = "6FA384038FFCA28C21F9A491B99EC07DD021B241E6696C4F96BC5EB922B1D806".HexToByteArray(),
            },
        };

        public override string ToString()
        {
            return Name;
        }

        private static bool TestBrainpoolSupport()
        {
            try
            {
                using (ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.brainpoolP256r1))
                {
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
