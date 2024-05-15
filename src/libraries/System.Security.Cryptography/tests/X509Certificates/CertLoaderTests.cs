// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public class CertLoaderTests
    {
        public class NewLoaderFromArray : CommonTests
        {
            protected override X509Certificate2 LoadCertificate(byte[] data) =>
                X509CertificateLoader.LoadCertificate(data);
        }

        public class NewLoaderFromSpan : CommonTests
        {
            protected override X509Certificate2 LoadCertificate(byte[] data) =>
                X509CertificateLoader.LoadCertificate(new ReadOnlySpan<byte>(data));
        }

        public class NewLoaderFromFile : CommonTests
        {
            protected override X509Certificate2 LoadCertificate(byte[] data)
            {
                using (TempFileHolder holder = new TempFileHolder(data))
                {
                    return X509CertificateLoader.LoadCertificateFromFile(holder.FilePath);
                }
            }
        }

        public class LegacyLoaderFromArray : CommonTests
        {
            protected override X509Certificate2 LoadCertificate(byte[] data) =>
                new X509Certificate2(data);
        }

        [ActiveIssue("NestedCertificates tests fail", TestPlatforms.Linux)]
        public class LegacyLoaderFromFile : CommonTests
        {
            protected override X509Certificate2 LoadCertificate(byte[] data)
            {
                using (TempFileHolder holder = new TempFileHolder(data))
                {
                    return new X509Certificate2(holder.FilePath);
                }
            }
        }

        public abstract class CommonTests
        {
            protected abstract X509Certificate2 LoadCertificate(byte[] data);

            [Fact]
            public void LoadWrappingCertificate_DER()
            {
                using (X509Certificate2 cert = LoadCertificate(TestData.NestedCertificates))
                {
                    AssertExtensions.SequenceEqual(TestData.NestedCertificates, cert.RawDataMemory.Span);
                }
            }

            [Fact]
            public void LoadWrappingCertificate_PEM()
            {
                byte[] data = System.Text.Encoding.ASCII.GetBytes(
                    PemEncoding.Write("CERTIFICATE", TestData.NestedCertificates));

                using (X509Certificate2 cert = LoadCertificate(data))
                {
                    AssertExtensions.SequenceEqual(TestData.NestedCertificates, cert.RawDataMemory.Span);
                }
            }

            [Fact]
            public void LoadWrappingCertificate_DER_Trailing()
            {
                byte[] source = TestData.NestedCertificates;
                byte[] data = new byte[source.Length + 5];
                Array.Copy(source, 0, data, 0, source.Length);
                RandomNumberGenerator.Fill(data.AsSpan(source.Length));

                using (X509Certificate2 cert = LoadCertificate(data))
                {
                    AssertExtensions.SequenceEqual(TestData.NestedCertificates, cert.RawDataMemory.Span);
                }
            }

            [Fact]
            [ActiveIssue("Fails as NewFile, NewSpan, NewArray, LegacyArray", TestPlatforms.Linux)]
            public void LoadWrappingCertificate_PEM_TrailingInner()
            {
                byte[] source = TestData.NestedCertificates;
                byte[] data = new byte[source.Length + 5];
                Array.Copy(source, 0, data, 0, source.Length);
                RandomNumberGenerator.Fill(data.AsSpan(source.Length));

                data = System.Text.Encoding.ASCII.GetBytes(
                    PemEncoding.Write("CERTIFICATE", data));

                using (X509Certificate2 cert = LoadCertificate(data))
                {
                    AssertExtensions.SequenceEqual(TestData.NestedCertificates, cert.RawDataMemory.Span);
                }
            }

            [Fact]
            [ActiveIssue("Fails as NewFile, NewSpan, NewArray, LegacyArray", TestPlatforms.Linux)]
            public void LoadWrappingCertificate_PEM_Surround()
            {
                string pem = PemEncoding.WriteString("CERTIFICATE", TestData.NestedCertificates);

                byte[] data = System.Text.Encoding.ASCII.GetBytes(
                    "Four score and seven years ago ...\n" + pem + "... perish from this Earth.");

                using (X509Certificate2 cert = LoadCertificate(data))
                {
                    AssertExtensions.SequenceEqual(TestData.NestedCertificates, cert.RawDataMemory.Span);
                }
            }
        }
    }
}
