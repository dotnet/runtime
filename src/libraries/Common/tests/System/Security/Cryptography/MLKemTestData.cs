// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Test.Cryptography;

namespace System.Security.Cryptography.Tests
{
    public static class MLKemTestData
    {
        internal const string MlKem512Oid = "2.16.840.1.101.3.4.4.1";
        internal const string MlKem768Oid = "2.16.840.1.101.3.4.4.2";
        internal const string MlKem1024Oid = "2.16.840.1.101.3.4.4.3";

        internal static ReadOnlySpan<byte> EncryptedPrivateKeyPasswordBytes => "PLACEHOLDER"u8;
        internal const string EncryptedPrivateKeyPassword = "PLACEHOLDER";

        internal static ReadOnlySpan<byte> IncrementalSeed =>
            [
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
                0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
            ];

        public static IEnumerable<object[]> MLKemAlgorithms =>
            [
                [MLKemAlgorithm.MLKem512],
                [MLKemAlgorithm.MLKem768],
                [MLKemAlgorithm.MLKem1024],
            ];

        internal static byte[] IetfMlKem512Spki => field ??= Convert.FromBase64String(@"
            MIIDMjALBglghkgBZQMEBAEDggMhADmVgV5ZfRBDVc8pqlMzyTJRhp1bzb5IcST2
            Ari2pmwWxHYWSK12XPXYAGtRXpBafwrAdrDGLvoygVPnylcBaZ8TBfHmvG+QsOSb
            aTUSts6ZKouAFt38GmYsfj+WGcvYad13GvMIlszVkYrGy3dGbF53mZbWf/mqvJdQ
            Pyx7fi0ADYZFD7GAfKTKvaRlgloxx4mht6SRqzhydl0yDQtxkg+iE8lAk0Frg7gS
            Tmn2XmLLUADcw3qpoP/3OXDEdy81fSQYnKb1MFVowOI3ajdipoxgXlY8XSCVcuD8
            dTLKKUcpU1VntfxBPF6HktJGRTbMgI+YrddGZPFBVm+QFqkKVBgpqYoEZM5BqLtE
            wtT6PCwglGByjvFKGnxMm5jRIgO0zDUpFgqasteDj3/2tTrgWqMafWRrevpsRZMl
            JqPDdVYZvplMIRwqMcBbNEeDbLIVC+GCna5rBMVTXP9Ubjkrp5dBFyD5JPSQpaxU
            lfITVtVQt4KmTBaItrZVvMeEIZekNML2Vjtbfwmni8xIgjJ4NWHRb0y6tnVUAAUH
            gVcMZmBLgXrRJSKUc26LAYYaS1p0UZuLb+UUiaUHI5Llh2JscTd2V10zgGocjicy
            r5fCaA9RZmMxxOuLvAQxxPloMtrxs8RVKPuhU/bHixwZhwKUfM0zdyekb7U7oR3l
            y0GRNGhZUWy2rXJADzzyCbI2rvNaWArIfrPjD6/WaXPKin3SZ1r0H3oXthQzzRr4
            D3cIhp9mVIhJeYCxrBCgzctjagDthoGzXkKRJMqANQcluF+DperDpKPMFgCQPmUp
            NWC5szblrw1SnawaBIEZMCy3qbzBELlIUb8CEX8ZncSFqFK3Rz8JuDGmgx1bVMC3
            kNIlz2u5LZRiomzbM92lEjx6rw4moLg2Ve6ii/OoB0clAY/WuuS2Ac9huqtxp6PT
            UZejQ+dLSicsEl1UCJZCbYW3lY07OKa6mH7DciXHtEzbEt3kU5tKsII2NoPwS/eg
            nMXEHf6DChsWLgsyQzQ2LwhKFEZ3IzRLrdAA+NjFN8SPmY8FMHzr0e3guBw7xZoG
            WhttY7Js");

        internal static byte[] IetfMlKem512PrivateKeySeed => field ??= Convert.FromBase64String(@"
            MFQCAQAwCwYJYIZIAWUDBAQBBEKAQAABAgMEBQYHCAkKCwwNDg8QERITFBUWFxgZ
            GhscHR4fICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj8=");

        internal static string IetfMlKem512PrivateKeySeedPem => field ??= PemEncoding.WriteString(
            "PRIVATE KEY",
            IetfMlKem512PrivateKeySeed);

        internal static byte[] IetfMlKem512EncryptedPrivateKeySeed => field ??= Convert.FromBase64String(@"
            MIGyMFYGCSqGSIb3DQEFDTBJMDEGCSqGSIb3DQEFDDAkBBBu4zqgXqt7HTK6mTmr
            5B/aAgIIADAMBggqhkiG9w0CCQUAMBQGCCqGSIb3DQMHBAioOjwRcwdjBwRYSGy/
            LN0wpvceGrPIQr/FTvN2wRvoozbkYMC1Tzs4phJh8lbMgdLgbTA0mCK16lBWgjdi
            /vxAu7Wn/wmKjFTqvST9vKxgu8sotadxpERtJaecmAaHqMjFtA==");

        internal static string IetfMlKem512EncryptedPrivateKeySeedPem => field ??= PemEncoding.WriteString(
            "ENCRYPTED PRIVATE KEY",
            IetfMlKem512EncryptedPrivateKeySeed);

        internal static byte[] IetfMlKem512PrivateKeyExpandedKey => field ??= Convert.FromBase64String(@"
            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
            JCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+Pw==");

        internal static string IetfMlKem512PrivateKeyExpandedKeyPem => field ??= PemEncoding.WriteString(
            "PRIVATE KEY",
            IetfMlKem512PrivateKeyExpandedKey);

        internal static byte[] IetfMlKem512EncryptedPrivateKeyExpandedKey => field ??= Convert.FromBase64String(@"
            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");

        internal static string IetfMlKem512EncryptedPrivateKeyExpandedKeyPem => field ??= PemEncoding.WriteString(
            "ENCRYPTED PRIVATE KEY",
            IetfMlKem512EncryptedPrivateKeyExpandedKey);

        internal static byte[] IetfMlKem512PrivateKeyBoth => field ??= Convert.FromBase64String(@"
            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
            Pj8=");

        internal static string IetfMlKem512PrivateKeyBothPem => field ??= PemEncoding.WriteString(
            "PRIVATE KEY",
            IetfMlKem512PrivateKeyBoth);

        internal static byte[] IetfMlKem512EncryptedPrivateKeyBoth => field ??= Convert.FromBase64String(@"
            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
            1ZBGWJKA1Fw=");

        internal static string IetfMlKem512EncryptedPrivateKeyBothPem => field ??= PemEncoding.WriteString(
            "ENCRYPTED PRIVATE KEY",
            IetfMlKem512EncryptedPrivateKeyBoth);

        internal static byte[] IetfMlKem512PrivateKeyDecapsulationKey => field ??= (
            "70554fd436344f2785b1b3b1bac184b6679003336c26f15a7de878c4825c6be03f3c4a480f75b7486aad31d3a00518623fd2" +
            "07ab528dd62721495835ae0062c367b74a71baf10aad0e8a2902076be31348beb15ccc0957cdebb4aff226756bbc601b6568" +
            "ab784acbaeb34702f0f86a26202118b22b23f83558776c79c14dba983379c803e0dcc3160a11757030e69c6919798d81eb69" +
            "8a9a4483a99e5a5cb2c31c9a661799f3cc89c790706ea041629045d42a83aed88860e394c69187e2105d28cc14ec393592d6" +
            "7dd00aa43fe8b4eae4414002866b5c713c6a8d7d16cf78b819d6f12e9e5a74233908f0b15e3c4ba8329c5cdda55c84928e3a" +
            "a8063e5aa9676403f91735b11010c7f593091364dc86445bc804840a9a21724212469f8a7b0ce0ac698eb86cad39a7f4824d" +
            "9a5163aac21ee6808b053c8a3facb0b6744b5262bbcb26a43f664c8732b64cfc7acf099605f41c796060976ac433833fe003" +
            "43fb1828300a424741116e4b45bb276ea81129a0db4c6e60bce611101e8c625474925e0222679308a3e7708d1972a7b423eb" +
            "232851c36d2ed53d3ed3bb7500637061a5dc2292fa1c466c07354683328bec2c1ed2cb5c99b78eca0969038cf7c34dd11872" +
            "4e31cae086206b34302b520f5d177aded5b3cce02acce808ea26bcc072625fdb93f17458a5fc1d4da394380a1f57e9cc6610" +
            "9438a075f0d2813fcc4a199cc76db3823f270b0061594192940411a37ffbafae2c150165cec5c6bf73c595fb92cd15312607" +
            "da070778652bd9944bc48bc7d1a534338bad0bad6656c5d502ce7850ab1587244eeb58f439ab5e08574a718c8aac3d77c798" +
            "bba1542733be73448f23fb70c0e5353a27c88322c5218493afbb38086434d6d60a56ba887dd498c3ab26a0870993815aa6a4" +
            "0975f218adca1582d64ffc8652fbb3a9a6fbc304f91945fa4aaef2878fd715df70113d2379f44886f812c83ff2b719a69e1e" +
            "c74ae4b15accd3aed5a53ce76a7b0982471633b973cb40a1a0015d0a424fa11a479c023017436d2a2900e993eb5a0a067400" +
            "c7f4aadf201fc4fa31264a63bae95cc8d65c3995815e597d104355cf29aa5333c93251869d5bcdbe487124f602b8b6a66c16" +
            "c4761648ad765cf5d8006b515e905a7f0ac076b0c62efa328153e7ca5701699f1305f1e6bc6f90b0e49b693512b6ce992a8b" +
            "8016ddfc1a662c7e3f9619cbd869dd771af30896ccd5918ac6cb77466c5e779996d67ff9aabc97503f2c7b7e2d000d86450f" +
            "b1807ca4cabda465825a31c789a1b7a491ab3872765d320d0b71920fa213c94093416b83b8124e69f65e62cb5000dcc37aa9" +
            "a0fff73970c4772f357d24189ca6f5305568c0e2376a3762a68c605e563c5d209572e0fc7532ca294729535567b5fc413c5e" +
            "8792d2464536cc808f98add74664f141566f9016a90a541829a98a0464ce41a8bb44c2d4fa3c2c209460728ef14a1a7c4c9b" +
            "98d12203b4cc3529160a9ab2d7838f7ff6b53ae05aa31a7d646b7afa6c45932526a3c3755619be994c211c2a31c05b344783" +
            "6cb2150be1829dae6b04c5535cff546e392ba797411720f924f490a5ac5495f21356d550b782a64c1688b6b655bcc7842197" +
            "a434c2f6563b5b7f09a78bcc488232783561d16f4cbab6755400050781570c66604b817ad1252294736e8b01861a4b5a7451" +
            "9b8b6fe51489a5072392e587626c713776575d33806a1c8e2732af97c2680f51666331c4eb8bbc0431c4f96832daf1b3c455" +
            "28fba153f6c78b1c198702947ccd337727a46fb53ba11de5cb4191346859516cb6ad72400f3cf209b236aef35a580ac87eb3" +
            "e30fafd66973ca8a7dd2675af41f7a17b61433cd1af80f7708869f665488497980b1ac10a0cdcb636a00ed8681b35e429124" +
            "ca80350725b85f83a5eac3a4a3cc1600903e65293560b9b336e5af0d529dac1a048119302cb7a9bcc110b94851bf02117f19" +
            "9dc485a852b7473f09b831a6831d5b54c0b790d225cf6bb92d9462a26cdb33dda5123c7aaf0e26a0b83655eea28bf3a80747" +
            "25018fd6bae4b601cf61baab71a7a3d35197a343e74b4a272c125d540896426d85b7958d3b38a6ba987ec37225c7b44cdb12" +
            "dde4539b4ab082363683f04bf7a09cc5c41dfe830a1b162e0b324334362f084a14467723344badd000f8d8c537c48f998f05" +
            "307cebd1ede0b81c3bc59a065a1b6d63b26c82f101ff648063b376e2bb6c5b7455f655a50c2feadade150efa0e0e6f365aea" +
            "202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f"
        ).HexToByteArray();

        internal const string IetfMlKem512CertificatePem = """
            -----BEGIN CERTIFICATE-----
            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
            -----END CERTIFICATE-----
            """;

        internal static byte[] IetfMlKem768Spki => field ??= Convert.FromBase64String(@"
            MIIEsjALBglghkgBZQMEBAIDggShACmKoQ1CPI3aBp0CvFnmzfA6CWuLPaTKubgM
            pKFJB2cszvHsT68jSgvFt+nUc/KzEzs7JqHRdctnp4BZGWmcAvdlMbmcX4kYBwS7
            TKRTXFuJcmecZgoHxeUUuHAJyGLrj1FXaV77P8QKne9rgcHMAqJJrk8JStDZvTSF
            wcHGgIBSCnyMYyAyzuc4FU5cUXbAfaVgJHdqQw/nbqz2ZaP3uDIQIhW8gvEJOcg1
            VwQzao+sHYHkuwSFql18dNa1m75cXpcqDYusQRtVtdVVfNaAoaj3G064a8SMmgUJ
            cxpUvZ1ykLJ5Y+Q3Lcmxmc/crAsBrNKKYjlREuTENkjWIsSMgjTQFEDozDdskn8j
            pa/JrAR0xmInTkJFJchVLs47P+JlFt6QG8fVFb3olVjmJslcgLkzQvgBAATznmxs
            lIccXjRMqzlmyDX5qWpZr9McQChrOLHBp4RwurlHUYk0RTzoZzapGfH1ptUQqG9U
            VPw5gMtcdlvSvV97NrFBDWY1yM60fE3aDXaijqyTnHHDAkgEhmxxYmZYRCFjwsIh
            F+UKzvzmN4qYVlIwKk7wws4Mxxa3eW4ray43d9+hrD2iWaMbWptTD4y2OKgaYqww
            GEmrr5WnMBvaMAaJCb/bfmfbzLs4pVUaJbGjoPaFdIrVdT2IgPABbGJ0hhZjhMVX
            H+I2WQA2TQODEeLYdds2ZoaTK17GAkMKNp6Hpu9cM4eGZXglvUwFes65I+sJNeaQ
            XmO0ztf4CFenc91ksVDSZhLqmsEgUtsgF78YQ8y0sygbaQ3HKK36hcACgbjjwJKH
            M1+Fa0/CiS9povV5Ia2gGRTECYhmLVd2lmKnhjUbm2ZJPat5WU2YbeIQDWW6D/Tq
            WLgVONJKRDWiWPrCVASqf0H2WLE4UGXhWNy2ARVzJyD0BFmqrBXkBpU6kKxSmX0c
            zQcAYO/GXbnmUzVEZ/rVbscTyG51QMQjrPJmn1L6b0rGiI2HHvPoR8ApqKr7uS4X
            skqgebH0GbphdbRCr7EZCdSla3CgM1soc5IYqnyTSOLDwvPrPRWkHmQXwN2Uv+sh
            QZsxGnuxOhgLvoMyGKmmsXRHzIXyJYWVh6cwdwSay8/UTQ8CVDjhXRU4Jw1Ybhv4
            MZKpRZz2PA6XL4UpdnmDHs8SFQmFHLg0D28Qew+hoO/Rs2qBibwIXE9ct4TlU/Qb
            kY+AOXzhlW94W+43fKmqi+aZitowwmt8PYxrVSVMyWIDsgxCruCsTh67QI5JqeP4
            edCrB4XrcCVCXRMFoimcAV4SDRY7DhlJTOVyU9AkbRgnRcuBl6t0OLPBu3lyvsWj
            BuujVnhVwBRpn+9lrlTHcKDYXBhADPZCrtxmB3e6SxOFAr1aeBL2IfhKSClrmN1D
            IrbxWCi4qPDgCoukSlPDqLFDVxsHQKvVZ9rxzenHnCBLbV4lnRdmoxu7y05qBc9F
            AhdrMBwcL0Ekd1AVe87IXoCbMKTWDXdHzdD1uZqoyCaYdRd5OqqAgKCxJKhVjfcr
            vje3X07btr6CFtbGM/srIoDiURPYaV5DSBw+6zl+sZJQUim2eiAeqJPD4ssy2ovD
            QvpN6gV4");

        internal static byte[] IetfMlKem768PrivateKeySeed => field ??= Convert.FromBase64String(@"
            MFQCAQAwCwYJYIZIAWUDBAQCBEKAQAABAgMEBQYHCAkKCwwNDg8QERITFBUWFxgZ
            GhscHR4fICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj8=");

        internal static string IetfMlKem768PrivateKeySeedPem => field ??= PemEncoding.WriteString(
            "PRIVATE KEY",
            IetfMlKem768PrivateKeySeed);

        internal static byte[] IetfMlKem768EncryptedPrivateKeySeed => field ??= Convert.FromBase64String(@"
            MIGyMFYGCSqGSIb3DQEFDTBJMDEGCSqGSIb3DQEFDDAkBBDVvN7dPv1xeTQ5V4S4
            lNYAAgIIADAMBggqhkiG9w0CCQUAMBQGCCqGSIb3DQMHBAhxYX16f/Or8ARY98/3
            tAF57U+XfDsiweIKGW37VcOMgrJr4jl8Tn6E1MC9sNiSKXd5Ge93Oscm46wIYOG/
            ltLe5Ba3maubTj7Sj1UHsFIRE0NGcpha09u2JH8iHIBR4tvBtg==");

        internal static string IetfMlKem768EncryptedPrivateKeySeedPem => field ??= PemEncoding.WriteString(
            "ENCRYPTED PRIVATE KEY",
            IetfMlKem768EncryptedPrivateKeySeed);

        internal static byte[] IetfMlKem768PrivateKeyExpandedKey => field ??= Convert.FromBase64String(@"
            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
            JCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+Pw==");

        internal static string IetfMlKem768PrivateKeyExpandedKeyPem => field ??= PemEncoding.WriteString(
            "PRIVATE KEY",
            IetfMlKem768PrivateKeyExpandedKey);

        internal static byte[] IetfMlKem768EncryptedPrivateKeyExpandedKey => field ??= Convert.FromBase64String(@"
            MIIJ3DBWBgkqhkiG9w0BBQ0wSTAxBgkqhkiG9w0BBQwwJAQQdV5wgVIICzzniNpD
            y7WD9gICCAAwDAYIKoZIhvcNAgkFADAUBggqhkiG9w0DBwQIj7uC5kmav+kEggmA
            QScCF6vlfHKzzp6SNeBj7Tp0H7UsYjGBidPdAuYzyKUhIDLbzE0kv3Y1QIKHan1s
            A10sgch4exJ+3q1Eej/TPfYe4zygiN/R3WzfFyUAKogMk7MUlZoGQby0ZehOdnfD
            Gpr6YzUzd4tiNoqXWUbkgit03Frm5KMQONAdcYDvloF276Ac1mYDu/2Ux1wy4jBQ
            u4HGCW7J9e/FvIEp55DsoklcvnT1isvMlXMocHQeKeOeuUobBDnca27hTu4NM3Fk
            XHd2ZcuNDDob2pWHAE3geH261nK0tV4DH+MXfPjaoU0CkFEcV+4K8wC8V4t+rwCQ
            f2F2U0IqSRQn1tg1rMG8fbn/rL5QrQaDYy2FL5PYoXMyjGwLq6xP49OJy7oWKkZu
            wZr9O6XOGuPGmrav2NKauIrk+sgxt6QpKkfMOnganh7V5WbieutqVOwQKITJ0HyM
            OfVpU/ZTiz59SsX+N2RsUCAMD7eqgUCDabtgyvyiPv4bAorchri3lB2pQJJhzL1X
            ruz1fyW1wNpDt6//rY17fs5XgkXROJY2UQG1cHUsJHOzfSR7wy97+ssY873rEs3H
            LFEMWAzpajAvHsCqx86ZGHaxWHTU0ZWcr9mYfH1B9PA8lhod1VoIpLIVDGqIb/h9
            FuLXqIVhLMwTMqMScUIh7uoC8r93ZQrlVMz5STTawaa3jNUqaHwaRNPFFOjjhWdF
            Kn6U4OS31m0P4AtIpF2DO51PRACdS2At/SorZ8sOy6/WW26eMlabpl466/svNU2b
            w0ngG83UuVm1YXEMnosYXaKyy2eQ7wOi3V7EpnMorEOl3Ty0RM34b69yjpA3NZ0M
            Hiig7GT5N5sJkOBARLxiNAejfAEmbtdvLrhqe+F+QRTB5HcnQTKxjn6f77QKMmWN
            hqh9f9tm9xLPJpn2BLOj61tFf6suKbTe5pWVEQhXUXHmlMKgM/TZoNOP2yGJkP3r
            getxy2iIU8Dj0GUjMy5cg4Y46HhqztmW4llvQNBKXIpOwm47MHMYEL3BeiP70lhX
            sI60Sa/z68+XyKxwyUO4mHhjA0HwbZhyYPcNGmhBCot8dnSpXOyTxU9EyAWNnENl
            RryG9G1kWbN9uXGR6i5K8tCQQKnIFyxTECtKua/4HYKq21qU71Xvuw73ir91NXIY
            gXEYLq8gR0Q+X8VrnIGgcYNXqFZAuAAFbVpVijEALQ2E9ed0/IvUTWADr8BEOj3N
            ab7Ubm6TRBwHMVfcw/7TQng9tCgz7TUItljAMKwrY8dvAPoupGNi90wjmNNTW3AU
            MDgWAmQMgSGm5rmXJFdCx1UznHqC+Vq3X3zq4aUjLA9QtMYCpL5Afx5WbwUPv1k7
            JMlb5J+4B+U9mMOgRm2/wAFkT4gOFtfA46n6tK4ZWWQdJnL2UjNh1AQK5C1dNs6Z
            ZI9plENBnnc+q+nhRaMp+rTnryFCPHkEDQe6us813013hJgVhCfwK9+7srgRSreU
            piMe9tR3iB+wR5/YkWYBA/mvcHOhmN8sEQkiTrGWnNU2TTuxFITZ/YYhDBlV8u6h
            77vyLxrM/uvHIpo/dbgZMyHJqvYAG0nQf3qsj2pG0Qyf/qLfo2fTb1vKxlbq4Dw7
            8bFRHwiIjkFxsb6EGcLnmK09NvK8k2VAZkdrbcjHYMUKq4LEL5hAEuynQ/ya/pgt
            NN7no04boQ9UW3UZfs4e+A5Ja25325XGDyL2NTi0tX0XDp9PWnUC276py3BpVaK0
            b85XJuEaTz9vJ2K8MB9gD1aVP+oLGkoSy/Ws0wgy3UbtQ6hSCLUR5FVFx6EFPyXU
            DJy5PdHxwHEkOZdIPyuPN7oetOD9mAu4U4CHJe2Z9yCE+gfwpe4K1bvehtDO7xC1
            oz269msjjUQ5BthBCxPi/ups8Q7CbFcNtmdI9/HQxCRwqiuC7iC5R5HxMNJQvvl3
            nzCfgJqGhRi3Gl1Qd+358A/CGoXvimbchOF6v/pNcjama2qoi1bulh/lxwDh/qu8
            16GClhxY6L5LkWOuWnME/lBpvoNye5l4MEDFDHN1v0xdqK2uxsOMxCLEnxlRnGa2
            B5rKkrIaRRv3Da1B4gwOeHT4fPaLVW70W1O0U397GS5h+Nr9zN0aZa6wZFYjmH9G
            EAFKiz9F8v2+x3E9muwe79OTquVxXPUwWxaOK6knT8St9zkvb6HGiyRKTXpIV1eu
            4knxKqqqTlqLiR1CIapvyCiZVTj67oVLRLnuhgyaiir6G0JFJKaCGA43z1NxcDre
            HzUoKz6+DbqhFKgCLtI4uzBEIAziLw+eLKLD4UcmHiAQbDt7Xrkb5tCfGSzqqboD
            p3jHf+jFWIVg2o+xfcSpv7Lu4+1biCUMVHzI0pNs/cDnKkKbQ0bbOxpepYYmkQxK
            kSR7xkitnrqEhi+zhlzKON5PMq3d3S9f6dhBIaJshCiJdwe8zB3Qv/bO0xau9yFI
            Xs9I0IQ0elH2961rpk7ycYqlkF70mHr6gW3i502A2zQ+pOTEwc0/CGifIQMpwU+4
            HLSKXfoSbyDrSojXei6yvkE9V+m54dnoxOLesMY3+amUzA8pHTrKtaX0rW0xJxUU
            GadsklH5gT9L/9h0y9xrNVS6FHBIt6UhB4an2J8jXQqzlz67xBV87Ee8rF+wSac3
            6L/S+suOi1dvbS/kYoYIZiEc2iQi2xtf0UlVZV2jxbLEIk5084TCWPSVz1lLMR4p
            4GbO6+/vNKO+Hy5MZSdFtaOE4ITEjLsRUjqbOKvj54VZZBttJcV/kaIaCGzDei2B
            KAokVUvJYluh7AYc6j9SKmA1VqJF62AcY3+dHPm8dULvf3zj8nmz7R/5vcFyPEOA
            lG59dYQxdnG1wzDrwd+UWXW4+0vl9WrAZBOg8gjPMhJaOX6rK+3rQcyM9oHasGYh
            i1ZRdJmmB6N/N2me9qKI7dN7nv5YLwXlFNWOgbfw97mE9cmv4DkCxwwUjOofK6bK
            zgQIr4YIGyWXDFYW04m+j8XPAHysvFfo2lU/vxUmWHezJUruXNA49PnmcCppa6aQ
            p9xLhjIUFLxKb+yiUoeUvsZAekkyy8+nTqUPrB/G/mFvGkxa8587HZI0YsR3a0lL
            NVzHLIRJmvTI+SGegSb/evnKdGvLbqUM9s+KK5DiWKnBH90kGR+N/tO3TVSgwpC0
            X3qZc/K8q1BBn9dqcJRIKr/dZ7Mq1U6sa5zg+sDIZvLoS/weutBuPRHP9AofQWpS
            F1JkgTbf0PrGVr3jgdaXCY/7vfsB6+utgcs1F7KfKZA=");

        internal static string IetfMlKem768EncryptedPrivateKeyExpandedKeyPem => field ??= PemEncoding.WriteString(
            "ENCRYPTED PRIVATE KEY",
            IetfMlKem768EncryptedPrivateKeyExpandedKey);

        internal static byte[] IetfMlKem768PrivateKeyBoth => field ??= Convert.FromBase64String(@"
            MIIJvgIBADALBglghkgBZQMEBAIEggmqMIIJpgRAAAECAwQFBgcICQoLDA0ODxAR
            EhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+PwSC
            CWAn0qd/M3VvYSCO8ROr6CWVhz1KvHMOW11nlSm/akzrY4NCcjGoYS9BVQUVrLpS
            5I6ti5QoM7vmhl0T0Up50sXD4H8KBW2N56rfyroFjEk8gLN8q4xWJ1O7O6a27IKX
            +IXqp1QNUwAVqEQG5VsTZrV34jbOWKJtih61pE1UIyPCFn2b9KR/mFaZygW65DuN
            7GF/AjgKOJCv1LjH7H7eJlU6Al885bxdemITAwQjXLGtSDa1ZrW4Y72b20WihEpw
            R7bI04PkSFJeBAtNyKK0jGw3yW1i1D8/2I4ogcQKIFyeJI9lK1kngad5+GiA8qFH
            tnhj85HMGlqQjACV4HISKR4u+KNuuanAxgcyJbNHA6SvBJOCxHVz2mj96SRa1ETj
            Gx+9tSHx9h83vAzvKSBn5nDSih/9kE9vEZCplpGKEwN6bKvzw3O/gpbNN6szundG
            gJzD+K3hs2Ob1Xv8xpZQqq8d4Zj8TARjKZ5SxGF4DMQo/F0EpcUYUMumwqUnQ0Bn
            V5PdoJvkTCnmOVxl+F0qCnxt9BHmkRsfLLbDUc0uh19Rtji+d2CX6T4vKy+D2gvu
            9KqFup52OrZFAqDKUiLp6rWztwiO1SBg6Mgmm5Q6casK4cWxtofS4BnPgDa8+b9u
            e6w6qjbkFmD6pFQPJkjNk6GJ7Fwt6nC6yqpP/JBvkIEOobZ78k8seM9rqIGq6mHA
            ZSv/lbG65EJtF3O5zCyoLCHjjGNuOxxSMkSYawvoqD9d1c8tVHYvs8Xr9ZuOiFMC
            sc5HAz7fdg9OApvkC21WaxnddYrNXHQSh4ExJE+QFyxT8mZjwh2QUwHUi6+RyRfM
            d3np2IAswQ2Jo3BQmaKtOjqIlnQ8EURpgJO+JX2stm3HhSKLkSyNll0Uqig0LDrE
            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
            Pj8=");

        internal static string IetfMlKem768PrivateKeyBothPem => field ??= PemEncoding.WriteString(
            "PRIVATE KEY",
            IetfMlKem768PrivateKeyBoth);

        internal static byte[] IetfMlKem768EncryptedPrivateKeyBoth => field ??= Convert.FromBase64String(@"
            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
            RDd5KC8EEzk=");

        internal static string IetfMlKem768EncryptedPrivateKeyBothPem => field ??= PemEncoding.WriteString(
            "ENCRYPTED PRIVATE KEY",
            IetfMlKem768EncryptedPrivateKeyBoth);

        internal static byte[] IetfMlKem768PrivateKeyDecapsulationKey => field ??= (
            "27d2a77f33756f61208ef113abe82595873d4abc730e5b5d679529bf6a4ceb6383427231a8612f41550515acba52e48ead8b" +
            "942833bbe6865d13d14a79d2c5c3e07f0a056d8de7aadfcaba058c493c80b37cab8c562753bb3ba6b6ec8297f885eaa7540d" +
            "530015a84406e55b1366b577e236ce58a26d8a1eb5a44d542323c2167d9bf4a47f985699ca05bae43b8dec617f02380a3890" +
            "afd4b8c7ec7ede26553a025f3ce5bc5d7a62130304235cb1ad4836b566b5b863bd9bdb45a2844a7047b6c8d383e448525e04" +
            "0b4dc8a2b48c6c37c96d62d43f3fd88e2881c40a205c9e248f652b592781a779f86880f2a147b67863f391cc1a5a908c0095" +
            "e07212291e2ef8a36eb9a9c0c6073225b34703a4af049382c47573da68fde9245ad444e31b1fbdb521f1f61f37bc0cef2920" +
            "67e670d28a1ffd904f6f1190a996918a13037a6cabf3c373bf8296cd37ab33ba7746809cc3f8ade1b3639bd57bfcc69650aa" +
            "af1de198fc4c0463299e52c461780cc428fc5d04a5c51850cba6c2a5274340675793dda09be44c29e6395c65f85d2a0a7c6d" +
            "f411e6911b1f2cb6c351cd2e875f51b638be776097e93e2f2b2f83da0beef4aa85ba9e763ab64502a0ca5222e9eab5b3b708" +
            "8ed52060e8c8269b943a71ab0ae1c5b1b687d2e019cf8036bcf9bf6e7bac3aaa36e41660faa4540f2648cd93a189ec5c2dea" +
            "70bacaaa4ffc906f90810ea1b67bf24f2c78cf6ba881aaea61c0652bff95b1bae4426d1773b9cc2ca82c21e38c636e3b1c52" +
            "3244986b0be8a83f5dd5cf2d54762fb3c5ebf59b8e885302b1ce47033edf760f4e029be40b6d566b19dd758acd5c74128781" +
            "31244f90172c53f26663c21d905301d48baf91c917cc7779e9d8802cc10d89a3705099a2ad3a3a8896743c1144698093be25" +
            "7dacb66dc785228b912c8d965d14aa28342c3ac4a93fefa532b20945ddc1020139c14d638b908c4ddde9a0645b95b2e4414d" +
            "40bb79f04413830f15a873c28bb7059c2741002015f20408f058e715b0bf995b5380b7dd325a056ab97e659a2be0cdf6c337" +
            "31c683a634b771e8c92a139aee4bb0e49c7077321d42fc199f7c1f298ca625d223a5c263a03cc48159b7812665b78637e4e1" +
            "8720b2c29a6b99f42766a4cbc4dc508ba94ba83b89c3a5c78f8bb26bbd9b79beb8c8182490f5793ee5b96013b74b7e169e29" +
            "d162f1315464ea7d72436d89b755161192c81cc2dd1c8b8bba795ef426ee1cc01c37aaa37b2cff8b0a378b47cbd0b4d49398" +
            "cfc2712959699fa0bd8cd84666acc61f541b84fa96b9c854e4e75e9144addb44b8566a57dfbb545ce423c03346f2b2c1a917" +
            "80d152a8de1a4d4c9cacde7392c996888cc2399c02c38b3353adf8acab283924da00a05b76e738c72c930d6cba09ae168990" +
            "faa1fef2226e780861d416eff402f4f759fc648ab1f97100109087f96e4b148d2cb31e4805314ea0cd95fb023eac0d989474" +
            "ba4201d7b41d26f5394b217eea5b34b71a8b37931c0e594271e0b7c733257240233e7ba735603e425a87dee77079e37cb28a" +
            "21764594ce5350d8da2b62a07174943032ec89c98809c73b6423d30c1d283a766a64d89703c3d629b497828d48320c346210" +
            "797a298aa10d423c8dda069d02bc59e6cdf03a096b8b3da4cab9b80ca4a14907672ccef1ec4faf234a0bc5b7e9d473f2b313" +
            "3b3b26a1d175cb67a7805919699c02f76531b99c5f89180704bb4ca4535c5b8972679c660a07c5e514b87009c862eb8f5157" +
            "695efb3fc40a9def6b81c1cc02a249ae4f094ad0d9bd3485c1c1c68080520a7c8c632032cee738154e5c5176c07da5602477" +
            "6a430fe76eacf665a3f7b832102215bc82f10939c8355704336a8fac1d81e4bb0485aa5d7c74d6b59bbe5c5e972a0d8bac41" +
            "1b55b5d5557cd680a1a8f71b4eb86bc48c9a0509731a54bd9d7290b27963e4372dc9b199cfdcac0b01acd28a62395112e4c4" +
            "3648d622c48c8234d01440e8cc376c927f23a5afc9ac0474c662274e424525c8552ece3b3fe26516de901bc7d515bde89558" +
            "e626c95c80b93342f8010004f39e6c6c94871c5e344cab3966c835f9a96a59afd31c40286b38b1c1a78470bab94751893445" +
            "3ce86736a919f1f5a6d510a86f5454fc3980cb5c765bd2bd5f7b36b1410d6635c8ceb47c4dda0d76a28eac939c71c3024804" +
            "866c71626658442163c2c22117e50acefce6378a985652302a4ef0c2ce0cc716b7796e2b6b2e3777dfa1ac3da259a31b5a9b" +
            "530f8cb638a81a62ac301849abaf95a7301bda30068909bfdb7e67dbccbb38a5551a25b1a3a0f685748ad5753d8880f0016c" +
            "627486166384c5571fe2365900364d038311e2d875db366686932b5ec602430a369e87a6ef5c338786657825bd4c057aceb9" +
            "23eb0935e6905e63b4ced7f80857a773dd64b150d26612ea9ac12052db2017bf1843ccb4b3281b690dc728adfa85c00281b8" +
            "e3c09287335f856b4fc2892f69a2f57921ada01914c40988662d57769662a786351b9b66493dab79594d986de2100d65ba0f" +
            "f4ea58b81538d24a4435a258fac25404aa7f41f658b1385065e158dcb60115732720f40459aaac15e406953a90ac52997d1c" +
            "cd070060efc65db9e653354467fad56ec713c86e7540c423acf2669f52fa6f4ac6888d871ef3e847c029a8aafbb92e17b24a" +
            "a079b1f419ba6175b442afb11909d4a56b70a0335b28739218aa7c9348e2c3c2f3eb3d15a41e6417c0dd94bfeb21419b311a" +
            "7bb13a180bbe833218a9a6b17447cc85f225859587a73077049acbcfd44d0f025438e15d1538270d586e1bf83192a9459cf6" +
            "3c0e972f85297679831ecf121509851cb8340f6f107b0fa1a0efd1b36a8189bc085c4f5cb784e553f41b918f80397ce1956f" +
            "785bee377ca9aa8be6998ada30c26b7c3d8c6b55254cc96203b20c42aee0ac4e1ebb408e49a9e3f879d0ab0785eb7025425d" +
            "1305a2299c015e120d163b0e19494ce57253d0246d182745cb8197ab7438b3c1bb7972bec5a306eba3567855c014699fef65" +
            "ae54c770a0d85c18400cf642aedc660777ba4b138502bd5a7812f621f84a48296b98dd4322b6f15828b8a8f0e00a8ba44a53" +
            "c3a8b143571b0740abd567daf1cde9c79c204b6d5e259d1766a31bbbcb4e6a05cf4502176b301c1c2f41247750157bcec85e" +
            "809b30a4d60d7747cdd0f5b99aa8c826987517793aaa8080a0b124a8558df72bbe37b75f4edbb6be8216d6c633fb2b2280e2" +
            "5113d8695e43481c3eeb397eb192505229b67a201ea893c3e2cb32da8bc342fa4dea0578a24e16d8f8f9383a95b77050f4d9" +
            "fd2f5733eec1d63ef3c23ebf9918173669a7202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f").HexToByteArray();

        internal const string IetfMlKem768CertificatePem = """
            -----BEGIN CERTIFICATE-----
            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
            AAAAAAAAAAAAAAAFDBUZHyU=
            -----END CERTIFICATE-----
            """;

        internal static byte[] IetfMlKem1024Spki => field ??= Convert.FromBase64String(@"
            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
            uYOILhF1");

        internal static byte[] IetfMlKem1024PrivateKeySeed => field ??= Convert.FromBase64String(@"
            MFQCAQAwCwYJYIZIAWUDBAQDBEKAQAABAgMEBQYHCAkKCwwNDg8QERITFBUWFxgZ
            GhscHR4fICEiIyQlJicoKSorLC0uLzAxMjM0NTY3ODk6Ozw9Pj8=");

        internal static string IetfMlKem1024PrivateKeySeedPem => field ??= PemEncoding.WriteString(
            "PRIVATE KEY",
            IetfMlKem1024PrivateKeySeed);

        internal static byte[] IetfMlKem1024EncryptedPrivateKeySeed => field ??= Convert.FromBase64String(@"
            MIGyMFYGCSqGSIb3DQEFDTBJMDEGCSqGSIb3DQEFDDAkBBArGFO1mU77a3ys0aR0
            +mWBAgIIADAMBggqhkiG9w0CCQUAMBQGCCqGSIb3DQMHBAh48Gqhu7YOpwRYPR66
            W02NrqRok/CagC9uo/viGlLLC5CUl4Y9cE3ZCEwfDxFufNeALt2Kusg+gJLMSq16
            g6YgQHQJeKZusLSnwzxOutuyKKgbGuIWxFBmtDZrXDjCO913Ow==");

        internal static string IetfMlKem1024EncryptedPrivateKeySeedPem => field ??= PemEncoding.WriteString(
            "ENCRYPTED PRIVATE KEY",
            IetfMlKem1024EncryptedPrivateKeySeed);

        internal static byte[] IetfMlKem1024PrivateKeyExpandedKey => field ??= Convert.FromBase64String(@"
            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
            JCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+Pw==");

        internal static string IetfMlKem1024PrivateKeyExpandedKeyPem => field ??= PemEncoding.WriteString(
            "PRIVATE KEY",
            IetfMlKem1024PrivateKeyExpandedKey);

        internal static byte[] IetfMlKem1024EncryptedPrivateKeyExpandedKey => field ??= Convert.FromBase64String(@"
            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");

        internal static string IetfMlKem1024EncryptedPrivateKeyExpandedKeyPem => field ??= PemEncoding.WriteString(
            "ENCRYPTED PRIVATE KEY",
            IetfMlKem1024EncryptedPrivateKeyExpandedKey);

        internal static byte[] IetfMlKem1024PrivateKeyBoth => field ??= Convert.FromBase64String(@"
            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
            Pj8=");

        internal static string IetfMlKem1024PrivateKeyBothPem => field ??= PemEncoding.WriteString(
            "PRIVATE KEY",
            IetfMlKem1024PrivateKeyBoth);

        internal static byte[] IetfMlKem1024EncryptedPrivateKeyBoth => field ??= Convert.FromBase64String(@"
            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
            FIgRVDi1lQg=");

        internal static string IetfMlKem1024EncryptedPrivateKeyBothPem => field ??= PemEncoding.WriteString(
            "ENCRYPTED PRIVATE KEY",
            IetfMlKem1024EncryptedPrivateKeyBoth);

        internal static byte[] IetfMlKem1024PrivateKeyDecapsulationKey => field ??= (
            "f77b7f6b15c73fe2cc546b67fb774ca19b42cd463ea9fbb984ca477a77b6c71087cbf051abe4736a9072c6e870c8311c5596" +
            "3f500a3c7b1b8f2a58558f49c62527b6c594b5e7acb3bcf597273a5743517d151208bd4aa61e75ba67b0bd594a994919627a" +
            "c0a804d489e171336bc339f4666706e5134412b366823d50318c8bf261ab120a28a04fec01cc15f2b71912cee54aa8eed854" +
            "694b6ba886b5eb7661e6d56aac213cc1d814d592b395554fae74476d34371163129bf864527250606cc21a53746b20997077" +
            "bba155733b28a4e7fa0776399524763eb481ceaa11366c3474a04685f40c3f08b0424f40bff949a0ac92704c3ba0c6eb36f1" +
            "f5b621d8bf2b6327beb57cd3facb94186fe3fc9ab0a1434bb291d2c9bb70723057e2254059656f565919a32cf74579de8968" +
            "1cd2c5a935a52b4aaa2d24cb5d5c9e20729ec5492ec36961efb8a28cbc00ac303523295f3d8036abc1603307ce70d7848a35" +
            "657a5687dd589927ea63731626abb26ec4e431b8eb6b3b0bc1e82573ee73b1a021183183528108ae2eacaddb95b464a0b984" +
            "69c319cc27bfa01bc31054a68c05502b1662b879fe98a1711c3426f6436cb0214cea379ac3a7e5fb60184a37c1da1eda61c6" +
            "c39c1dd4e847845811f2a358a43731528536d4a3291b04158c2c3dc641624882678bc7805f58a9d94c7104567846a2044e65" +
            "aece2a225372b6024799a5477d60237504aa5c0ac57bc70a3558c08c4de687ef1302b4fcb5594413d22cb959bc31be423450" +
            "403c6bc57dc411b3fefac1052ac4bb162c44545a4ca80892657fa13a0b2c482ced629cc4999d969c593d4aadf073cc3e3a45" +
            "8e78a8aa039408e652be93b20c8b42ec5b0e50239dac726052851a6d15312ec39ed208b72209a577c6b2770112895749d526" +
            "0e7dd446c0b0118c1000be6801d2611fcf00792a9cc4f4b49922f9a2d4b9c8fa5a5d0d60506631a7e971cee840b08fa63c13" +
            "729d7ea5aac70352a984cdb669331cba758fe87ec3931b3e3161fcc747aa749424689feae14bf7c9a2ffba1302b212b80372" +
            "d8e9049db69a3a1261d0a2859a9b4d57899e0ba41607a1b67a7c0e12923689f8c6395377d970c7490a4129611a1d05c3b781" +
            "3bed945420723f7f9525a87793fafbbfca982e66bb80681c83248a89da084c19882f48f31e7fc09093a49e9fd09691b021ed" +
            "f463afc519b62853816118346115fb0b882cc6482f3c5cbcc1c1894697e1239598b34b2a9a7acd15244d0690c88194097a9b" +
            "eda585e87c437124624c210768e6215d376482653eb89947877c118d370c696a6ffcc1018ae413a08a8d0ffaa819945da7a1" +
            "67c229913290cad1c80a369258762610ea253e62dc24226a30c892c12136c326f13f4446664712b0b90bc063b4028593cbde" +
            "06cdc22289e240c7e296b59172c1aeda8c99e0512d1a0163a942ea33148e6937c026029424b81b996b1df22ea0623ec65c6b" +
            "f093500cf3bf35374adc392035ca7c583b99685bca541a0807b163acd0888be0385dea820da46e4dbb44d2e462c734b83a47" +
            "3fed1364273159257cc259a8c5676c1c76d41d56b9907ec1c3599c9e8907403a27a705e3619b04b0ad046e8ec8169c17b460" +
            "d44c0c0c4464d044c946186bc725965083a892bcc495c0540311ff9b3e5192c303d88f8ba46a901c782ef02388f1b2addab6" +
            "a5350fc3639700e3154337337e4a178d351cd2b56ee1f0bfea34aacfa33d2ec791e50752d4d034cb1c951572caaa5c4d9094" +
            "7b6b175a6dd3c62a77bb8f7ac9ae24719b53c2b120a2876986e217b72bd7cee44a7265b11cee1ab2261762b31a3738386969" +
            "c0825fb79452e652e1142fc73c9df6fba411795b4717922b29ba2d53abe5a8c0dcc1601b096c96d7938fd5a68a8797c7b947" +
            "7a86a472eb5da250cb2fec318d83c8f43bbe8e11c35e377d349366c85c4382597f6fc27a0051c0fb00b02c01ca20f9a427f1" +
            "72599477ca690cc1327e0f025f80ec338a80a159e308c12a27db1a7e1b960a99d37dfc22872e51930f28c651ab221f53abae" +
            "e20bad9a3eabcbab913251bf135beb29617b5754333c4daadb2238341c2ad9378186280f6449440b784ba78f5dac44d8f65b" +
            "3b7421950397c3913a2dd23ec6d1cb717b36a5fc95af191e278296948c1254ea86b4ec004b94c29450111191823b3514c9ac" +
            "1ea3d9825ccb86393a2dfb04654fa2192d37bfad1c497c6502eee5ca80a73bfce0baf5a54a88585a401397a3d232f426a7af" +
            "b082bc21a44317090eaac7592c2ea88a653c4491ea193931335f52e989a3c4cc56d9c553732d57c470fb41ab759b65d2d044" +
            "45382fcd9c4e344a1128fa9e11e04358e192ed014b23232a7ee2b22e23717f44111ee33575399c37646da9813ec9b212afe9" +
            "4e5dc5c2330a7294cc1f4234a6d3fbb4f1685ab8892c04acb17cd1c170d7b0611b6a7176c794cc8c67f55fc923c2ad203100" +
            "f365991882c30243d77813843b5ec7c964032263706092ecf00c7516be64e4598ca4226c069bb5e67e4175cf2286c8dd5c48" +
            "8a6c5861f31baa0bd0269470e8b551dd3bcd38c86c12f9cdb176c77dc8b6c02a701f478902c8553f694c0d82727b4c4a5c2c" +
            "1041212aa1274808b82111b377ec75214e9b1978f76004d4139d98613f4b8e98d20af7b534073a509a959b7a7564f9b40ca2" +
            "18bf61829320a8502017954d328d7ac6c769ec29700756e7b0685b340d5e118059504a49a9a50a10198eb10a5784678eb427" +
            "d7b4babb9552933b062897973e1318eaf0a0eac37584a65401b1703e042accd837531483f241cadcd1c1d378119e694429db" +
            "199ac891e4c5343757085bb3ae783667350c4458d97672e861e80b1d2679510ea3a6f2360c77a46942c7a06a554d228080c8" +
            "4b47aef14db17620cb16c06ab30a1be4cda7082be9f87e9c211c46916349a5ba8eaa5201c7294a3c0885b53b657452108825" +
            "ec646c90a04612324ee7d031afe5343132cbef67b6efb1a5ec2809b773538ce77b3d8b04eb0b3c2256011e4c716c19a8ba07" +
            "52bf71492117649f0615c3290fc29a46fde4bd52db9286d603388244259c15a7ac2b640a60cc03376a5841a3fb8a473568fa" +
            "9b1a267215f34c01697b0f0e627175d72105b7707c29b9e614bdc33a6f6c818a95370b427882d7b476796a9ec6eb993274cd" +
            "9b2391a82ba45e3393d2e9ae9721ca9d6c1b988b5827713f90a6585de9433528c02b03ce10bb5f720138d0fbb4c30c1266b9" +
            "18e52925dfe17b37f95d22bca54f475919ac859098c0f0d08ac5875ef29b56fd141e6ef15f700a0b66f39595c588177373c4" +
            "669b21bc071e4c3aa5f0b4a31b6258f35da24ac3cd29c7f2092410c5078355b138fb53a6b9ae6e0b9c08243e7baa45c47376" +
            "eb8c7f13d4cf51aa736fa31540c9241f370da544bf9f9c28d9a57e2f2a7ca95a4e4b466e641ab3bcc76adf1139d567a6f12b" +
            "52f3a65e7ec0aae26bcaa8c55833b04e59998ebc9a1930fbb6d2233c53d2c1f8b9518e3c2de73a19dee6b380a5b32971cf64" +
            "e129fd6c1fa6e75d4a234501e966dd3a540af5c8f4f34a6b4a253ee28492566d5e67c6f55855fcb0506fb06c156744d9a03a" +
            "31a26fa94cad14f157b7f303d07a69c773768fcb4d079c09059703a0c3a94de4b99ea3a2f16583d0f9170a3950db07b4f0bc" +
            "30802927f9f7961b6259892636a9502a2705303637799dd344da451c1cf7bf67840ceb3079ab8c6b8c1927f64053c612450c" +
            "45c9e603bc16666e596b3471e103b6f15447424d17022048111ffbd37e1c670f64f14b8a7b32b94c1a49b45dd2fc38cd5289" +
            "d910ad63602cf5e13042c64ac6797b89fb551ad08e05a92d200cccb7e712ef23c9312cb350f029ab537e287347fd3075ac10" +
            "906a783f1c6c07ccb88f41228c4be1c640f790b5c3a5d5d3ca792495d74bc461562658c07ac600276b924ab5bc9be1f0494c" +
            "b76f82f460a7480972663381e169996061d799859ec54d4f5ca5c411c01db1597b165977669de13a928a34afbac258fea8c4" +
            "764239c9421dc3119bf5b47699206978327b1c5345ef746a7983841f056e2534100ab24d4e9abbd0b17c6a95bd4c3c0e40f6" +
            "9e1612aceeb28b99086c95116e7204273893390bf46b899b36286b0ebf1947bb9884f732ca27da82b19b5dc0cc7f88857149" +
            "10888b2310c4f9319d410b34e6433b9003e2176bb995257456106e8952163b8ba592530cc5aa0aeb43ad398fe9e97baa523d" +
            "7a4431677c3d3af0719e475db85ca95af5089beabeb05b2faab4896ba60f81c88472a57b46a828826a0cdfb446f8189182d2" +
            "bf5eac4ec1cc5deaf599c8a13e48235406d17ffddc8344b6c66984a868aa92fa02227a086950eb0c8701ed58dc628776b983" +
            "882e117561349e5c131a7e116a0463861d7d18663c5627c38c7147ddaadfd48acd7a4535202122232425262728292a2b2c2d" +
            "2e2f303132333435363738393a3b3c3d3e3f").HexToByteArray();

        internal static byte[] MLKem512PrivateSeed => field ??= (
            "00F8418B8BE63D8433058E3AFEBEFA43F434B2393896C2E75750FAFA5E9AE9A4" +
            "226FD2493AF33A6A92282E3FF0E5BA8AB9F09529C53671CBA1D98981DA8FE800").HexToByteArray();

        internal static byte[] MLKem512EncapsulationKey => field ??= (
            "002645126709F87B5C6DF9116BA175020895C5C3031AFBBFCDF95AF2839095396A21F23BB1232091EB8F983BCBC95400D4A3" +
            "35C555187FC2311B79402A261AEB276F1DE4CA62458D5AA772F0C25C3A4824DC095AB63584CF863CD4DCA9736CAAFADC1CA2" +
            "249475946B4BF4B208334EC419C916157E84C9056DE713CD055D69009D295C1C1A6A07E008297BD1B167D4641ED946E0432C" +
            "DD285A5E9794E6D3CF7422012999AC50C42CACC68E659C37CDD93A70166192719CC0E22D1317C1A3F00715004D505A2B6060" +
            "55626421611BCC64425D2F1452E32B8D68B2584A66180D648B8925467DB306DFC39859EAC383AB60B1F9793051C8AE215162" +
            "1166B3F862B52C44CD796602C80F45D3C095749EA9D65EB6024752761B364891C7669441E3AE30E43178D13B2D1C3B3AFC99" +
            "9FD4B444E3591320A766ACCA6C4059B8F34289211F82C5A333EA029154C716B640AAC1BC7D516DFB00881EFA9472725A9BC5" +
            "7DD1781E7A0689C5E399CB1B1F41A031D33C3528D421047A48A29C91222074FCC538814242C78520D201171615518D36CC98" +
            "BC885F2127CE182BA15041B07737A8ABA7ACC4C0A22624D49C4854674A54F11AFFB03D7AE6B90E7413B854A131A6496D0274" +
            "4A66BA7E4B8DF973577EA902E7D1B7CA40CA7AA63F2C04B5D6DA87A735148F003F78395010B11AC9C87E1AB7BDCD8B18CEBA" +
            "7226E6899DD3A7F2937686D52F7BA21D1A439A7DA06B367C240F644058C434337880FFE79DE9F2A277B13B0B82907E22104F" +
            "0134423A3F06167FF8254276922361C0368CBC73BFB8C35CD1C0A79076C420B053E604C7098C74344F9A5094983B17ADD46C" +
            "34984CA317CDED2B0A1ECC12C52124655389FDB813DBBA8B04310259F0A9EE57B03AB04B18079ABFA336FFF854DA70BD8276" +
            "0A39FB3D9BB2837DF052E925449C88680242B73A39AB9D332A9C0371CD06663FC6A65968CA27663E44984B91C8B51A2CB0B6" +
            "364CCE2097DB286E01FB8D2C871472C68117EA6497196F5F56A3CE778D48B687DAF440BB483748B7C2F0889F02DAB06EC64F" +
            "33E59E49931A20084C7E78563A766B5223909DC385C5BC4BD8AFF51B5CC52F60FD181D8AC43537254ABC2F29E8FCB8698CD4").HexToByteArray();

        internal static byte[] MLKem512DecapsulationKey => field ??= (
            "00E5CDA3A6960283140B49775D0111591493BD0A8C002C1701B53442FAB2A6C7C60B7AB64F1B90104460986803C8C464FA4A" +
            "A334513887F28A7A4810073301ED40BACD4B1123D73679F63735D17F2EF39B40B92A0A65A170A7A14721C8D23350FE1B90C9" +
            "13C7955A5E3DDB701417C6C505667377A611AA26F5AC981C4953A058BC51E1519B47780702A65407722B9C32BC4819181B67" +
            "5C05AE55116C96B084E58CAFAA5BA1FB9AA893C2B4C399B6C1503AD7782FF6568EEE402451C874EBB8A4417A2AF04A102ABC" +
            "7D4064BF7E451B81508ECFF32529491126536D64A65142F9AD804881B802433F89C3C38701DCC670628A7E8DCB77F5086C97" +
            "2397D777982B7C420ACA22492720D218B69AAC36A5509FEEAC0324BB9E3263B38251C8BEF95BCADC953749C3F4B886EFC8A8" +
            "80366177D8A45E3C433809C7D5889893C588B5216E3C47683D79318BB90E20EC09F481B0AD77B6433773458C24FDE361AE05" +
            "0BE76959A069530771AC66D4BEA1F4285C462260C48EC894AC444582DA3C1F4DBC03FB6046AEF9BE8DE01A479156DC200FED" +
            "2518E36A7EC4915AF7E59096037C35B5B3F1C4C8CA96C30A372B8D72B54E91B57E8050B464B27AB671B2EC44D6244020D109" +
            "8709AB5A79A2B5CC47CCF3480DB0BE27509C162CCB842671E3D5BB571986BA02548A81668F41BC5F6A7C4F66639262CD37C6" +
            "9FAC6BA024183B1D4902773C82F9156EADF17E7057B8FCCA21D8C895A92B080879B49A001DF8E88FB4F667D069CDCDC23732" +
            "4A51297224DEF542F65904F7FCBC4ED66A1BA90321172C3570BCDA36415DE93522984C0CAB3EF1819A31808B294ACFF1D517" +
            "E045A0D2D59BD1E1374DA3AE97F09D6720C0516B9CC4352A2CD6479309335C300E4596AF5424B5E7D4A5FF06685481407FAB" +
            "8AB32B0CA336886F5892135B6D49906DB9497244C7983AD6A474194329081AB26920724B317DDB59FD43108F97BDDBEA49E6" +
            "C7A681A11ACF374AD5AC01443203A53281C143114A19CE2CE427A78C1B24A603372BC1A3E16ED826639D294047F89CFEE17C" +
            "C1E00CB69A0EA71C20B37A4C208C5FB9CA44002645126709F87B5C6DF9116BA175020895C5C3031AFBBFCDF95AF283909539" +
            "6A21F23BB1232091EB8F983BCBC95400D4A335C555187FC2311B79402A261AEB276F1DE4CA62458D5AA772F0C25C3A4824DC" +
            "095AB63584CF863CD4DCA9736CAAFADC1CA2249475946B4BF4B208334EC419C916157E84C9056DE713CD055D69009D295C1C" +
            "1A6A07E008297BD1B167D4641ED946E0432CDD285A5E9794E6D3CF7422012999AC50C42CACC68E659C37CDD93A7016619271" +
            "9CC0E22D1317C1A3F00715004D505A2B606055626421611BCC64425D2F1452E32B8D68B2584A66180D648B8925467DB306DF" +
            "C39859EAC383AB60B1F9793051C8AE2151621166B3F862B52C44CD796602C80F45D3C095749EA9D65EB6024752761B364891" +
            "C7669441E3AE30E43178D13B2D1C3B3AFC999FD4B444E3591320A766ACCA6C4059B8F34289211F82C5A333EA029154C716B6" +
            "40AAC1BC7D516DFB00881EFA9472725A9BC57DD1781E7A0689C5E399CB1B1F41A031D33C3528D421047A48A29C91222074FC" +
            "C538814242C78520D201171615518D36CC98BC885F2127CE182BA15041B07737A8ABA7ACC4C0A22624D49C4854674A54F11A" +
            "FFB03D7AE6B90E7413B854A131A6496D02744A66BA7E4B8DF973577EA902E7D1B7CA40CA7AA63F2C04B5D6DA87A735148F00" +
            "3F78395010B11AC9C87E1AB7BDCD8B18CEBA7226E6899DD3A7F2937686D52F7BA21D1A439A7DA06B367C240F644058C43433" +
            "7880FFE79DE9F2A277B13B0B82907E22104F0134423A3F06167FF8254276922361C0368CBC73BFB8C35CD1C0A79076C420B0" +
            "53E604C7098C74344F9A5094983B17ADD46C34984CA317CDED2B0A1ECC12C52124655389FDB813DBBA8B04310259F0A9EE57" +
            "B03AB04B18079ABFA336FFF854DA70BD82760A39FB3D9BB2837DF052E925449C88680242B73A39AB9D332A9C0371CD06663F" +
            "C6A65968CA27663E44984B91C8B51A2CB0B6364CCE2097DB286E01FB8D2C871472C68117EA6497196F5F56A3CE778D48B687" +
            "DAF440BB483748B7C2F0889F02DAB06EC64F33E59E49931A20084C7E78563A766B5223909DC385C5BC4BD8AFF51B5CC52F60" +
            "FD181D8AC43537254ABC2F29E8FCB8698CD4C74CD655ECCF259367D8B329389410E01AFB493B6A1D936E1F3D2F8E8CC43B72" +
            "226FD2493AF33A6A92282E3FF0E5BA8AB9F09529C53671CBA1D98981DA8FE800").HexToByteArray();

        internal const string IetfMlKem1024CertificatePem = """
            -----BEGIN CERTIFICATE-----
            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
            -----END CERTIFICATE-----
            """;
    }
}
