// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

using Xunit;

using TestOids = System.Security.Cryptography.Pkcs.Tests.Oids;

namespace System.Security.Cryptography.Pkcs.EnvelopedCmsTests.Tests
{
    [PlatformSpecific(~TestPlatforms.Windows)]
    public static class CompositeMLKemDecryptTests
    {
        public static bool IsCompositeMLKemSupported =>
            CompositeMLKem.IsAlgorithmSupported(CompositeMLKemAlgorithm.MLKem768WithX25519);

        public static bool IsRfcCompositeMLKemSupported =>
            CompositeMLKem.IsAlgorithmSupported(CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256);

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsRfcCompositeMLKemSupported))]
        public static void DecryptRfcExampleRecipientInfo()
        {
            // From https://datatracker.ietf.org/doc/html/draft-ietf-lamps-cms-composite-kem-03#appendix-B
            // The RFC uses an AuthEnvelopedData so the contents here are translated to an EnvelopedData.
            const string EnvelopedData = """
                MIIFYAYJKoZIhvcNAQcDoIIFUTCCBU0CAQMxggUIpIIFBAYLKoZIhvcNAQkQDQMwggTzAgEAgBQU8dj+0h9hA2dsdSyX0JSVN6lqsjAKBggrBgEFBQcGOwSC
                BIFUt1yUX/gxlPvzEiFHSbEUv2g4h4tJQD9SNb53SuZxnZBTHamrAToPioHb1lEFkvX7y+OxvPOTeT9RfnWLvdEEZ8GnKxQWUjPjhuprAn5IKCTTMkTMf4EL
                99Ic0o4AfIQZCHaYvg3Xk0ZT81yLAWTCGNy8vigwKoE+R6M0yVZTSQz1dgLHmNY9OT6ebrwhkmWqZWJjKmGNTD5AUrIMifLwTqRVVGc9G+iRH4PKSiDOhrWc
                8KjJZOWwRweHXVYBHUiSkavyNwg43GCP4uJuYX0OwsLGQ+Ucr2cW5kwsxaGFxydw7EGFSHWJFWu0fkFBgMbO3qzY2tB/+pCRRB/aztVTxIK9DEHVt3ZJ8O2A
                PQfSloLcLEHSBH0Y/dq5ZSm5+KLw3dXkswL//YzL7APZkX/6a40kkRcUbFj9iaE1qW7OZwxYpVliimgBLUyaQ+X/+0J/DfMNp0W7Dk6zNHFruCF1EEZ7i1jh
                vlSzDIV1ImVMu1eHgTyxsvuVenJfUWEhh7WLuDNT+OVqpq+JyU7dZW2Sv2aRMCL86PhXMGF9ihZruyRRVVmRXMRyQ1lQvOjCUhn+9QemUnpqOSDC3q2ncykC
                4tKujGC9pRimXaZvk1gbfjcEKhBGRnWrMm9gO+FOW1UlY0reuVZRKoT9uAmvLzfSCp/SP2fsp3QbSdW310+UeottrGiIiz44AplMmMzLaMKg55J/FeXnvhQA
                mD4ipMnvK6vfJEUlrd47TZHneOIeK9MsUiH1Qz8oOQEo6r87GBuFwsHv1/rpRjmbuP4h3irtCj5yyrNNMFUJVH3zOkXRfQE6OsCPDbadv/C65aS5wV7ovcTi
                UltJ5oZa63UgUZNfji+12uqRLRGGcRArj7RnXDk3s4mnxwaMtpezeY//6dbmQVZJO7gI0Gej6sQYp79YGeJadAyDSZFOUAg0DDgaB9hwsUub6ZOdMGvHAtRq
                WCFt+TLSvLRaPxgb2E9Lm6mS86XxKl22Fa4FmKnEMs9cAJUYeoSak8oNLX3u2y2xztU8DUrl0rN+sl4HmSRS4BiOLXJoL0bwFn8PamgCQ+/yxLX88CNYUvmd
                76TVNaR5F2+joiWHURWBs2Sf5BDjAuwbkGHLU1xJKuV6wSbLSepH3hKpCXxfioadhNSq6QPwvG9TpgUUzr4C/cmiBKa+KmZWfVify6yjZWWj4C0QsWkg6iaw
                XFC+gF0G7OPJ7n236yB8M8HUrJLKKU8aD4+sg5/qA4n65DOEotqnpMH16S1iL46zesDFXrnfjZcT3gOg74stkwbIUw9gfYmLqIrbatmCvs07BcrDK33+dnyC
                bl6L30ASDxpJ6EzRHEwJsHwnWRpgMrNyp71Giwns4H7m7KbpRVnxSEPdmpeTHgwG0QWqIjfmejwSjTOrYdxHmG/a+3l+YMtERTtN+896/f4C9obHIP1l1TgY
                J75i8zIqy8NyEwRD1mkOOuOxfyY/k0WZjKJjF/dXuG0K1BUxsRT11X+oLlAj5RdiJ9CH92XhQhzvMb6cMVhmg4AXuyqliVW/Us5uMA0GCyqGSIb3DQEJEAMc
                AgEgMAsGCWCGSAFlAwQBLQQoXROuANkUopFYqO8ysjrl8nuMKTOH475l/D38Gbmdj9xVJSf9QhVLNzA8BgkqhkiG9w0BBwEwHQYJYIZIAWUDBAEqBBAAESIz
                RFVmd4iZqrvM3e7/gBAcbi4o9kaMopTTmesr2ihb
                """;
            const string PrivateKey = """
                MIGEAgEAMAoGCCsGAQUFBwY7BHOImiTrgVkiRNnS3EmMdxHUrh+EHeflRSSQaMrG7NAvf0DDSVt58hvzJz/RuCOE4/8REOs/DZVr
                5gWO2jOyldClMDECAQEEIM/ctKyhCrQyBNhBbpwS5ZkEO1mklf14j5NRtwj3obB+oAoGCCqGSM49AwEH
                """;

            EnvelopedCms cms = new EnvelopedCms();
            cms.Decode(Convert.FromBase64String(EnvelopedData));
            KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

            Assert.Equal(TestOids.MLKem768WithECDiffieHellmanP256Sha3_256, recipientInfo.KeyEncapsulationAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyEncapsulationAlgorithm.Parameters);
            Assert.Equal(1153, recipientInfo.KeyEncapsulationCiphertext.Length);
            Assert.Equal(TestOids.HkdfSha256, recipientInfo.KeyDerivationAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyDerivationAlgorithm.Parameters);
            Assert.Equal(32, recipientInfo.KeyEncryptionKeyLengthInBytes);
            Assert.Null(recipientInfo.UserKeyingMaterial);
            Assert.Equal(TestOids.Aes256Wrap, recipientInfo.KeyEncryptionAlgorithm.Oid.Value);
            Assert.Empty(recipientInfo.KeyEncryptionAlgorithm.Parameters);
            Assert.Equal(40, recipientInfo.EncryptedKey.Length);

            byte[] privateKeyBytes = Convert.FromBase64String(PrivateKey);

            using (CompositeMLKem privateKey = CompositeMLKem.ImportPkcs8PrivateKey(privateKeyBytes))
            {
                cms.Decrypt(recipientInfo, privateKey);
            }

            Assert.Equal("Hello, world!"u8.ToArray(), cms.ContentInfo.Content);
        }

        [ConditionalFact(typeof(CompositeMLKemDecryptTests), nameof(IsCompositeMLKemSupported))]
        public static void CompositeMLKemCertificate()
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
            const string PrivateKey = """
                MHECAQAwCgYIKwYBBQUHBjoEYNjr+gQRhL+Dp1vHrwmR+Ahvq5b16g2Q6gr1bxt1FZy6nNvsi52p9iUBpciix2VKZCZMm8XxpbO0
                Px49FJM734yApQgRuq7ShGZnXu4lRzb0i9Vg7+/t0oZ5jumV4r/CWg==
                """;

            byte[] certificateBytes = Convert.FromBase64String(Certificate);
            byte[] keyBytes = Convert.FromBase64String(PrivateKey);
            byte[] contents = "hello world"u8.ToArray();

            using (X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(certificateBytes))
            using (CompositeMLKem privateKey = CompositeMLKem.ImportPkcs8PrivateKey(keyBytes))
            {
                EnvelopedCms cms = new EnvelopedCms(new ContentInfo(contents));
                CmsRecipient recipient = CmsRecipient.CreateForKeyEncapsulation(certificate, []);
                cms.Encrypt(recipient);
                byte[] encodedMessage = cms.Encode();

                cms = new EnvelopedCms();
                cms.Decode(encodedMessage);
                KemRecipientInfo recipientInfo = Assert.IsType<KemRecipientInfo>(Assert.Single(cms.RecipientInfos));

                Assert.Equal(TestOids.MLKem768WithX25519Sha3_256, recipientInfo.KeyEncapsulationAlgorithm.Oid.Value);
                Assert.Empty(recipientInfo.KeyEncapsulationAlgorithm.Parameters);
                Assert.Equal(1120, recipientInfo.KeyEncapsulationCiphertext.Length);
                Assert.Equal(TestOids.HkdfSha384, recipientInfo.KeyDerivationAlgorithm.Oid.Value);
                Assert.Empty(recipientInfo.KeyDerivationAlgorithm.Parameters);
                Assert.Equal(TestOids.Aes256Wrap, recipientInfo.KeyEncryptionAlgorithm.Oid.Value);
                Assert.Empty(recipientInfo.KeyEncryptionAlgorithm.Parameters);

                cms.Decrypt(recipientInfo, privateKey);
                AssertExtensions.SequenceEqual(contents, cms.ContentInfo.Content);
            }
        }

    }
}
