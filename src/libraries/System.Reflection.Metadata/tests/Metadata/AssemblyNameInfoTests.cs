// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Metadata.Tests.Metadata
{
    public class AssemblyNameInfoTests
    {
        [Theory]
        [InlineData("MyAssemblyName, Version=1.0.0.0, PublicKeyToken=b77a5c561934e089", "MyAssemblyName, Version=1.0.0.0, PublicKeyToken=b77a5c561934e089")]
        public void WithPublicTokenKey(string name, string expectedName)
        {
            AssemblyName assemblyName = new AssemblyName(name);

            AssemblyNameInfo assemblyNameInfo = AssemblyNameInfo.Parse(name.AsSpan());

            Assert.Equal(expectedName, assemblyName.FullName);
            Assert.Equal(expectedName, assemblyNameInfo.FullName);

            Roundtrip(assemblyName);
        }

        [Fact]
        public void NoPublicKeyOrToken()
        {
            AssemblyName source = new AssemblyName();
            source.Name = "test";
            source.Version = new Version(1, 2, 3, 4);
            source.CultureName = "en-US";

            Roundtrip(source);
        }

        static void Roundtrip(AssemblyName source)
        {
            AssemblyNameInfo parsed = AssemblyNameInfo.Parse(source.FullName.AsSpan());
            Assert.Equal(source.Name, parsed.Name);
            Assert.Equal(source.Version, parsed.Version);
            Assert.Equal(source.CultureName, parsed.CultureName);
            Assert.Equal(source.FullName, parsed.FullName);

            AssemblyName fromParsed = parsed.ToAssemblyName();
            Assert.Equal(source.Name, fromParsed.Name);
            Assert.Equal(source.Version, fromParsed.Version);
            Assert.Equal(source.CultureName, fromParsed.CultureName);
            Assert.Equal(source.FullName, fromParsed.FullName);
        }
    }
}
