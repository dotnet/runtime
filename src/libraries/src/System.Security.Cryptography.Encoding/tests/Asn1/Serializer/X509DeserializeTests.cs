// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public static class X509DeserializeTests
    {
        [Fact]
        public static void ReadMicrosoftDotCom()
        {
            byte[] buf = Convert.FromBase64String(MicrosoftDotComBase64);

            Certificate cert = AsnSerializer.Deserialize<Certificate>(
                buf,
                AsnEncodingRules.DER);

            ref TbsCertificate tbsCertificate = ref cert.TbsCertificate;
            ref SubjectPublicKeyInfo spki = ref tbsCertificate.SubjectPublicKeyInfo;

            Assert.Equal(2, tbsCertificate.Version);
            Assert.Equal("3DF70C5D9903F8D8868B9B8CCF20DF69", tbsCertificate.SerialNumber.ByteArrayToHex());

            Assert.Equal("1.2.840.113549.1.1.11", tbsCertificate.Signature.Algorithm.Value);
            Assert.Equal("0500", tbsCertificate.Signature.Parameters.ByteArrayToHex());
            
            // Issuer goes here

            Assert.Equal(new DateTimeOffset(2014, 10, 15, 0, 0, 0, TimeSpan.Zero), tbsCertificate.Validity.NotBefore.Value);
            Assert.Equal(new DateTimeOffset(2016, 10, 15, 23, 59, 59, TimeSpan.Zero), tbsCertificate.Validity.NotAfter.Value);
            
            // Subject goes here

            Assert.Equal("1.2.840.113549.1.1.1", spki.AlgorithmIdentifier.Algorithm.Value);
            Assert.Equal("0500", spki.AlgorithmIdentifier.Parameters.ByteArrayToHex());
            Assert.Equal(
                "3082010A0282010100A46861FA9D5DB763633BF5A64EF6E7C2C2367F48D2D466" +
                "43A22DFCFCCB24E58A14D0F06BDC956437F2A56BA4BEF70BA361BF12964A0D66" +
                "5AFD84B0F7494C8FA4ABC5FCA2E017C06178AEF2CDAD1B5F18E997A14B965C07" +
                "4E8F564970607276B00583932240FE6E2DD013026F9AE13D7C91CC07C4E1E8E8" +
                "7737DC06EF2B575B89D62EFE46859F8255A123692A706C68122D4DAFE11CB205" +
                "A7B3DE06E553F7B95F978EF8601A8DF819BF32040BDF92A0DE0DF269B4514282" +
                "E17AC69934E8440A48AB9D1F5DF89A502CEF6DFDBE790045BD45E0C94E5CA8AD" +
                "D76A013E9C978440FC8A9E2A9A4940B2460819C3E302AA9C9F355AD754C86D3E" +
                "D77DDAA3DA13810B4D0203010001",
                spki.PublicKey.ByteArrayToHex());

            Assert.Null(tbsCertificate.IssuerUniqueId);
            Assert.Null(tbsCertificate.SubjectUniqueId);
            
            Assert.Equal(8, tbsCertificate.Extensions.Length);
            Assert.Equal("2.5.29.17", tbsCertificate.Extensions[0].ExtnId);
            Assert.Equal("2.5.29.19", tbsCertificate.Extensions[1].ExtnId);
            Assert.Equal("2.5.29.15", tbsCertificate.Extensions[2].ExtnId);
            Assert.Equal("2.5.29.37", tbsCertificate.Extensions[3].ExtnId);
            Assert.Equal("2.5.29.32", tbsCertificate.Extensions[4].ExtnId);
            Assert.Equal("2.5.29.35", tbsCertificate.Extensions[5].ExtnId);
            Assert.Equal("2.5.29.31", tbsCertificate.Extensions[6].ExtnId);
            Assert.Equal("1.3.6.1.5.5.7.1.1", tbsCertificate.Extensions[7].ExtnId);

            Assert.Equal("1.2.840.113549.1.1.11", cert.SignatureAlgorithm.Algorithm.Value);
            Assert.Equal("0500", cert.SignatureAlgorithm.Parameters.ByteArrayToHex());

            Assert.Equal(
                "15F8505B627ED7F9F96707097E93A51E7A7E05A3D420A5C258EC7A1CFE1843EC" +
                "20ACF728AAFA7A1A1BC222A7CDBF4AF90AA26DEEB3909C0B3FB5C78070DAE3D6" +
                "45BFCF840A4A3FDD988C7B3308BFE4EB3FD66C45641E96CA3352DBE2AEB4488A" +
                "64A9C5FB96932BA70059CE92BD278B41299FD213471BD8165F924285AE3ECD66" +
                "6C703885DCA65D24DA66D3AFAE39968521995A4C398C7DF38DFA82A20372F13D" +
                "4A56ADB21B5822549918015647B5F8AC131CC5EB24534D172BC60218A88B65BC" +
                "F71C7F388CE3E0EF697B4203720483BB5794455B597D80D48CD3A1D73CBBC609" +
                "C058767D1FF060A609D7E3D4317079AF0CD0A8A49251AB129157F9894A036487",
                cert.Signature.ByteArrayToHex());
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Certificate
        {
            public TbsCertificate TbsCertificate;
            public AlgorithmIdentifier SignatureAlgorithm;
            [BitString]
            public ReadOnlyMemory<byte> Signature;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TbsCertificate
        {
            [ExpectedTag(0, ExplicitTag = true)]
            [DefaultValue(0x02, 0x01, 0x01)]
            public int Version;

            [Integer]
            public ReadOnlyMemory<byte> SerialNumber;

            public AlgorithmIdentifier Signature;

            [AnyValue]
            public ReadOnlyMemory<byte> Issuer;

            public Validity Validity;

            [AnyValue]
            public ReadOnlyMemory<byte> Subject;

            public SubjectPublicKeyInfo SubjectPublicKeyInfo;

            [ExpectedTag(1), BitString, OptionalValue]
            public ReadOnlyMemory<byte>? IssuerUniqueId;

            [ExpectedTag(2), BitString, OptionalValue]
            public ReadOnlyMemory<byte>? SubjectUniqueId;

            [ExpectedTag(3, ExplicitTag = true), OptionalValue]
            public Extension[] Extensions;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Extension
        {
            [ObjectIdentifier]
            public string ExtnId;

            [DefaultValue(0x01, 0x01, 0x00)]
            public bool Critical;

            [OctetString]
            public ReadOnlyMemory<byte> ExtnValue;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Validity
        {
            public Time NotBefore;
            public Time NotAfter;
        }

        [Choice]
        [StructLayout(LayoutKind.Sequential)]
        public struct Time
        {
            [UtcTime]
            public DateTimeOffset? UtcTime;
            [GeneralizedTime(DisallowFractions = true)]
            public DateTimeOffset? GeneralTime;

            public DateTimeOffset Value => UtcTime ?? GeneralTime.Value;
        }

        private const string MicrosoftDotComBase64 =
            @"
MIIFlDCCBHygAwIBAgIQPfcMXZkD+NiGi5uMzyDfaTANBgkqhkiG9w0BAQsFADB3
MQswCQYDVQQGEwJVUzEdMBsGA1UEChMUU3ltYW50ZWMgQ29ycG9yYXRpb24xHzAd
BgNVBAsTFlN5bWFudGVjIFRydXN0IE5ldHdvcmsxKDAmBgNVBAMTH1N5bWFudGVj
IENsYXNzIDMgRVYgU1NMIENBIC0gRzMwHhcNMTQxMDE1MDAwMDAwWhcNMTYxMDE1
MjM1OTU5WjCCAQ8xEzARBgsrBgEEAYI3PAIBAxMCVVMxGzAZBgsrBgEEAYI3PAIB
AgwKV2FzaGluZ3RvbjEdMBsGA1UEDxMUUHJpdmF0ZSBPcmdhbml6YXRpb24xEjAQ
BgNVBAUTCTYwMDQxMzQ4NTELMAkGA1UEBhMCVVMxDjAMBgNVBBEMBTk4MDUyMRMw
EQYDVQQIDApXYXNoaW5ndG9uMRAwDgYDVQQHDAdSZWRtb25kMRgwFgYDVQQJDA8x
IE1pY3Jvc29mdCBXYXkxHjAcBgNVBAoMFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEO
MAwGA1UECwwFTVNDT00xGjAYBgNVBAMMEXd3dy5taWNyb3NvZnQuY29tMIIBIjAN
BgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEApGhh+p1dt2NjO/WmTvbnwsI2f0jS
1GZDoi38/Msk5YoU0PBr3JVkN/Kla6S+9wujYb8SlkoNZlr9hLD3SUyPpKvF/KLg
F8BheK7yza0bXxjpl6FLllwHTo9WSXBgcnawBYOTIkD+bi3QEwJvmuE9fJHMB8Th
6Oh3N9wG7ytXW4nWLv5GhZ+CVaEjaSpwbGgSLU2v4RyyBaez3gblU/e5X5eO+GAa
jfgZvzIEC9+SoN4N8mm0UUKC4XrGmTToRApIq50fXfiaUCzvbf2+eQBFvUXgyU5c
qK3XagE+nJeEQPyKniqaSUCyRggZw+MCqpyfNVrXVMhtPtd92qPaE4ELTQIDAQAB
o4IBgDCCAXwwMQYDVR0RBCowKIIRd3d3Lm1pY3Jvc29mdC5jb22CE3d3d3FhLm1p
Y3Jvc29mdC5jb20wCQYDVR0TBAIwADAOBgNVHQ8BAf8EBAMCBaAwHQYDVR0lBBYw
FAYIKwYBBQUHAwEGCCsGAQUFBwMCMGYGA1UdIARfMF0wWwYLYIZIAYb4RQEHFwYw
TDAjBggrBgEFBQcCARYXaHR0cHM6Ly9kLnN5bWNiLmNvbS9jcHMwJQYIKwYBBQUH
AgIwGRoXaHR0cHM6Ly9kLnN5bWNiLmNvbS9ycGEwHwYDVR0jBBgwFoAUAVmr5906
C1mmZGPWzyAHV9WR52owKwYDVR0fBCQwIjAgoB6gHIYaaHR0cDovL3NyLnN5bWNi
LmNvbS9zci5jcmwwVwYIKwYBBQUHAQEESzBJMB8GCCsGAQUFBzABhhNodHRwOi8v
c3Iuc3ltY2QuY29tMCYGCCsGAQUFBzAChhpodHRwOi8vc3Iuc3ltY2IuY29tL3Ny
LmNydDANBgkqhkiG9w0BAQsFAAOCAQEAFfhQW2J+1/n5ZwcJfpOlHnp+BaPUIKXC
WOx6HP4YQ+wgrPcoqvp6GhvCIqfNv0r5CqJt7rOQnAs/tceAcNrj1kW/z4QKSj/d
mIx7Mwi/5Os/1mxFZB6WyjNS2+KutEiKZKnF+5aTK6cAWc6SvSeLQSmf0hNHG9gW
X5JCha4+zWZscDiF3KZdJNpm06+uOZaFIZlaTDmMffON+oKiA3LxPUpWrbIbWCJU
mRgBVke1+KwTHMXrJFNNFyvGAhioi2W89xx/OIzj4O9pe0IDcgSDu1eURVtZfYDU
jNOh1zy7xgnAWHZ9H/BgpgnX49QxcHmvDNCopJJRqxKRV/mJSgNkhw==
";
    }
}
