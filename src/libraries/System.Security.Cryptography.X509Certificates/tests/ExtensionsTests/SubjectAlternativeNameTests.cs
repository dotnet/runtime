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
        public static void VerifyInvalidDataBehavior(LoadMode loadMode)
        {
            byte[] invalidEncoding = { 0x05, 0x00 };

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
