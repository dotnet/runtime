// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.Tests;
using System.Security.Cryptography.X509Certificates;

using Xunit;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public static class KemDecryptTests
    {
        public static TheoryData<byte[]> AesKeyWrapDocuments { get; } = new TheoryData<byte[]>
        {
            KemTestDocuments.MlKem768Aes128Wrap,
            KemTestDocuments.MlKem768Aes192Wrap,
            KemTestDocuments.MlKem768,
        };

        public static TheoryData<byte[]> HkdfDocuments { get; } = new TheoryData<byte[]>
        {
            KemTestDocuments.MlKem768HkdfSha256,
            KemTestDocuments.MlKem768,
            KemTestDocuments.MlKem768HkdfSha512,
            KemTestDocuments.MlKem768HkdfSha3_256,
            KemTestDocuments.MlKem768HkdfSha3_384,
            KemTestDocuments.MlKem768HkdfSha3_512,
        };

        public static TheoryData<byte[], byte[]> MlKemParameterSetDocuments { get; } = new TheoryData<byte[], byte[]>
        {
            { KemTestDocuments.MlKem512, MLKemTestData.IetfMlKem512PrivateKeySeed },
            { KemTestDocuments.MlKem768, MLKemTestData.IetfMlKem768PrivateKeySeed },
            { KemTestDocuments.MlKem1024, MLKemTestData.IetfMlKem1024PrivateKeySeed },
        };

        public static TheoryData<byte[], byte[]?> UkmDocuments { get; } = new TheoryData<byte[], byte[]?>
        {
            { KemTestDocuments.MlKem768, null },
            { KemTestDocuments.MlKem768EmptyUkm, [] },
            { KemTestDocuments.MlKem768NonEmptyUkm, [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08] },
        };

        [Theory]
        [MemberData(nameof(AesKeyWrapDocuments))]
        public static void DecryptAesKeyWrapAlgorithm(byte[] encodedMessage)
        {
            Decrypt(encodedMessage, MLKemTestData.IetfMlKem768PrivateKeySeed);
        }

        [Theory]
        [MemberData(nameof(HkdfDocuments))]
        public static void DecryptHkdfAlgorithm(byte[] encodedMessage)
        {
            Decrypt(encodedMessage, MLKemTestData.IetfMlKem768PrivateKeySeed);
        }

        [Theory]
        [MemberData(nameof(MlKemParameterSetDocuments))]
        public static void DecryptMlKemParameterSet(byte[] encodedMessage, byte[] privateKey)
        {
            Decrypt(encodedMessage, privateKey);
        }

        [Theory]
        [MemberData(nameof(UkmDocuments))]
        public static void DecryptUserKeyingMaterial(byte[] encodedMessage, byte[]? expectedUkm)
        {
            EnvelopedCms cms = Decrypt(encodedMessage, MLKemTestData.IetfMlKem768PrivateKeySeed);
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));
            ReadOnlyMemory<byte>? actualUkm = recipientInfo.UserKeyingMaterial;

            if (expectedUkm is null)
            {
                Assert.Null(actualUkm);
            }
            else
            {
                Assert.True(actualUkm.HasValue);
                Assert.Equal<byte>(expectedUkm, actualUkm.Value.ToArray());
            }
        }

        [Fact]
        public static void DecryptWithCertificatePrivateKey()
        {
            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem,
                MLKemTestData.IetfMlKem768PrivateKeySeedPem))
            {
                EnvelopedCms cms = new EnvelopedCms();
                cms.Decode(KemTestDocuments.MlKem768);
                cms.Decrypt(new X509Certificate2Collection(certificate));

                Assert.Equal("hello world!"u8.ToArray(), cms.ContentInfo.Content);
            }
        }

        [Fact]
        public static void DecryptCompositeMLKemNotSupported()
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (TestCompositeMLKem key = new TestCompositeMLKem(CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048))
            {
                Assert.Throws<PlatformNotSupportedException>(() => cms.Decrypt(recipientInfo, key));
            }
        }

        [Fact]
        public static void CompositeMLKemCertificateNotSupported()
        {
            // From https://github.com/lamps-wg/draft-composite-kem/blob/6f6c8a5601cfe8d66730841a413d209add7dc9ed/src/testvectors.json
            const string Certificate = """
                MIISujCCBbegAwIBAgIURHAx+XL1507qW/pajcphiCdpX7kwCwYJYIZIAWUDBAMSMD0xDTALBgNVBAoMBElFVEYxDjAMBgNVBAsM
                BUxBTVBTMRwwGgYDVQQDDBNDb21wb3NpdGUgTUwtS0VNIENBMB4XDTI2MDExNDEyMTUzN1oXDTM2MDExNTEyMTUzN1owRTENMAsG
                A1UECgwESUVURjEOMAwGA1UECwwFTEFNUFMxJDAiBgNVBAMMG2lkLU1MS0VNNzY4LVgyNTUxOS1TSEEzLTI1NjCCBNEwCgYIKwYB
                BQUHBjoDggTBAMkypwPiMcCsS//yuYwUQ4RphG2JAcrGGBeps1wEzXWqKGLZdTbUr7WqrWLSAomDhl31uEmBlmbcJLPpvmxIqPB8
                oUZ1v40QanZ8mkJWyi5Ja6X2ge/mULgJJRXsv8QwuSIAXa6CN6j5Ma6IPuZlA9GgK+i2BkYBFrUSpKShRlRskDIIuKFLiYdHa2+X
                UR/UM2bSWjrLCxbAm3pUvyClzkX7qKH0cTa0vSX5K8kDLVcBmnr6LNtcAzaiQeIVEH2iBgPlTfsrcbqawdHsqjhSC34JasHli8HF
                dnF4R+8MV9cUbyY2z3yhfJK4occnkWMlrfA2QVp1OqZCv6rRd+loOfACInJjnjNWIHDWpuCKJDFTFWHjnWuJFqmiuEjnQB5zqK5L
                Zm1geFeyxI+Tx32IyRtQpt/FIebJtz44KNokgkrTcN/pUA6AUm+irDi0uIHXFzsMfCgRctghskVUgnMqTYuwZ0EgReSWpJwyVPiA
                QK3ZXrTboullw4/gJd2Fo1VFm29hbhFVOOJ1GLpEanA2DYlZNp9oT5nxrBoikwMXL9BFXUlCuhFGnCCgZJLmdnC2Qs7aUnbbScjY
                zroUn+CSgWLiz1hDJfK1AGECrAbihXesTVlEorDSLS6yvG8HM9mRZ1JUFsCKuiQcnvogshXhZ+GxlVTsZjQQPVWMdSS4YGOVYCfG
                p1C7YdrbewCwQEOmLtCxMlu4KGxcA1BbZt7sFWlRqranb/VXLh34kJeFb25YxcvQHU+7BNtJa6Qypwx8lUZzSpIUzhyDh46VC3eq
                lTMnv3zbFHRhkKZAgU/5hkURaR4ztX2jMmzZOVmlbTlkdPI5Wgs6GR0YbNhjNE+2jz6svSTEP6MhTPjhHuCIXfvqHU8nWEdgnS7r
                n+IIpI2AF2O1J3GRwRJnbpT1sVhUom1juyCCiah3z+NIUpIll0bUBkH0l4gZLS8pzcWyYv4sTJ0zWg6BiwXGyzcprK/LNw8cX0sK
                XcCIpKPFmwWrh8w6cgwKuC+lKKrjGxjZsnGCfrYsum94mvbLK4vSv+B4FNVGAEQJSGZZX+EEJzIgtFzsSuPyrwwCqfdazv/6HR3J
                uCLxVYskrIPcNN28VBSzRH9gAwg5xUu0Giwjh7bnDeo6U6irVvnGVF2qOZHgetMzp9jZvHsqNFqrFScmt76Fwe0IPeozxA4Tec65
                yyHwI/dQIyHFChFIHQ6Zss66nFcxgR8rb8JQUADXTcdpZywbEqqXVaoRM7oUQueXFdCCHasMCAEsVebSuog8SHoZUVWkg0xBK4sz
                VRv3p8aCWB9Dy6f3js16hLOmw8MGTGTUOyyDRgVhAy1anJplx6hZCQ5VSvCptj2FoLBGXDMWY1/RRRu8TgGgy7VmBO7xoVXIQJs3
                pLvBKT1DjNmXDRNoQKvBWxqyZLbhj0bJBEbHRDHVOw7FKvtoBLTsWrioU6xFnyITWjj5FszBLpGsdoZbWBRlzHcRCgaGX11TwBMz
                l05GSLgVnPsJTxPxvXwKiBvcT7ZorbiBQ87ysb4nyOFAC7usnYKgnURmArUCiVmQOU7n9IxYpm1R4GasBABFgvlwJqOpXkH1TY23
                VNh+kK015TFUe84G3h0f/if3bPQwTsjwqFijEjAQMA4GA1UdDwEB/wQEAwIFIDALBglghkgBZQMEAxIDggzuACNSe3+fWqBYY4Tk
                Mwl1BsmXtB848mA4ZxOX/AUAyAsJ0qErCIc0yVp7Poco6xvBKYGxMNIO777b8RGvpgxf6yIAG5fAfYBkugDvDH4i1VSt4W5OYYio
                CxYAPvOUyqnMs36RfkqS51XmnehGaHWb2O1QoC2uoaHub1j8baYkD8xnV4u8rk5ZvZ9pwLRB1k4jbAzSwFNFq68+6cVa7ftXWZjZ
                j39Ebmt14gkmfKQxDxxoL2TM0uVcuBq5a8t1DV08yPphna5S+meEZSy+sAWgHF3Hkuov/s28sFUnWIb5vq49w4xTWdAiRgH/KRQm
                +X+uQjYI/1HvXikVL/eU9xsGNRs/JD9hQL4sZAaH4Z8/v/ugm4ZQ7s2G1dwrJyfspymO96f3hkdPeqCgxEZdLfaO8wWB7AspcUZG
                gbQhCsWWg74TRm5kKuL7UuHQTpe7u2LX58BaMozAjNg4K9nMZlOfYwFmTYmsm5W2RF+MUKjzF3OsRTUzeqJhmHlIi/aBhpvgAnJL
                l69n3LZ809nx0y0XsC+N1K/pAYcJR9yXNu+vxDc1BIDxH4bRW62vxujybaAKBQ674dHOIqZ/BnLUcyq6bxP3fxAWEjVr58rZbO77
                50QC5p7YJHQoTBl18VcDoczDO+O6AHR6gWgam0CUgIipy4sWPJpFQ5dQUCBZ9T7oiRPgmVj51HRJWjJNFvwasl5ln+Tj6VYSHTMg
                coV3rEUKXn4j+KDlsMzjeOgW+ROtGKng/IAm1/8Mdp/XxM1zcByaUpwAYirgsnoRQghUDRZneH6bzfGJPqcZ15QSHocINc+5MQOf
                qHc8od8uRCZgo7x04yez9TMZiHy5jWjAYzOnd7GmSw5EuKOp+vnD2lGlRwn+YjkfaWsxMD8JaJwbZYTlK6MfspLOqgFwaNTj/4Zd
                q8JccSKD5/QkofPy2Dlh1TH4jcBQIcM5IAL6s98jxX+HyehcAQjv8Su4QW2KFfPXvjKhVwd2PmrIO38Vd2vDEhRC8TTCT7jeagHV
                B3wIEHErZUXr0qpKTi6DMFZHVof9+bfu0ml4Ui2ks544zS3+rTpj9+dT9m6+b3W26M6weMdED0E6Kj68ivq7gq1pi/ThkTGNuBhS
                p4jQiyx9VuNF4PVZSF7V+mSedm9Ih/KQYtIJ/NJXFrV8Eu7HU+1OmNnBQOrjDFFgXc47FUk+nIjJO8faAD/fUlpzKx3nnJ5nHgX1
                Se8ew3a2gsJSdlywsywNtj8+udtzBbxCjA+vAb+aVLxOoN1UItOLtsCXKa4596YVcr//JE8PNlrOe+6unp72D+8PQNfebWOu2RQq
                ICraT2HTRAXhtdVRupCkITidPR18WUThOEtxmGbkxprl2ZwHSIG7ZWXoSf+LV2VqtnKM7k5Y2NfUAz+kuNpI7YrpO4k/sTcHoOAy
                KxpUvtNhqxmtZZ/LPzuLx8+pCTPQni7RtDlc3XYdu15/ydeYtr2JIQcZnJdJ65upaKyumiEdznDDakIsKJAVYJNddiSmLHqWs+O3
                pJvPBw9UoYPK1GieBw2+ZCcM2LuqLpy/g5nF5aDAs8g6LZ5AU2JYqniHvWqiIhLe9imTdFDcLw9w0wDRomQ1ZgK5p5vNDmGlhpQz
                I/mhnCsAQq0m2O9mq7kdEVVIhs5pwV7/nLsks8v/Dub+A9uDyPkoheIFW7l6k797kO7f94oaRR2qK2J5PkZVBPXSkH05zIL2u9aP
                pYNE483fSLBfO13I+ll6WGlnatodpPKah7PrUmjB76VlDxM2fK3gWp1yGhElLyZReoLBU0+z55rAQBu37u5cEPRnRY/KHqoiesBb
                6Y8v5FBCtsrbvA1J0YwsJhV5Zg3w8JCWqJSGPIj2s4jg8nH9coWloWWGwyyDv8R0wXXvs/PvXvm9ibORqWai0OXcU+pskhMZztWm
                KfU+vJ4xSMew9vgTm5IsdltPOVEAujcPU+WREyh1PkyuYp3F5O4n828Hsge1uYwqHUwmKZYA+UiLq4JH56RZjxQPtGtSqXnDcgbP
                yhtYCnkV6Tnv1bpb9lywECtaBYhAjDv6uiZtDK2ellbXw99AmmE5E6pb0SFgE9WkLeLEjJyMjxWKKNcbcD5+bnAxi68KqukdsCOk
                mByattmvuWlk+C9BdRTqg+rKK2pAHtjXOtY12lCzx0eHspFa7lOA8rTaxUgYdqsao+QxHusVl3zcwRGmNHyu5sgkzg7fOROduQwo
                xv+39Ed9QVC8sTh2Z7ErCRCdg94oVA/Nh7ONODiexlElkXk8SfpS5rSYFZquHrjh+CLMXdPlVPPxCtA1K3Ac2KaR0llOMMFtQA55
                C9U4yK0ePN7dTV3c6rjY4XUzq7xkSXF6Qvmpn6VEjxyZ8IK/jMelUloIQZzDNXruFcLiWCmaI332kQaS6mzh9hCFmbT3BSZNzTGR
                EZ8l/41X1UzyjuAyXRUb87gQJXUsDjuu4ZRqDJNtDvIxx9HosPa90Md3iNwFok4x07rOy0H1h3oYtNFqq/M90XeAlDIlTvyJfisX
                np4T6KtxBiIJ79NM+fo1R0VPTPqI44OnyvWJPG9CEo6uYgUbArE+ug+Iag2F4hsJ2ftSUeC0+x/x7hTffXuX1on68Kg4lFm/9L9c
                eilLhX802uA0kcRqIvGLkCducJoM3fUfIR3C6qkfmFQGpGNOJl9TQz9SRLP5PU4hBP2vZuULVs+bBcP7FxbT4OgaH6vvuS9OgWZJ
                A6Tred6Wy2juSoLx4zH+Ovv5I157L0aQ+scorV16lFdt85LTi0KHIoJKYGEgOnboardRHqr5b+ijfQk7NO5MPA8G+7Z77WFXESpX
                GXDCIepXtXI42GwJo3rhrnv8eNq+oKyZrkcZbzK99dOLVBfz4if8WyFElWGceyTtrqcH9hcm5EvkjzgTYxUnjLarbyoRGTUJqKO8
                bepnY4Z323H6kufkEYOeS5LRd9J7lJ8lQx2zJ7sTkvcKLzPEjvXxyCjIv2K4RuFxHT/E0icU75DN6PkQ0LVtHt0baozj/TqJnnoC
                uaOQqM++/fdlH1tLa9qms63qcPcjBEov0JReqyfcUxfA7lw/mQstbgYJ78DsVQWKSiQd867RyzPLCAfF3j4c720wom5QdeJzAwhy
                8LsiklvRTUl9dw6ddbtENTda+h4Pf/I0pCSHoYEVBxHiPXTIy4mQCxazaL1L7glRn5R/wAu4hjwF/57QbkAz7Dq7x0ae7BQmWPFw
                fVaExLdBBZ+ula9aFB7Z6CVhYelKHoGrbaON7g6HZ9SzY/DxOJ22wXY5wd9JxvFgpL1/w/vBZ/zZs8aWCEIKk2AVIArhdNpHeF65
                It/m94lOjLKw14jXrHnxD0UKF0VsjvhhlKt6vGA3XqSNTfSt24zfwIBAHp4pcs5T4OmtrPPCEyAYuedGGXrZD8zjoXLblbE1w8rP
                BwqJ3BhIVrnenhNWqagobtbcyU+777fGfQFBDVNAU9SUm1Cp4T93efBa/ebw8N/JKQU6Fqhmm0eih5CWE1odR8gk6PHHzZUea53+
                NM6cI2lKpJpWv+CB1kS5ZDAyiA/66BPLSOTJBPlAwr22f4K1X5F0VMLosjkQVoMAiXIL1QygLdHk8vm3Pz9haswJNgdzpZqsowrL
                hTDwZzRWjM5khMFHWjho2uIobB0ybGN+HEfKiYzEgKIepNW80UPjpJUwGp7+FRbuRvw+to/f4AgjYAV05h3vEedd6Kitd2HxxK08
                /Lcn8ILXUzuk9kc9PFatwZtEy9UwYBKBY6XeY+aCb5KYS99XQgxTRiTnpDK7QY5nPGUfHMSHHkzDUWPR7Cve/fJvcnAU5hCVKSS/
                o1rBiMp43MVcHWyNh9taGFt+uyWeadRyYIBFlDnECdqnZ0GHMHB8Nnc09Nb7YvNiIrwQKmouzi7TPx6y7S31YS7LkeBoxW0QDNDg
                +FeJNCsqnqaUYNB+haNGGS46oPKsOvJJAvhEdQQE5XiStdTdIUvLqnTkDeA0GSOMnZVdxjXBlqe4sECzsNQNZHHquyCobcevwkh8
                UjvHk+Q1dbXI2Q0UHLXyIksixWZWM9Ub09M+enacBfEwj+UBtJ64rHYEQY3m4UDxbsKVYEM4BTDpxoI6B4KBHK48GSJAIdOlK40L
                GhTsuDe49C0r2ZIgy+qF+ehiu0r/cdz3+P1M4YOvxwUzwtKscy0cdc1LibRI1EBS8U3USjKTUTrLsN9uHyAtKQ3cFv1yWlG/rTu7
                NXxMOaUcGSB8gmfC8RoBdHRHZEDjI0SysCdZVgnoc4ZRdoxcSvHsDkN7hLQh8lxyGSM5X3l1NqK3oUIRNUrSMe0FXKCr7BXrg+vr
                WnZOHjBosehUdvXFAwhYbnCJ4SFWtOr7DhQahZqtvMrfLVBXZ4KRncNFR4ySlrnnDxdTXWSSmgAAAAAAAAAAAAAAAAcMFR0kKw==
                """;
            const string Document = """
                MIIFYAYJKoZIhvcNAQcDoIIFUTCCBU0CAQMxggUIpIIFBAYLKoZIhvcNAQkQDQMwggTzAgEAMFUwPTENMAsGA1UECgwESUVURjEO
                MAwGA1UECwwFTEFNUFMxHDAaBgNVBAMME0NvbXBvc2l0ZSBNTC1LRU0gQ0ECFERwMfly9edO6lv6Wo3KYYgnaV+5MAoGCCsGAQUF
                BwY6BIIEQPwlsWLBJ5hc9nmSoo0id+KiF8wGPJQubX6GUiNM32orh2C8ilD6MB2sd0nWE6Yi1ngu8YXr8/6jGrf4VJlJpt/AFW7A
                YxL9iJTLE9YE7RSapZOGuzjaL4gZTSGvnKAeRvoAak+b8h0OLjxO0zU80E+XVAEFNozZMDFvc2fbVrQS0HxsKYsCCcCMULvZgHiS
                n74fBNrInqe66Kb3hWa4+484CkTAhWEMQIilOgXn8z9rvHfSq2ABcLXNwewKJ2TPM756Sq3yVY/OBS+02YMka7aRRlqNk6oZi8Gp
                PyY7JClEGghJQWH4zlgQTYMxbKtFPimO+9QDmXCsd5IN0+JkBnZBo5YTXbidF/6AkbwMbbMmqqZo/hOxw9v5uz6/7TxB/Ro2NnCk
                sboOqxTLTrpYyY6jKB/9eXLFAVcnofECyDRygP+28TutH0vROISQ+OSw8fHOhx/FAra0PbaWIy96ZHaA7SpWOkhkCUjOr2In0n6y
                rmVahpEdayf5svOkGJO6UsTvjXMcnLicJvwECUKD4k6uHnRlCmOkdygnhfg10u5AM4Kna60afHFAjjdyr6YsVissf2960KS1G3Io
                jmIOwYkxWR4jNSaH4ge6dVwTWdOaKJMiR5ttUaQYvK/5cjfHpAdHBCiOMx6v9uGhPyvXb0Zg0w4BDNCbcXCk0ssHOsiPvQ2bZytr
                ee0v9oww9EjcIaWExzjTmvevW1ux9VyCpo2xYmyPY9xQMsUqdjDVzIS9RnXIXgxGweAJoA0LD3NhRCPFWd955ddznSGo7MAP9lq7
                NVQB0FFGlMHURwOcjhFEWW6SHHt9e+jZZRbLS8R2PV8zD5smEVNFAjnhl3Q5L20Tx+bkri05pBO0Il5Ywjhxi5XL7z5kWvDVgbO1
                GCr+6Z/vAE7fMZ0BioSGrduDyWfyOKkwPjBOZByaQYvVHVDF4lJdAI1l48rAgxoYamIpMhmsQcBKR/1dxtO1wvT0UsqMfOjYVbno
                08mdaDVxN0YIdP1UFxPTpV3rObLeqMYJLt+JJmzXuWrhTJz+U9hzH7QTUInjMxwCD36MQJXlxJRn4d5MqmZs86Aveuf2hY4aLS9d
                wnx20h8xEie54eo47MNv0VukwUNnUBkxsa4KHHFm2NUz2LDTyZz9YgXapr21UiQQpfxlAO4jPx74LUdNrAQVhnVcEGoxPLQF9ilm
                FPVb9Ql7muBQM0ldcG/kt09aFk9cV6iYILTQ7RuqKhRUeqTMHubRW00eScRqvaSzmnOKC+jUDB312yB17VThmlEIgwBHtEPjytzy
                9M1I0Xkt+8NKMntVklESmGiDf1mZSFS4yXehxM5d00HBHHXGnWBjQ46AFeS7UNEU2SYmb45fiA73Pe957QDiUc8/O37AMbwXyawu
                z4UY8NadQ/VNZXIctZYhPVVn8Cu8Q/l4zuPRcf0JgiX/hnWdxfrQm24751ihMA0GCyqGSIb3DQEJEAMdAgEgMAsGCWCGSAFlAwQB
                LQQoE38MeUYr9a83NDiSiUZLBkxT+ZGKkeSxxEB8pID/NJje0ky4KzJQTzA8BgkqhkiG9w0BBwEwHQYJYIZIAWUDBAEqBBBHF8mZ
                UKv1MnkeqhxpXN1KgBB3KH22OastHUzab9ADNivv
                """;

            using (X509Certificate2 certificate =
                X509CertificateLoader.LoadCertificate(Convert.FromBase64String(Certificate)))
            {
                EnvelopedCms cms = new EnvelopedCms(new ContentInfo("hello world!"u8.ToArray()));
                CmsRecipient recipient = CmsRecipient.CreateForKeyEncapsulation(certificate, []);
                Assert.Throws<PlatformNotSupportedException>(() => cms.Encrypt(recipient));

                cms = new EnvelopedCms();
                cms.Decode(Convert.FromBase64String(Document));
                Assert.Throws<PlatformNotSupportedException>(
                    () => cms.Decrypt(new X509Certificate2Collection(certificate)));
            }
        }

        [Fact]
        public static void DecryptNullArguments()
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem mlKem = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
            using (TestCompositeMLKem compositeMLKem = new TestCompositeMLKem(
                CompositeMLKemAlgorithm.MLKem768WithRsaOaep2048))
            {
                Assert.Throws<ArgumentNullException>(() => cms.Decrypt(null, mlKem));
                Assert.Throws<ArgumentNullException>(() => cms.Decrypt(recipientInfo, (MLKem)null));
                Assert.Throws<ArgumentNullException>(() => cms.Decrypt(null, compositeMLKem));
                Assert.Throws<ArgumentNullException>(() => cms.Decrypt(recipientInfo, (CompositeMLKem)null));
            }
        }

        [Fact]
        public static void DecryptWithCertificateWithoutPrivateKey()
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);

            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => cms.Decrypt(new X509Certificate2Collection(certificate)));
            }
        }

        [Fact]
        public static void DecryptWithDisposedPrivateKey()
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));
            MLKem privateKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed);
            privateKey.Dispose();

            Assert.Throws<ObjectDisposedException>(() => cms.Decrypt(recipientInfo, privateKey));
        }

        [Fact]
        public static void DecryptWithEncapsulationOnlyKey()
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem publicKey = MLKem.ImportSubjectPublicKeyInfo(MLKemTestData.IetfMlKem768Spki))
            {
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, publicKey));
            }
        }

        [Fact]
        public static void DecryptBeforeDecode()
        {
            EnvelopedCms decodedCms = new EnvelopedCms();
            decodedCms.Decode(KemTestDocuments.MlKem768);
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(decodedCms.RecipientInfos));

            using (MLKem privateKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
            {
                EnvelopedCms cms = new EnvelopedCms();
                Assert.Throws<InvalidOperationException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [Fact]
        public static void DecryptAfterEncrypt()
        {
            EnvelopedCms decodedCms = new EnvelopedCms();
            decodedCms.Decode(KemTestDocuments.MlKem768);
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(decodedCms.RecipientInfos));

            using (X509Certificate2 certificate = X509Certificate2.CreateFromPem(
                MLKemTestData.IetfMlKem768CertificatePem))
            using (MLKem privateKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
            {
                EnvelopedCms cms = new EnvelopedCms(new ContentInfo("hello world!"u8.ToArray()));
                cms.Encrypt(new CmsRecipient(certificate));

                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [Fact]
        public static void DecryptTwice()
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem privateKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
            {
                cms.Decrypt(recipientInfo, privateKey);
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [Fact]
        public static void DecryptCorrectKeyAfterWrongKey()
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem wrongKey = MLKem.GenerateKey(MLKemAlgorithm.MLKem768))
            {
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, wrongKey));
            }

            using (MLKem correctKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
            {
                cms.Decrypt(recipientInfo, correctKey);
            }

            Assert.Equal("hello world!"u8.ToArray(), cms.ContentInfo.Content);
        }

        [Fact]
        public static void DecryptWrongPrivateKey()
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem privateKey = MLKem.GenerateKey(MLKemAlgorithm.MLKem768))
            {
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [Fact]
        public static void DecryptTamperedEncryptedKey()
        {
            const string Document = """
                MIIFRgYJKoZIhvcNAQcDoIIFNzCCBTMCAQMxggTupIIE6gYLKoZIhvcNAQkQDQMwggTZAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwPAYJKoZIhvcNAQcB
                MB0GCWCGSAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(Convert.FromBase64String(Document));

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem privateKey = MLKem.ImportPkcs8PrivateKey(MLKemTestData.IetfMlKem768PrivateKeySeed))
            {
                Assert.ThrowsAny<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        [Fact]
        public static void DecryptInvalidVersion()
        {
            const string Document = """
                MIIFRgYJKoZIhvcNAQcDoIIFNzCCBTMCAQMxggTupIIE6gYLKoZIhvcNAQkQDQMwggTZAgEBMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcB
                MB0GCWCGSAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptInvalidKemCiphertextLength()
        {
            const string Document = """
                MIIFRgYJKoZIhvcNAQcDoIIFNzCCBTMCAQMxggTupIIE6gYLKoZIhvcNAQkQDQMwggTZAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAwSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcB
                MB0GCWCGSAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem1024);
        }

        [Fact]
        public static void DecryptInvalidAesKeyWrapLength()
        {
            const string Document = """
                MIIFNQYJKoZIhvcNAQcDoIIFJjCCBSICAQMxggTdpIIE2QYLKoZIhvcNAQkQDQMwggTIAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EFwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAMDwGCSqGSIb3DQEHATAdBglghkgBZQMEASoEEEcX
                yZlQq/UyeR6qHGlc3UqAEHcofbY5qy0dTNpv0AM2K+8=
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptAesKeyWrapLengthTooShort()
        {
            const string Document = """
                MIIFLgYJKoZIhvcNAQcDoIIFHzCCBRsCAQMxggTWpIIE0gYLKoZIhvcNAQkQDQMwggTBAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EEAAAAAAAAAAAAAAAAAAAAAAwPAYJKoZIhvcNAQcBMB0GCWCGSAFlAwQBKgQQRxfJmVCr9TJ5
                HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptAesKeyWrapLengthNotAligned()
        {
            const string Document = """
                MIIFNwYJKoZIhvcNAQcDoIIFKDCCBSQCAQMxggTfpIIE2wYLKoZIhvcNAQkQDQMwggTKAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EGQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAwPAYJKoZIhvcNAQcBMB0GCWCGSAFlAwQBKgQQ
                RxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptAesKeyWrapOidDoesNotMatchKekLength()
        {
            const string Document = """
                MIIFRgYJKoZIhvcNAQcDoIIFNzCCBTMCAQMxggTupIIE6gYLKoZIhvcNAQkQDQMwggTZAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDALBglghkgBZQMEAQUEKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcB
                MB0GCWCGSAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptUnknownKdf()
        {
            const string Document = """
                MIIFPgYJKoZIhvcNAQcDoIIFLzCCBSsCAQMxggTmpIIE4gYLKoZIhvcNAQkQDQMwggTRAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTAFBgMqAwQCASAwCwYJYIZIAWUDBAEtBCgTfwx5Riv1rzc0OJKJRksGTFP5kYqR5LHEQHykgP80mN7STLgrMlBPMDwGCSqGSIb3DQEHATAdBglghkgB
                ZQMEASoEEEcXyZlQq/UyeR6qHGlc3UqAEHcofbY5qy0dTNpv0AM2K+8=
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptUnknownKem()
        {
            const string Document = """
                MIIFQAYJKoZIhvcNAQcDoIIFMTCCBS0CAQMxggTopIIE5AYLKoZIhvcNAQkQDQMwggTTAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAUGAyoDBQSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ1hOmItZ4
                LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8bCmLAgnA
                jFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvBqT8mOyQp
                RBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmOoygf/Xly
                xQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4nCb8BAlC
                g+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3x6QHRwQo
                jjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4MRsHgCaAN
                Cw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJeWMI4cYuV
                y+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo2FW56NPJ
                nWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRInueHqOOzD
                b9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZPXFeomCC0
                0O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNBwRx1xp1g
                Y0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dYoTANBgsq
                hkiG9w0BCRADHQIBIDALBglghkgBZQMEAS0EKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcBMB0GCWCG
                SAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptKemAlgorithmParameters()
        {
            const string Document = """
                MIIFSAYJKoZIhvcNAQcDoIIFOTCCBTUCAQMxggTwpIIE7AYLKoZIhvcNAQkQDQMwggTbAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMA0GCWCGSAFlAwQEAgUABIIEQPwlsWLBJ5hc9nmSoo0id+KiF8wGPJQubX6GUiNM32orh2C8ilD6MB2s
                d0nWE6Yi1ngu8YXr8/6jGrf4VJlJpt/AFW7AYxL9iJTLE9YE7RSapZOGuzjaL4gZTSGvnKAeRvoAak+b8h0OLjxO0zU80E+XVAEFNozZMDFvc2fbVrQS
                0HxsKYsCCcCMULvZgHiSn74fBNrInqe66Kb3hWa4+484CkTAhWEMQIilOgXn8z9rvHfSq2ABcLXNwewKJ2TPM756Sq3yVY/OBS+02YMka7aRRlqNk6oZ
                i8GpPyY7JClEGghJQWH4zlgQTYMxbKtFPimO+9QDmXCsd5IN0+JkBnZBo5YTXbidF/6AkbwMbbMmqqZo/hOxw9v5uz6/7TxB/Ro2NnCksboOqxTLTrpY
                yY6jKB/9eXLFAVcnofECyDRygP+28TutH0vROISQ+OSw8fHOhx/FAra0PbaWIy96ZHaA7SpWOkhkCUjOr2In0n6yrmVahpEdayf5svOkGJO6UsTvjXMc
                nLicJvwECUKD4k6uHnRlCmOkdygnhfg10u5AM4Kna60afHFAjjdyr6YsVissf2960KS1G3IojmIOwYkxWR4jNSaH4ge6dVwTWdOaKJMiR5ttUaQYvK/5
                cjfHpAdHBCiOMx6v9uGhPyvXb0Zg0w4BDNCbcXCk0ssHOsiPvQ2bZytree0v9oww9EjcIaWExzjTmvevW1ux9VyCpo2xYmyPY9xQMsUqdjDVzIS9RnXI
                XgxGweAJoA0LD3NhRCPFWd955ddznSGo7MAP9lq7NVQB0FFGlMHURwOcjhFEWW6SHHt9e+jZZRbLS8R2PV8zD5smEVNFAjnhl3Q5L20Tx+bkri05pBO0
                Il5Ywjhxi5XL7z5kWvDVgbO1GCr+6Z/vAE7fMZ0BioSGrduDyWfyOKkwPjBOZByaQYvVHVDF4lJdAI1l48rAgxoYamIpMhmsQcBKR/1dxtO1wvT0UsqM
                fOjYVbno08mdaDVxN0YIdP1UFxPTpV3rObLeqMYJLt+JJmzXuWrhTJz+U9hzH7QTUInjMxwCD36MQJXlxJRn4d5MqmZs86Aveuf2hY4aLS9dwnx20h8x
                Eie54eo47MNv0VukwUNnUBkxsa4KHHFm2NUz2LDTyZz9YgXapr21UiQQpfxlAO4jPx74LUdNrAQVhnVcEGoxPLQF9ilmFPVb9Ql7muBQM0ldcG/kt09a
                Fk9cV6iYILTQ7RuqKhRUeqTMHubRW00eScRqvaSzmnOKC+jUDB312yB17VThmlEIgwBHtEPjytzy9M1I0Xkt+8NKMntVklESmGiDf1mZSFS4yXehxM5d
                00HBHHXGnWBjQ46AFeS7UNEU2SYmb45fiA73Pe957QDiUc8/O37AMbwXyawuz4UY8NadQ/VNZXIctZYhPVVn8Cu8Q/l4zuPRcf0JgiX/hnWdxfrQm247
                51ihMA0GCyqGSIb3DQEJEAMdAgEgMAsGCWCGSAFlAwQBLQQoE38MeUYr9a83NDiSiUZLBkxT+ZGKkeSxxEB8pID/NJje0ky4KzJQTzA8BgkqhkiG9w0B
                BwEwHQYJYIZIAWUDBAEqBBBHF8mZUKv1MnkeqhxpXN1KgBB3KH22OastHUzab9ADNivv
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptKdfAlgorithmParameters()
        {
            const string Document = """
                MIIFSAYJKoZIhvcNAQcDoIIFOTCCBTUCAQMxggTwpIIE7AYLKoZIhvcNAQkQDQMwggTbAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTAPBgsqhkiG9w0BCRADHQUAAgEgMAsGCWCGSAFlAwQBLQQoE38MeUYr9a83NDiSiUZLBkxT+ZGKkeSxxEB8pID/NJje0ky4KzJQTzA8BgkqhkiG9w0B
                BwEwHQYJYIZIAWUDBAEqBBBHF8mZUKv1MnkeqhxpXN1KgBB3KH22OastHUzab9ADNivv
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptAesKeyWrapAlgorithmParameters()
        {
            const string Document = """
                MIIFSAYJKoZIhvcNAQcDoIIFOTCCBTUCAQMxggTwpIIE7AYLKoZIhvcNAQkQDQMwggTbAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDANBglghkgBZQMEAS0FAAQoE38MeUYr9a83NDiSiUZLBkxT+ZGKkeSxxEB8pID/NJje0ky4KzJQTzA8BgkqhkiG9w0B
                BwEwHQYJYIZIAWUDBAEqBBBHF8mZUKv1MnkeqhxpXN1KgBB3KH22OastHUzab9ADNivv
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptUnknownAesKeyWrap()
        {
            const string Document = """
                MIIFQAYJKoZIhvcNAQcDoIIFMTCCBS0CAQMxggTopIIE5AYLKoZIhvcNAQkQDQMwggTTAgEAMDowIjENMAsGA1UEChMESUVURjERMA8GA1UEAxMITEFN
                UFMgV0cCFBWf/m8i/VzELFJN9v1eKNDeOPNPMAsGCWCGSAFlAwQEAgSCBED8JbFiwSeYXPZ5kqKNInfiohfMBjyULm1+hlIjTN9qK4dgvIpQ+jAdrHdJ
                1hOmItZ4LvGF6/P+oxq3+FSZSabfwBVuwGMS/YiUyxPWBO0UmqWThrs42i+IGU0hr5ygHkb6AGpPm/IdDi48TtM1PNBPl1QBBTaM2TAxb3Nn21a0EtB8
                bCmLAgnAjFC72YB4kp++HwTayJ6nuuim94VmuPuPOApEwIVhDECIpToF5/M/a7x30qtgAXC1zcHsCidkzzO+ekqt8lWPzgUvtNmDJGu2kUZajZOqGYvB
                qT8mOyQpRBoISUFh+M5YEE2DMWyrRT4pjvvUA5lwrHeSDdPiZAZ2QaOWE124nRf+gJG8DG2zJqqmaP4TscPb+bs+v+08Qf0aNjZwpLG6DqsUy066WMmO
                oygf/XlyxQFXJ6HxAsg0coD/tvE7rR9L0TiEkPjksPHxzocfxQK2tD22liMvemR2gO0qVjpIZAlIzq9iJ9J+sq5lWoaRHWsn+bLzpBiTulLE741zHJy4
                nCb8BAlCg+JOrh50ZQpjpHcoJ4X4NdLuQDOCp2utGnxxQI43cq+mLFYrLH9vetCktRtyKI5iDsGJMVkeIzUmh+IHunVcE1nTmiiTIkebbVGkGLyv+XI3
                x6QHRwQojjMer/bhoT8r129GYNMOAQzQm3FwpNLLBzrIj70Nm2cra3ntL/aMMPRI3CGlhMc405r3r1tbsfVcgqaNsWJsj2PcUDLFKnYw1cyEvUZ1yF4M
                RsHgCaANCw9zYUQjxVnfeeXXc50hqOzAD/ZauzVUAdBRRpTB1EcDnI4RRFlukhx7fXvo2WUWy0vEdj1fMw+bJhFTRQI54Zd0OS9tE8fm5K4tOaQTtCJe
                WMI4cYuVy+8+ZFrw1YGztRgq/umf7wBO3zGdAYqEhq3bg8ln8jipMD4wTmQcmkGL1R1QxeJSXQCNZePKwIMaGGpiKTIZrEHASkf9XcbTtcL09FLKjHzo
                2FW56NPJnWg1cTdGCHT9VBcT06Vd6zmy3qjGCS7fiSZs17lq4Uyc/lPYcx+0E1CJ4zMcAg9+jECV5cSUZ+HeTKpmbPOgL3rn9oWOGi0vXcJ8dtIfMRIn
                ueHqOOzDb9FbpMFDZ1AZMbGuChxxZtjVM9iw08mc/WIF2qa9tVIkEKX8ZQDuIz8e+C1HTawEFYZ1XBBqMTy0BfYpZhT1W/UJe5rgUDNJXXBv5LdPWhZP
                XFeomCC00O0bqioUVHqkzB7m0VtNHknEar2ks5pzigvo1Awd9dsgde1U4ZpRCIMAR7RD48rc8vTNSNF5LfvDSjJ7VZJREphog39ZmUhUuMl3ocTOXdNB
                wRx1xp1gY0OOgBXku1DRFNkmJm+OX4gO9z3vee0A4lHPPzt+wDG8F8msLs+FGPDWnUP1TWVyHLWWIT1VZ/ArvEP5eM7j0XH9CYIl/4Z1ncX60JtuO+dY
                oTANBgsqhkiG9w0BCRADHQIBIDAFBgMqAwYEKBN/DHlGK/WvNzQ4kolGSwZMU/mRipHkscRAfKSA/zSY3tJMuCsyUE8wPAYJKoZIhvcNAQcBMB0GCWCG
                SAFlAwQBKgQQRxfJmVCr9TJ5HqocaVzdSoAQdyh9tjmrLR1M2m/QAzYr7w==
                """;

            AssertInvalidDocument(Document, MLKemAlgorithm.MLKem768);
        }

        [Fact]
        public static void DecryptKemAlgorithmDoesNotMatchPrivateKey()
        {
            AssertInvalidDocument(KemTestDocuments.MlKem768, MLKemAlgorithm.MLKem512);
        }

        private static void AssertInvalidDocument(string document, MLKemAlgorithm algorithm)
        {
            AssertInvalidDocument(Convert.FromBase64String(document), algorithm);
        }

        private static void AssertInvalidDocument(byte[] document, MLKemAlgorithm algorithm)
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(document);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (ValidationMLKem privateKey = new ValidationMLKem(algorithm))
            {
                Assert.Throws<CryptographicException>(() => cms.Decrypt(recipientInfo, privateKey));
            }
        }

        private static EnvelopedCms Decrypt(byte[] encodedMessage, byte[] privateKey)
        {
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(encodedMessage);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MLKem mlKem = MLKem.ImportPkcs8PrivateKey(privateKey))
            {
                cms.Decrypt(recipientInfo, mlKem);
            }

            Assert.Equal("hello world!"u8.ToArray(), cms.ContentInfo.Content);
            return cms;
        }

        private sealed class ValidationMLKem : MLKem
        {
            internal ValidationMLKem(MLKemAlgorithm algorithm)
                : base(algorithm)
            {
            }

            protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
                Assert.Fail("Decapsulation should not be attempted.");

            protected override void Dispose(bool disposing)
            {
            }

            protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret) =>
                throw new NotSupportedException();

            protected override void ExportDecapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override void ExportEncapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override void ExportPrivateSeedCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
                throw new NotSupportedException();
        }

        private sealed class TestCompositeMLKem : CompositeMLKem
        {
            internal TestCompositeMLKem(CompositeMLKemAlgorithm algorithm)
                : base(algorithm)
            {
            }

            protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret) =>
                throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
            }

            protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret) =>
                throw new NotSupportedException();

            protected override int ExportDecapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override int ExportEncapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
                throw new NotSupportedException();
        }
    }

    [PlatformSpecific(~TestPlatforms.Windows)]
    public static class KemCustomImplementationTests
    {
        [Fact]
        public static void Decrypt_CustomMLKemInstance()
        {
            // Even if MLKem.IsSupported returns false, a custom implementation of ML-KEM should work.
            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(KemTestDocuments.MlKem768);

            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            using (MockMLKem privateKey = new MockMLKem())
            {
                cms.Decrypt(recipientInfo, privateKey);
            }

            Assert.Equal("hello world!"u8.ToArray(), cms.ContentInfo.Content);
        }

        private sealed class MockMLKem : MLKem
        {
            private static readonly byte[] s_expectedCiphertext = Convert.FromBase64String(
                """
                /CWxYsEnmFz2eZKijSJ34qIXzAY8lC5tfoZSI0zfaiuHYLyKUPowHax3SdYTpiLWeC7xhevz/qMat/hUmUmm38AVbsBjEv2IlMsT
                1gTtFJqlk4a7ONoviBlNIa+coB5G+gBqT5vyHQ4uPE7TNTzQT5dUAQU2jNkwMW9zZ9tWtBLQfGwpiwIJwIxQu9mAeJKfvh8E2sie
                p7ropveFZrj7jzgKRMCFYQxAiKU6BefzP2u8d9KrYAFwtc3B7AonZM8zvnpKrfJVj84FL7TZgyRrtpFGWo2TqhmLwak/JjskKUQa
                CElBYfjOWBBNgzFsq0U+KY771AOZcKx3kg3T4mQGdkGjlhNduJ0X/oCRvAxtsyaqpmj+E7HD2/m7Pr/tPEH9GjY2cKSxug6rFMtO
                uljJjqMoH/15csUBVyeh8QLINHKA/7bxO60fS9E4hJD45LDx8c6HH8UCtrQ9tpYjL3pkdoDtKlY6SGQJSM6vYifSfrKuZVqGkR1r
                J/my86QYk7pSxO+NcxycuJwm/AQJQoPiTq4edGUKY6R3KCeF+DXS7kAzgqdrrRp8cUCON3KvpixWKyx/b3rQpLUbciiOYg7BiTFZ
                HiM1JofiB7p1XBNZ05ookyJHm21RpBi8r/lyN8ekB0cEKI4zHq/24aE/K9dvRmDTDgEM0JtxcKTSywc6yI+9DZtnK2t57S/2jDD0
                SNwhpYTHONOa969bW7H1XIKmjbFibI9j3FAyxSp2MNXMhL1GdcheDEbB4AmgDQsPc2FEI8VZ33nl13OdIajswA/2Wrs1VAHQUUaU
                wdRHA5yOEURZbpIce3176NllFstLxHY9XzMPmyYRU0UCOeGXdDkvbRPH5uSuLTmkE7QiXljCOHGLlcvvPmRa8NWBs7UYKv7pn+8A
                Tt8xnQGKhIat24PJZ/I4qTA+ME5kHJpBi9UdUMXiUl0AjWXjysCDGhhqYikyGaxBwEpH/V3G07XC9PRSyox86NhVuejTyZ1oNXE3
                Rgh0/VQXE9OlXes5st6oxgku34kmbNe5auFMnP5T2HMftBNQieMzHAIPfoxAleXElGfh3kyqZmzzoC965/aFjhotL13CfHbSHzES
                J7nh6jjsw2/RW6TBQ2dQGTGxrgoccWbY1TPYsNPJnP1iBdqmvbVSJBCl/GUA7iM/HvgtR02sBBWGdVwQajE8tAX2KWYU9Vv1CXua
                4FAzSV1wb+S3T1oWT1xXqJggtNDtG6oqFFR6pMwe5tFbTR5JxGq9pLOac4oL6NQMHfXbIHXtVOGaUQiDAEe0Q+PK3PL0zUjReS37
                w0oye1WSURKYaIN/WZlIVLjJd6HEzl3TQcEcdcadYGNDjoAV5LtQ0RTZJiZvjl+IDvc973ntAOJRzz87fsAxvBfJrC7PhRjw1p1D
                9U1lchy1liE9VWfwK7xD+XjO49Fx/QmCJf+GdZ3F+tCbbjvnWKE=
                """);

            private static readonly byte[] s_sharedSecret =
                Convert.FromBase64String("pTct2UThy1KN636LnF86ahCWlweRAKmeYDORxVk4NFs=");

            internal MockMLKem()
                : base(MLKemAlgorithm.MLKem768)
            {
            }

            protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
            {
                Assert.Equal<byte>(s_expectedCiphertext, ciphertext.ToArray());
                s_sharedSecret.CopyTo(sharedSecret);
            }

            protected override void Dispose(bool disposing)
            {
            }

            protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret) =>
                throw new NotSupportedException();

            protected override void ExportDecapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override void ExportEncapsulationKeyCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override void ExportPrivateSeedCore(Span<byte> destination) =>
                throw new NotSupportedException();

            protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
                throw new NotSupportedException();
        }
    }
}
