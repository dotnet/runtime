// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.ExtensionsTests
{
    public static class SubjectAlternativeNameTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            X509SubjectAlternativeNameExtension ext = new X509SubjectAlternativeNameExtension();
            Assert.Empty(ext.RawData);
            Assert.Equal("2.5.29.17", ext.Oid.Value);
            Assert.Empty(ext.EnumerateDnsNames());
            Assert.Empty(ext.EnumerateIPAddresses());
            Assert.False(ext.Critical, "ext.Critical");
        }

        [Fact]
        public static void ArrayCtorRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(
                "rawData",
                () => new X509SubjectAlternativeNameExtension((byte[])null));
        }

        [Theory]
        [InlineData(LoadMode.CopyFrom)]
        [InlineData(LoadMode.Array)]
        [InlineData(LoadMode.Span)]
        public static void EnumerateDnsNames(LoadMode loadMode)
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName("foo");
            builder.AddIpAddress(IPAddress.Loopback);
            builder.AddUserPrincipalName("user@some.domain");
            builder.AddIpAddress(IPAddress.IPv6Loopback);
            builder.AddDnsName("*.foo");
            X509Extension built = builder.Build(true);

            X509SubjectAlternativeNameExtension ext;

            switch (loadMode)
            {
                case LoadMode.CopyFrom:
                    ext = new X509SubjectAlternativeNameExtension();
                    ext.CopyFrom(built);
                    break;
                case LoadMode.Array:
                    ext = new X509SubjectAlternativeNameExtension(built.RawData);
                    break;
                case LoadMode.Span:
                    byte[] tmp = new byte[built.RawData.Length + 2];
                    built.RawData.AsSpan().CopyTo(tmp.AsSpan(1));
                    ext = new X509SubjectAlternativeNameExtension(tmp.AsSpan()[1..^1]);
                    tmp.AsSpan().Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loadMode), loadMode, "Unexpected mode");
            }

            Assert.Equal(new[] { "foo", "*.foo" }, ext.EnumerateDnsNames());
        }

        [Theory]
        [InlineData(LoadMode.CopyFrom)]
        [InlineData(LoadMode.Array)]
        [InlineData(LoadMode.Span)]
        public static void EnumerateIPAddresses(LoadMode loadMode)
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName("foo");
            builder.AddIpAddress(IPAddress.Loopback);
            builder.AddUserPrincipalName("user@some.domain");
            builder.AddIpAddress(IPAddress.IPv6Loopback);
            builder.AddDnsName("*.foo");
            X509Extension built = builder.Build(true);

            X509SubjectAlternativeNameExtension ext;

            switch (loadMode)
            {
                case LoadMode.CopyFrom:
                    ext = new X509SubjectAlternativeNameExtension();
                    ext.CopyFrom(built);
                    break;
                case LoadMode.Array:
                    ext = new X509SubjectAlternativeNameExtension(built.RawData);
                    break;
                case LoadMode.Span:
                    byte[] tmp = new byte[built.RawData.Length + 2];
                    built.RawData.AsSpan().CopyTo(tmp.AsSpan(1));
                    ext = new X509SubjectAlternativeNameExtension(tmp.AsSpan()[1..^1]);
                    tmp.AsSpan().Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loadMode), loadMode, "Unexpected mode");
            }

            Assert.Equal(new[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, ext.EnumerateIPAddresses());
        }

        [Theory]
        [InlineData(LoadMode.CopyFrom)]
        [InlineData(LoadMode.Array)]
        [InlineData(LoadMode.Span)]
        public static void CopyFromAfterLoaded(LoadMode originalLoadMode)
        {
            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName("foo");
            builder.AddIpAddress(IPAddress.Loopback);
            builder.AddUserPrincipalName("user@some.domain");
            builder.AddIpAddress(IPAddress.IPv6Loopback);
            builder.AddDnsName("*.foo");
            X509Extension built = builder.Build(true);

            X509SubjectAlternativeNameExtension ext;

            switch (originalLoadMode)
            {
                case LoadMode.CopyFrom:
                    ext = new X509SubjectAlternativeNameExtension();
                    ext.CopyFrom(built);
                    break;
                case LoadMode.Array:
                    ext = new X509SubjectAlternativeNameExtension(built.RawData, critical: true);
                    break;
                case LoadMode.Span:
                    byte[] tmp = new byte[built.RawData.Length + 2];
                    built.RawData.AsSpan().CopyTo(tmp.AsSpan(1));
                    ext = new X509SubjectAlternativeNameExtension(tmp.AsSpan()[1..^1], critical: true);
                    tmp.AsSpan().Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(originalLoadMode), originalLoadMode, "Unexpected mode");
            }

            Assert.True(ext.Critical, "ext.Critical");
            Assert.Equal(new[] { "foo", "*.foo" }, ext.EnumerateDnsNames());
            Assert.Equal(new[] { IPAddress.Loopback, IPAddress.IPv6Loopback }, ext.EnumerateIPAddresses());

            builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName("a");
            builder.AddDnsName("b");
            builder.AddDnsName("c");
            builder.AddIpAddress(IPAddress.IPv6Loopback);
            ext.CopyFrom(builder.Build());

            Assert.False(ext.Critical, "ext.Critical");
            Assert.Equal(new[] { "a", "b", "c" }, ext.EnumerateDnsNames());
            Assert.Equal(new[] { IPAddress.IPv6Loopback }, ext.EnumerateIPAddresses());
        }

        [Theory]
        [InlineData(LoadMode.CopyFrom)]
        [InlineData(LoadMode.Array)]
        [InlineData(LoadMode.Span)]
        public static void VerifyDecodeFailureBehavior(LoadMode loadMode)
        {
            byte[] invalidEncoding = { 0x05, 0x00 };

            VerifyDecodeFailure(invalidEncoding, loadMode);
        }

        [Theory]
        [InlineData(LoadMode.CopyFrom)]
        [InlineData(LoadMode.Array)]
        [InlineData(LoadMode.Span)]
        public static void VerifyInvalidIPAddressBehavior(LoadMode loadMode)
        {
            // dNSName: foo
            // iPAddress: 127.0.0.1
            // iPAddress: 7F 00 00 01 00 (127.0.0.1 with a trailing 0)
            // UPN: user@some.domain
            // iPAddress: ::1
            // dNSName: *.foo
            byte[] invalidEncoding =
            {
                0x30, 0x4D, 0x82, 0x03, 0x66, 0x6F, 0x6F, 0x87, 0x04, 0x7F, 0x00, 0x00, 0x01, 0x87, 0x05, 0x7F,
                0x00, 0x00, 0x01, 0x00, 0xA0, 0x20, 0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x14,
                0x02, 0x03, 0xA0, 0x12, 0x0C, 0x10, 0x75, 0x73, 0x65, 0x72, 0x40, 0x73, 0x6F, 0x6D, 0x65, 0x2E,
                0x64, 0x6F, 0x6D, 0x61, 0x69, 0x6E, 0x87, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x82, 0x05, 0x2A, 0x2E, 0x66, 0x6F, 0x6F,
            };

            VerifyDecodeFailure(invalidEncoding, loadMode);
        }

        [Theory]
        [InlineData(LoadMode.CopyFrom)]
        [InlineData(LoadMode.Array)]
        [InlineData(LoadMode.Span)]
        public static void VerifyInvalidDnsNameBehavior(LoadMode loadMode)
        {
            // dNSName: foo
            // iPAddress: 127.0.0.1
            // UPN: user@some.domain
            // dNSName: 86 6F 6F ("foo" with the f changed from 66 to 86)
            // iPAddress: ::1
            // dNSName: *.foo
            byte[] invalidEncoding =
            {
                0x30, 0x4B, 0x82, 0x03, 0x66, 0x6F, 0x6F, 0x87, 0x04, 0x7F, 0x00, 0x00, 0x01, 0xA0, 0x20, 0x06,
                0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x14, 0x02, 0x03, 0xA0, 0x12, 0x0C, 0x10, 0x75,
                0x73, 0x65, 0x72, 0x40, 0x73, 0x6F, 0x6D, 0x65, 0x2E, 0x64, 0x6F, 0x6D, 0x61, 0x69, 0x6E, 0x82,
                0x03, 0x86, 0x6F, 0x6F, 0x87, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x82, 0x05, 0x2A, 0x2E, 0x66, 0x6F, 0x6F,
            };

            VerifyDecodeFailure(invalidEncoding, loadMode);
        }

        private static void VerifyDecodeFailure(byte[] invalidEncoding, LoadMode loadMode)
        {
            switch (loadMode)
            {
                case LoadMode.CopyFrom:
                    X509Extension untyped = new X509Extension("0.0", invalidEncoding, true);
                    X509SubjectAlternativeNameExtension ext = new X509SubjectAlternativeNameExtension();

                    // The pattern for X509Extension is that CopyFrom doesn't validate data,
                    // and it blindly accepts the incoming OID.  The semantic properties then throw late.
                    ext.CopyFrom(untyped);
                    Assert.True(ext.Critical);
                    Assert.Equal("0.0", ext.Oid.Value);
                    AssertExtensions.SequenceEqual(invalidEncoding, ext.RawData);
                    Assert.Throws<CryptographicException>(ext.EnumerateDnsNames);
                    Assert.Throws<CryptographicException>(ext.EnumerateIPAddresses);
                    break;
                case LoadMode.Array:
                    // The ctors don't need to be so forgiving, through.
                    Assert.Throws<CryptographicException>(
                        () => new X509SubjectAlternativeNameExtension(invalidEncoding));
                    break;
                case LoadMode.Span:
                    Assert.Throws<CryptographicException>(
                        () => new X509SubjectAlternativeNameExtension(new ReadOnlySpan<byte>(invalidEncoding)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(loadMode), loadMode, "Unexpected mode");
            }
        }

        public enum LoadMode
        {
            CopyFrom,
            Array,
            Span,
        }
    }
}
