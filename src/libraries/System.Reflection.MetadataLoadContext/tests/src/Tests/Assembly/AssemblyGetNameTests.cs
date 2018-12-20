// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    // This disambiguating "using" has to be inside the namespace scope - otherwise, the other AssemblyHashAlgorithm enum
    // in System.Reflection takes precedence.
    using AssemblyHashAlgorithm = global::System.Configuration.Assemblies.AssemblyHashAlgorithm;

    //
    // This group of tests checks that Assembly.GetName() returns an AssemblyName compatible with the classic Reflection
    // behavior and that it will always round-trip though LoadFromAssemblyName() to retrieve the Assembly just loaded.
    //
    public static partial class AssemblyTests
    {
        [Fact]
        public static void AssemblyName_GetName_SimpleNameOnly()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleNameOnlyImage);
                AssemblyName an = a.GetName(copiedName: false);

                Assert.Equal("SimpleNameOnly", an.Name);

                Assert.Equal(AssemblyNameFlags.PublicKey, an.Flags);

                Version v = an.Version;
                Assert.NotNull(v);
                Assert.Equal(0, v.Major);
                Assert.Equal(0, v.Minor);
                Assert.Equal(0, v.Build);
                Assert.Equal(0, v.Revision);

                string cultureName = an.CultureName;
                Assert.Equal(string.Empty, cultureName);

                byte[] publicKey = an.GetPublicKey();
                Assert.Equal(0, publicKey.Length);

                Assert.Equal(AssemblyContentType.Default, an.ContentType);
                Assert.Equal(AssemblyHashAlgorithm.SHA1, an.HashAlgorithm);

                Assembly aAgain = lc.LoadFromAssemblyName(an);
                Assert.Equal(a, aAgain);
            }
        }

        [Fact]
        public static void AssemblyName_GetName_Version1_2_65534_4()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_Version1_2_65534_4Image);
                AssemblyName an = a.GetName(copiedName: false);
                Assert.Equal("Version1_2_65534_4", an.Name);

                Version v = an.Version;
                Assert.NotNull(v);
                Assert.Equal(1, v.Major);
                Assert.Equal(2, v.Minor);
                Assert.Equal(65534, v.Build);
                Assert.Equal(4, v.Revision);

                Assembly aAgain = lc.LoadFromAssemblyName(an);
                Assert.Equal(a, aAgain);
            }
        }

        [Fact]
        public static void AssemblyName_GetName_Version1_2_65535_65535()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_Version1_2_65535_65535Image);
                AssemblyName an = a.GetName(copiedName: false);
                Assert.Equal("Version1_2_65535_65535", an.Name);

                Version v = an.Version;
                Assert.NotNull(v);
                Assert.Equal(1, v.Major);
                Assert.Equal(2, v.Minor);
                Assert.Equal(65535, v.Build);
                Assert.Equal(65535, v.Revision);

                Assembly aAgain = lc.LoadFromAssemblyName(an);
                Assert.Equal(a, aAgain);
            }
        }

        [Fact]
        public static void AssemblyName_GetName_CultureFrCh()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_CultureFrChImage);
                AssemblyName an = a.GetName(copiedName: false);
                Assert.Equal("CultureFrCh", an.Name);

                string cultureName = an.CultureName;
                Assert.Equal("fr-CH", cultureName);

                Assembly aAgain = lc.LoadFromAssemblyName(an);
                Assert.Equal(a, aAgain);
            }
        }

        [Fact]
        public static void AssemblyName_GetName_PublicKeyToken1ee753223f71263d()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_PublicKeyToken1ee753223f71263dImage);
                AssemblyName an = a.GetName(copiedName: false);
                Assert.Equal("PublicKeyToken1ee753223f71263d", an.Name);

                Assert.Equal(AssemblyNameFlags.PublicKey, an.Flags);

                byte[] publicKey = an.GetPublicKey();
                Assert.Equal<byte>(TestData.s_PublicKeyToken1ee753223f71263d_Pk, publicKey);

                Assembly aAgain = lc.LoadFromAssemblyName(an);
                Assert.Equal(a, aAgain);
            }
        }

        [Fact]
        public static void AssemblyName_GetName_HashWithSha256()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_HashWithSha256Image);
                AssemblyName an = a.GetName(copiedName: false);
                Assert.Equal("HashWithSha256", an.Name);

                Assert.Equal(AssemblyHashAlgorithm.SHA256, an.HashAlgorithm);

                Assembly aAgain = lc.LoadFromAssemblyName(an);
                Assert.Equal(a, aAgain);
            }
        }
    }
}
