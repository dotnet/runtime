// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

#pragma warning disable CS0618 // Disable obsolete warnings

namespace System.Reflection.Tests
{
    public static class AssemblyAttributesTests
    {
        [Fact]
        public static void AssemblyAlgorithmIdAttributeTests()
        {
            var attr1 = new AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA512);
            Assert.Equal((uint)System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA512, attr1.AlgorithmId);

            var attr2 = new AssemblyAlgorithmIdAttribute(0u);
            Assert.Equal((uint)System.Configuration.Assemblies.AssemblyHashAlgorithm.None, attr2.AlgorithmId);
        }

        [Fact]
        public static void AssemblyCompanyAttributeTests()
        {
            var attr1 = new AssemblyCompanyAttribute(null);
            Assert.Null(attr1.Company);

            var attr2 = new AssemblyCompanyAttribute("My Company");
            Assert.Equal("My Company", attr2.Company);
        }

        [Fact]
        public static void AssemblyConfigurationAttributeTests()
        {
            var attr1 = new AssemblyConfigurationAttribute(null);
            Assert.Null(attr1.Configuration);

            var attr2 = new AssemblyConfigurationAttribute("My Configuration");
            Assert.Equal("My Configuration", attr2.Configuration);
        }

        [Fact]
        public static void AssemblyCopyrightAttributeTests()
        {
            var attr1 = new AssemblyCopyrightAttribute(null);
            Assert.Null(attr1.Copyright);

            var attr2 = new AssemblyCopyrightAttribute("My Copyright");
            Assert.Equal("My Copyright", attr2.Copyright);
        }

        [Fact]
        public static void AssemblyCultureAttributeTests()
        {
            var attr1 = new AssemblyCultureAttribute(null);
            Assert.Null(attr1.Culture);

            var attr2 = new AssemblyCultureAttribute("Czech");
            Assert.Equal("Czech", attr2.Culture);
        }

        [Fact]
        public static void AssemblyDefaultAliasAttributeTests()
        {
            var attr1 = new AssemblyDefaultAliasAttribute(null);
            Assert.Null(attr1.DefaultAlias);

            var attr2 = new AssemblyDefaultAliasAttribute("My Alias");
            Assert.Equal("My Alias", attr2.DefaultAlias);
        }

        [Fact]
        public static void AssemblyDelaySignAttributeTests()
        {
            var attr1 = new AssemblyDelaySignAttribute(false);
            Assert.False(attr1.DelaySign);

            var attr2 = new AssemblyDelaySignAttribute(true);
            Assert.True(attr2.DelaySign);
        }

        [Fact]
        public static void AssemblyDescriptionAttributeTests()
        {
            var attr1 = new AssemblyDescriptionAttribute(null);
            Assert.Null(attr1.Description);

            var attr2 = new AssemblyDescriptionAttribute("My Description");
            Assert.Equal("My Description", attr2.Description);
        }

        [Fact]
        public static void AssemblyFileVersionAttributeTests()
        {
            AssertExtensions.Throws<ArgumentNullException>("version", () => new AssemblyFileVersionAttribute(null));

            var attr = new AssemblyFileVersionAttribute("1.2.3.4.5");
            Assert.Equal("1.2.3.4.5", attr.Version);
        }

        [Fact]
        public static void AssemblyFlagsAttributeTests()
        {
            var attr1 = new AssemblyFlagsAttribute(1);
            Assert.Equal((uint)AssemblyNameFlags.PublicKey, attr1.Flags);

            var attr2 = new AssemblyFlagsAttribute(0u);
            Assert.Equal((uint)AssemblyNameFlags.None, attr2.Flags);

            var attr3 = new AssemblyFlagsAttribute(AssemblyNameFlags.EnableJITcompileTracking);
            Assert.Equal((uint)AssemblyNameFlags.EnableJITcompileTracking, attr3.Flags);
        }

        [Fact]
        public static void AssemblyInformationalVersionAttributeTests()
        {
            var attr1 = new AssemblyInformationalVersionAttribute(null);
            Assert.Null(attr1.InformationalVersion);

            var attr2 = new AssemblyInformationalVersionAttribute("3.4.5.6.7");
            Assert.Equal("3.4.5.6.7", attr2.InformationalVersion);
        }

        [Fact]
        public static void AssemblyKeyFileAttributeTests()
        {
            var attr1 = new AssemblyKeyFileAttribute(null);
            Assert.Null(attr1.KeyFile);

            var attr2 = new AssemblyKeyFileAttribute("KeyFile.snk");
            Assert.Equal("KeyFile.snk", attr2.KeyFile);
        }

        [Fact]
        public static void AssemblyKeyNameAttributeTests()
        {
            var attr1 = new AssemblyKeyNameAttribute(null);
            Assert.Null(attr1.KeyName);

            var attr2 = new AssemblyKeyNameAttribute("My Key");
            Assert.Equal("My Key", attr2.KeyName);
        }

        [Fact]
        public static void AssemblyMetadataAttributeTests()
        {
            var attr1 = new AssemblyMetadataAttribute(null, null);
            Assert.Null(attr1.Key);
            Assert.Null(attr1.Value);

            var attr2 = new AssemblyMetadataAttribute("My Key", "My Value");
            Assert.Equal("My Key", attr2.Key);
            Assert.Equal("My Value", attr2.Value);
        }

        [Fact]
        public static void AssemblyProductAttributeTests()
        {
            var attr1 = new AssemblyProductAttribute(null);
            Assert.Null(attr1.Product);

            var attr2 = new AssemblyProductAttribute(".NET Core");
            Assert.Equal(".NET Core", attr2.Product);
        }

        [Fact]
        public static void AssemblySignatureKeyAttributeTests()
        {
            var attr1 = new AssemblySignatureKeyAttribute(null, null);
            Assert.Null(attr1.Countersignature);
            Assert.Null(attr1.PublicKey);

            var attr2 = new AssemblySignatureKeyAttribute("public_key", "counter_signature");
            Assert.Equal("public_key", attr2.PublicKey);
            Assert.Equal("counter_signature", attr2.Countersignature);
        }

        [Fact]
        public static void AssemblyTitleAttributeTests()
        {
            var attr1 = new AssemblyTitleAttribute(null);
            Assert.Null(attr1.Title);

            var attr2 = new AssemblyTitleAttribute("My Assembly");
            Assert.Equal("My Assembly", attr2.Title);
        }

        [Fact]
        public static void AssemblyTrademarkAttributeTests()
        {
            var attr1 = new AssemblyTrademarkAttribute(null);
            Assert.Null(attr1.Trademark);

            var attr2 = new AssemblyTrademarkAttribute("My Trademark");
            Assert.Equal("My Trademark", attr2.Trademark);
        }

        [Fact]
        public static void AssemblyVersionAttributeTests()
        {
            var attr1 = new AssemblyVersionAttribute(null);
            Assert.Null(attr1.Version);

            var attr2 = new AssemblyVersionAttribute("5.6.7.8.9");
            Assert.Equal("5.6.7.8.9", attr2.Version);
        }
    }
}
