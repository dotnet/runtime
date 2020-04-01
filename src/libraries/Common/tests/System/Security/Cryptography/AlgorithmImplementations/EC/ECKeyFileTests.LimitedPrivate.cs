// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract partial class ECKeyFileTests<T>
    {
        private static bool LimitedPrivateKeySupported { get; } = EcDiffieHellman.Tests.ECDiffieHellmanFactory.LimitedPrivateKeySupported;

        [Fact]
        public void ReadWriteNistP521Pkcs8_LimitedPrivate()
        {
            const string base64 = @"
MGACAQAwEAYHKoZIzj0CAQYFK4EEACMESTBHAgEBBEIBpV+HhaVzC67h1rPTAQaf
f9ZNiwTM6lfv1ZYeaPM/q0NUUWbKZVPNOP9xPRKJxpi9fQhrVeAbW9XtJ+NjA3ax
FmY=";

            ReadWriteBase64Pkcs8(base64, EccTestData.GetNistP521Key2(), LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadNistP521EncryptedPkcs8_Pbes2_Aes128_LimitedPrivateKey()
        {
            const string base64 = @"
MIHLMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAiS8R2OYS+H4wICCAAw
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEEB8zZ4/4VXlh4WPKYssZeNEEcBsA
EHOyooViqm3L/Zn04q+v1yzY+OvegfeTDpvSHCepckKEYklMB2K/O47PlH+jojKo
TpRPFq9qLqOb+SrZVk4Ubljzr0u3pkpnJXczE+wGyATXgF1kfPTDKZR9qk5vaeAj
PFzVQfJ396S+yx4IIC4=";

            ReadWriteBase64EncryptedPkcs8(
                base64,
                "qwerty",
                new PbeParameters(
                    PbeEncryptionAlgorithm.TripleDes3KeyPkcs12,
                    HashAlgorithmName.SHA1,
                    12321),
                EccTestData.GetNistP521Key2(),
                LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadNistP521EncryptedPkcs8_Pbes2_Aes128_LimitedPrivateKey_PasswordBytes()
        {
            const string base64 = @"
MIHLMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAiS8R2OYS+H4wICCAAw
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEEB8zZ4/4VXlh4WPKYssZeNEEcBsA
EHOyooViqm3L/Zn04q+v1yzY+OvegfeTDpvSHCepckKEYklMB2K/O47PlH+jojKo
TpRPFq9qLqOb+SrZVk4Ubljzr0u3pkpnJXczE+wGyATXgF1kfPTDKZR9qk5vaeAj
PFzVQfJ396S+yx4IIC4=";

            ReadWriteBase64EncryptedPkcs8(
                base64,
                Encoding.UTF8.GetBytes("qwerty"),
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA1,
                    12321),
                EccTestData.GetNistP521Key2(),
                LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteNistP256ECPrivateKey_LimitedPrivateKey()
        {
            const string base64 = @"
MDECAQEEIHChLC2xaEXtVv9oz8IaRys/BNfWhRv2NJ8tfVs0UrOKoAoGCCqGSM49
AwEH";

            ReadWriteBase64ECPrivateKey(
                base64,
                EccTestData.GetNistP256ReferenceKey(),
                LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteNistP256ExplicitECPrivateKey_LimitedPrivate_NotSupported()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MIIBIgIBAQQgcKEsLbFoRe1W/2jPwhpHKz8E19aFG/Y0ny19WzRSs4qggfowgfcC
AQEwLAYHKoZIzj0BAQIhAP////8AAAABAAAAAAAAAAAAAAAA////////////////
MFsEIP////8AAAABAAAAAAAAAAAAAAAA///////////////8BCBaxjXYqjqT57Pr
vVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMVAMSdNgiG5wSTamZ44ROdJreBn36QBEEE
axfR8uEsQkf4vOblY6RA8ncDfYEt6zOg9KE5RdiYwpZP40Li/hp/m47n60p8D54W
K84zV2sxXs7LtkBoN79R9QIhAP////8AAAAA//////////+85vqtpxeehPO5ysL8
YyVRAgEB",
                EccTestData.GetNistP256ReferenceKeyExplicit(),
                LimitedPrivateKeySupported && SupportsExplicitCurves);
        }

        [Fact]
        public void ReadWriteNistP256ExplicitPkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MIIBMwIBADCCAQMGByqGSM49AgEwgfcCAQEwLAYHKoZIzj0BAQIhAP////8AAAAB
AAAAAAAAAAAAAAAA////////////////MFsEIP////8AAAABAAAAAAAAAAAAAAAA
///////////////8BCBaxjXYqjqT57PrvVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMV
AMSdNgiG5wSTamZ44ROdJreBn36QBEEEaxfR8uEsQkf4vOblY6RA8ncDfYEt6zOg
9KE5RdiYwpZP40Li/hp/m47n60p8D54WK84zV2sxXs7LtkBoN79R9QIhAP////8A
AAAA//////////+85vqtpxeehPO5ysL8YyVRAgEBBCcwJQIBAQQgcKEsLbFoRe1W
/2jPwhpHKz8E19aFG/Y0ny19WzRSs4o=",
                EccTestData.GetNistP256ReferenceKeyExplicit(),
                LimitedPrivateKeySupported && SupportsExplicitCurves);
        }

        [Fact]
        public void ReadWriteBrainpoolKey1ECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                "MCYCAQEEFMXZRFR94RXbJYjcb966O0c+nE2WoAsGCSskAwMCCAEBAQ==",
                EccTestData.BrainpoolP160r1Key1,
                SupportsBrainpool && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteBrainpoolKey1Pkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MDYCAQAwFAYHKoZIzj0CAQYJKyQDAwIIAQEBBBswGQIBAQQUxdlEVH3hFdsliNxv
3ro7Rz6cTZY=",
                EccTestData.BrainpoolP160r1Key1,
                SupportsBrainpool && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteBrainpoolKey1EncryptedPkcs8_LimitedPrivate()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIGbMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAibpes/q40kbQICCAAw
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEEKU1rOHbrpBkttHYwlM7e8gEQBNB
7CJfOdSzyntp2X212/dU3Tu6pa1BEh6hdfljYPnBNRbrSFjzavRhjUoOOEzLgaqr
heDtThcoFBJUsNhEHrc=",
                "chicken",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA384,
                    4096),
                EccTestData.BrainpoolP160r1Key1,
                SupportsBrainpool && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteSect163k1Key1ECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                "MCMCAQEEFQPBmVrfrowFGNwT3+YwS7AQF+akEqAHBgUrgQQAAQ==",
                EccTestData.Sect163k1Key1,
                SupportsSect163k1 && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteSect163k1Key1Pkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MDMCAQAwEAYHKoZIzj0CAQYFK4EEAAEEHDAaAgEBBBUDwZla366MBRjcE9/mMEuw
EBfmpBI=",
                EccTestData.Sect163k1Key1,
                SupportsSect163k1 && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteSect163k1Key1EncryptedPkcs8_LimitedPrivate()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIGbMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAihxqVEJNIIvgICCAAw
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEENKfCUCiZgnSk3NJ1fYNsfsEQEiv
8tmNavm0fpTJFrAikkaj4BOwz87uce+AoMHaI9kH0dHR4oX5L4euffHY9NwYjywd
2OTmoam/Bux6qv2V1vM=",
                "dinner",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA256,
                    7),
                EccTestData.Sect163k1Key1,
                SupportsSect163k1 && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteSect283k1Key1ECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MDICAQEEJAC08a4ef9zUsOggU8CKkIhSsmIx5sAWcPzGw+osXT/tQO3wN6AHBgUr
gQQAEA==",
                EccTestData.Sect283k1Key1,
                SupportsSect283k1 && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteSect283k1Key1Pkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MEICAQAwEAYHKoZIzj0CAQYFK4EEABAEKzApAgEBBCQAtPGuHn/c1LDoIFPAipCI
UrJiMebAFnD8xsPqLF0/7UDt8Dc=",
                EccTestData.Sect283k1Key1,
                SupportsSect283k1 && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteSect283k1Key1EncryptedPkcs8_LimitedPrivate()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIGrMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAjzxZBMGbGUIQICCAAw
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEEAkgh22WW899Po2QL5+Yz4gEUKHh
/hrl7Ia0jUr5dJ++pEOwWgpdvn8zV+6pt2d0w8D3DAJaJNEqgpaqH6uHS/tYJxWS
vW82QOEXDhi1gO24nhx2gUeqVTHjhFq14blAu5l5",
                "Enter PEM pass phrase",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA384,
                    4096),
                EccTestData.Sect283k1Key1,
                SupportsSect283k1 && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteC2pnb163v1ECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                "MCYCAQEEFQD00koUBxIvRFlnvh2TwAk6ZTZ5hqAKBggqhkjOPQMAAQ==",
                EccTestData.C2pnb163v1Key1,
                SupportsC2pnb163v1 && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteC2pnb163v1Pkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MDYCAQAwEwYHKoZIzj0CAQYIKoZIzj0DAAEEHDAaAgEBBBUA9NJKFAcSL0RZZ74d
k8AJOmU2eYY=",
                EccTestData.C2pnb163v1Key1,
                SupportsC2pnb163v1 && LimitedPrivateKeySupported);
        }

        [Fact]
        public void ReadWriteC2pnb163v1EncryptedPkcs8_LimitedPrivate()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIGbMFcGCSqGSIb3DQEFDTBKMCkGCSqGSIb3DQEFDDAcBAhXAZB3O0dcawICCAAw
DAYIKoZIhvcNAgkFADAdBglghkgBZQMEAQIEEKWBssmLHI618uBvF0PA4VoEQIDy
4luj/sC8xYPCCDX8YQ6ppmkq+5aBw9Rwxrp/1wsrkDUhrU1wCN3eV1sFu+OCEdzQ
1N8AhXsRbbNjXWKX25U=",
                "sleepy",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes192Cbc,
                    HashAlgorithmName.SHA512,
                    1024),
                EccTestData.C2pnb163v1Key1,
                SupportsC2pnb163v1 && LimitedPrivateKeySupported);
        }
    }
}
