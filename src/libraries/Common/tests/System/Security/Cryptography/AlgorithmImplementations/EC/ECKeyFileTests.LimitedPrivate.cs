// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public abstract partial class ECKeyFileTests<T>
    {
        private const int NTE_PERM = unchecked((int)0x80090010);

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ReadWriteNistP256_PreservesKeyUsage_Explicit_LimitedPrivate()
        {
            if (!SupportsExplicitCurves)
            {
                return;
            }

            // This key has a keyUsage set to 0b00000000 (no key usages are valid).
            // Since the CNG PKCS8 import will re-write these keys with Q=(0,0)
            // in the PrivateKeyInfo, we want to make sure that the Attributes
            // are kept.
            const string base64 = @"
MIIBQgIBADCCAQMGByqGSM49AgEwgfcCAQEwLAYHKoZIzj0BAQIhAP////8AAAAB
AAAAAAAAAAAAAAAA////////////////MFsEIP////8AAAABAAAAAAAAAAAAAAAA
///////////////8BCBaxjXYqjqT57PrvVV2mIa8ZR0GsMxTsPY7zjw+J9JgSwMV
AMSdNgiG5wSTamZ44ROdJreBn36QBEEEaxfR8uEsQkf4vOblY6RA8ncDfYEt6zOg
9KE5RdiYwpZP40Li/hp/m47n60p8D54WK84zV2sxXs7LtkBoN79R9QIhAP////8A
AAAA//////////+85vqtpxeehPO5ysL8YyVRAgEBBCcwJQIBAQQgcKEsLbFoRe1W
/2jPwhpHKz8E19aFG/Y0ny19WzRSs4qgDTALBgNVHQ8xBAMCAAA=";

            T key = CreateKey();
            key.ImportPkcs8PrivateKey(Convert.FromBase64String(base64), out _);
            CryptographicException ex = Assert.ThrowsAny<CryptographicException>(() => Exercise(key));
            Assert.Equal(NTE_PERM, ex.HResult);
        }

        [Fact]
        public void ReadWriteNistP521Pkcs8_LimitedPrivate()
        {
            const string base64 = @"
MGACAQAwEAYHKoZIzj0CAQYFK4EEACMESTBHAgEBBEIBpV+HhaVzC67h1rPTAQaf
f9ZNiwTM6lfv1ZYeaPM/q0NUUWbKZVPNOP9xPRKJxpi9fQhrVeAbW9XtJ+NjA3ax
FmY=";

            ReadWriteBase64Pkcs8(base64, EccTestData.GetNistP521Key2(), CanDeriveNewPublicKey);
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
                CanDeriveNewPublicKey);
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
                CanDeriveNewPublicKey);
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
                CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteNistP256ExplicitECPrivateKey_LimitedPrivate()
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
                SupportsExplicitCurves && CanDeriveNewPublicKey);
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
                SupportsExplicitCurves && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteNistP256ExplicitEncryptedPkcs8_LimitedPrivate()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIIBnTBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQIS4D9Fbzp0gQCAggA
MAwGCCqGSIb3DQIJBQAwHQYJYIZIAWUDBAECBBBNE0X1G2z4D96fhP/t6xc1BIIB
QLKzXdUbVqnjlzUS7HQPTmgfxQkvieRm92ot4nTbEztKelQ3M9ijA4ToTaWz4crM
RM4VTFzSAk6c3IIYzc5aFe33r76ootud+YnkKLMtT+zrQOxhYV4vT/dVsfqPaTjk
yBN/spLA/AAetSqqxkG3jLvh3TSx/9ymLVRp10748aNMBK7136V0lOBT9VmJLD/R
rtJTh6Lgx8JIAJpyR7Omjb6uaf0/QInS3bWOEnTHt2kRba4GEahQ/Fw8zDwuBX9V
U4vrY201zbeyqVRsabSaru/xQwDUHA++FmiJuY8p0T3y7u0pKtPkdGTBnYjWqcDc
BSJFRM1hEoL4pr7fCtb4mdnEoWGIG6O7SYr92M3TAxFcYEEMSUJi7TxEAmPAKpYe
hjy6jYfLa1BCJhvq+WbNc7zEb2MfXVhnImaG+XTqXI0c",
                "test",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes128Cbc,
                    HashAlgorithmName.SHA256,
                    1234),
                EccTestData.GetNistP256ReferenceKeyExplicit(),
                SupportsExplicitCurves && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteBrainpoolKey1ECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                "MCYCAQEEFMXZRFR94RXbJYjcb966O0c+nE2WoAsGCSskAwMCCAEBAQ==",
                EccTestData.BrainpoolP160r1Key1,
                SupportsBrainpool && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteBrainpoolKey1Pkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MDYCAQAwFAYHKoZIzj0CAQYJKyQDAwIIAQEBBBswGQIBAQQUxdlEVH3hFdsliNxv
3ro7Rz6cTZY=",
                EccTestData.BrainpoolP160r1Key1,
                SupportsBrainpool && CanDeriveNewPublicKey);
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
                SupportsBrainpool && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteSect163k1Key1ECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                "MCMCAQEEFQPBmVrfrowFGNwT3+YwS7AQF+akEqAHBgUrgQQAAQ==",
                EccTestData.Sect163k1Key1,
                SupportsSect163k1Explicit && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteSect163k1Key1Pkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MDMCAQAwEAYHKoZIzj0CAQYFK4EEAAEEHDAaAgEBBBUDwZla366MBRjcE9/mMEuw
EBfmpBI=",
                EccTestData.Sect163k1Key1,
                SupportsSect163k1 && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteSect163k1Key1ExplicitECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MIHBAgEBBBUDwZla366MBRjcE9/mMEuwEBfmpBKggaQwgaECAQEwJQYHKoZIzj0B
AjAaAgIAowYJKoZIzj0BAgMDMAkCAQMCAQYCAQcwLgQVAAAAAAAAAAAAAAAAAAAA
AAAAAAABBBUAAAAAAAAAAAAAAAAAAAAAAAAAAAEEKwQC/hPAU3u8EayqB9eT3k5t
XlyU7ugCiQcPsF04/1gyHy6ABTbVOMzao9kCFQQAAAAAAAAAAAACAQii4MwNmfil
7wIBAg==",
                EccTestData.Sect163k1Key1Explicit,
                SupportsSect163k1Explicit && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteSect163k1Key1ExplicitPkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MIHRAgEAMIGtBgcqhkjOPQIBMIGhAgEBMCUGByqGSM49AQIwGgICAKMGCSqGSM49
AQIDAzAJAgEDAgEGAgEHMC4EFQAAAAAAAAAAAAAAAAAAAAAAAAAAAQQVAAAAAAAA
AAAAAAAAAAAAAAAAAAABBCsEAv4TwFN7vBGsqgfXk95ObV5clO7oAokHD7BdOP9Y
Mh8ugAU21TjM2qPZAhUEAAAAAAAAAAAAAgEIouDMDZn4pe8CAQIEHDAaAgEBBBUD
wZla366MBRjcE9/mMEuwEBfmpBI=",
                EccTestData.Sect163k1Key1Explicit,
                SupportsSect163k1Explicit && CanDeriveNewPublicKey);
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
                SupportsSect163k1 && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteSect163k1Key1ExplicitEncryptedPkcs8_LimitedPrivate()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIIBPDBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQIY8iZ0ZLe8O8CAggA
MAwGCCqGSIb3DQIJBQAwHQYJYIZIAWUDBAECBBB+R0cFaFSqsTlu68p1La4yBIHg
NU0YrkKbg2TyKi62Uh410kgwE/IHqbfoeQZl9P7MDIrah1hR9yk6DTeJE8WRI2BX
+X5cInMazbVLOIO//WTY90MKq/PE9eJ3jch1VGI2VfHh2V5u/uwJT3z1d4fXTpXc
2iP7btbXJhougcGiOtWMQrZtNdAi4OwIgnW1f4VkIWEf0TUjiC7A74AdgMwnu04u
d4sHylN7CUBYGVAtZ7fHwK0CsyggK/7/IoexhoaTUvzXi3xS8rEjY+5w8OcweCnr
RVA9DXUNz5+yUlfGzgErHYGwRLaLCACU6+WAC34Kkyk=",
                "test",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA256,
                    7),
                EccTestData.Sect163k1Key1Explicit,
                SupportsSect163k1Explicit && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteSect283k1Key1ECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MDICAQEEJAC08a4ef9zUsOggU8CKkIhSsmIx5sAWcPzGw+osXT/tQO3wN6AHBgUr
gQQAEA==",
                EccTestData.Sect283k1Key1,
                SupportsSect283k1 && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteC2pnb163v1ExplicitECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                @"
MIHYAgEBBBUA9NJKFAcSL0RZZ74dk8AJOmU2eYaggbswgbgCAQEwJQYHKoZIzj0B
AjAaAgIAowYJKoZIzj0BAgMDMAkCAQECAQICAQgwRQQVByVGtUNSNKQi4HiWdfQy
yJQ13lJCBBUAyVF9BtUkDTz/OMdLILbNTW+d1NkDFQDSwPsVdghg3vHu9NaW5naH
VhUXVAQrBAevaZiVRhA9eTKfzD10iA8zu+gDywHsIyEbWWat6h0/h/fqWEiu8LfK
nwIVBAAAAAAAAAAAAAHmD8iCHMdNrq/BAgEC",
                EccTestData.C2pnb163v1Key1Explicit,
                SupportsC2pnb163v1Explicit && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteC2pnb163v1ExplicitPkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MIHoAgEAMIHEBgcqhkjOPQIBMIG4AgEBMCUGByqGSM49AQIwGgICAKMGCSqGSM49
AQIDAzAJAgEBAgECAgEIMEUEFQclRrVDUjSkIuB4lnX0MsiUNd5SQgQVAMlRfQbV
JA08/zjHSyC2zU1vndTZAxUA0sD7FXYIYN7x7vTWluZ2h1YVF1QEKwQHr2mYlUYQ
PXkyn8w9dIgPM7voA8sB7CMhG1lmreodP4f36lhIrvC3yp8CFQQAAAAAAAAAAAAB
5g/IghzHTa6vwQIBAgQcMBoCAQEEFQD00koUBxIvRFlnvh2TwAk6ZTZ5hg==",
                EccTestData.C2pnb163v1Key1Explicit,
                SupportsC2pnb163v1Explicit && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteC2pnb163v1ExplicitEncryptedPkcs8_LimitedPrivate()
        {
            ReadWriteBase64EncryptedPkcs8(
                @"
MIIBTDBXBgkqhkiG9w0BBQ0wSjApBgkqhkiG9w0BBQwwHAQIvcAOWkixD/4CAggA
MAwGCCqGSIb3DQIJBQAwHQYJYIZIAWUDBAECBBCx4zH4H0Pf9XGdJMtik+XVBIHw
y5JKEMkohGZgjTHkXUs9hSq9JtyJzz8VcSXpid7NkRXFAtEEcO1yIs2xUVxlPER7
4loKRPmPR9GKCeTEsoUyQH9T+X6r0nKqvuoWq5iU8w3ZGrQ8FUBsODMdCAlmfJau
cIB+jp8kGPDQckBBp+R4i2qPYRSKzANEHegDeu9s24IQk2+B3b5uqynkVJa2z+Dp
fyL21cPvHEx04p39oKmWh7S5M6FjHAu/9eGHQtiJ/QKisMgE1ICf+OmO6nfFhNnZ
AerBJbccwFJfDAXP+eW3qWtaMgulL0gUYZQ7FcXH+z5CAWwdarLOCDZGqvQFtZ16",
                "meow",
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA256,
                    7),
                EccTestData.C2pnb163v1Key1Explicit,
                SupportsC2pnb163v1Explicit && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteSect283k1Key1Pkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MEICAQAwEAYHKoZIzj0CAQYFK4EEABAEKzApAgEBBCQAtPGuHn/c1LDoIFPAipCI
UrJiMebAFnD8xsPqLF0/7UDt8Dc=",
                EccTestData.Sect283k1Key1,
                SupportsSect283k1 && CanDeriveNewPublicKey);
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
                SupportsSect283k1 && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteC2pnb163v1ECPrivateKey_LimitedPrivate()
        {
            ReadWriteBase64ECPrivateKey(
                "MCYCAQEEFQD00koUBxIvRFlnvh2TwAk6ZTZ5hqAKBggqhkjOPQMAAQ==",
                EccTestData.C2pnb163v1Key1,
                SupportsC2pnb163v1 && CanDeriveNewPublicKey);
        }

        [Fact]
        public void ReadWriteC2pnb163v1Pkcs8_LimitedPrivate()
        {
            ReadWriteBase64Pkcs8(
                @"
MDYCAQAwEwYHKoZIzj0CAQYIKoZIzj0DAAEEHDAaAgEBBBUA9NJKFAcSL0RZZ74d
k8AJOmU2eYY=",
                EccTestData.C2pnb163v1Key1,
                SupportsC2pnb163v1 && CanDeriveNewPublicKey);
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
                SupportsC2pnb163v1 && CanDeriveNewPublicKey);
        }
    }
}
