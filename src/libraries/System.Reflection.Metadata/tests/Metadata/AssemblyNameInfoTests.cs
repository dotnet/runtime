// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Metadata.Tests.Metadata
{
    public class AssemblyNameInfoTests
    {
        [Theory]
        [InlineData("MyAssemblyName, Version=1.0.0.0, PublicKeyToken=b77a5c561934e089")]
        public void WithPublicTokenKey(string fullName)
        {
            AssemblyName assemblyName = new AssemblyName(fullName);

            AssemblyNameInfo assemblyNameInfo = AssemblyNameInfo.Parse(fullName.AsSpan());

            Assert.Equal(fullName, assemblyName.FullName);
            Assert.Equal(fullName, assemblyNameInfo.FullName);

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

        [Theory]
        [InlineData(ProcessorArchitecture.MSIL)]
        [InlineData(ProcessorArchitecture.X86)]
        [InlineData(ProcessorArchitecture.IA64)]
        [InlineData(ProcessorArchitecture.Amd64)]
        [InlineData(ProcessorArchitecture.Arm)]
        public void ProcessorArchitectureIsPropagated(ProcessorArchitecture architecture)
        {
            string input = $"Abc, ProcessorArchitecture={architecture}";
            AssemblyNameInfo assemblyNameInfo = AssemblyNameInfo.Parse(input.AsSpan());

            AssemblyName assemblyName = assemblyNameInfo.ToAssemblyName();

            Assert.Equal(architecture, assemblyName.ProcessorArchitecture);
            Assert.Equal(AssemblyContentType.Default, assemblyName.ContentType);
            // By design (desktop compat) AssemblyName.FullName and ToString() do not include ProcessorArchitecture.
            Assert.Equal(assemblyName.FullName, assemblyNameInfo.FullName);
            Assert.DoesNotContain("ProcessorArchitecture", assemblyNameInfo.FullName);
        }

        [Fact]
        public void AssemblyContentTypeIsPropagated()
        {
            const string input = "Abc, ContentType=WindowsRuntime";
            AssemblyNameInfo assemblyNameInfo = AssemblyNameInfo.Parse(input.AsSpan());

            AssemblyName assemblyName = assemblyNameInfo.ToAssemblyName();

            Assert.Equal(AssemblyContentType.WindowsRuntime, assemblyName.ContentType);
            Assert.Equal(ProcessorArchitecture.None, assemblyName.ProcessorArchitecture);
            Assert.Equal(input, assemblyNameInfo.FullName);
            Assert.Equal(assemblyName.FullName, assemblyNameInfo.FullName);
        }

        [Fact]
        public void RetargetableIsPropagated()
        {
            const string input = "Abc, Retargetable=Yes";
            AssemblyNameInfo assemblyNameInfo = AssemblyNameInfo.Parse(input.AsSpan());
            Assert.True((assemblyNameInfo.Flags & AssemblyNameFlags.Retargetable) != 0);

            AssemblyName assemblyName = assemblyNameInfo.ToAssemblyName();

            Assert.True((assemblyName.Flags & AssemblyNameFlags.Retargetable) != 0);
            Assert.Equal(AssemblyContentType.Default, assemblyName.ContentType);
            Assert.Equal(ProcessorArchitecture.None, assemblyName.ProcessorArchitecture);
            Assert.Equal(input, assemblyNameInfo.FullName);
            Assert.Equal(assemblyName.FullName, assemblyNameInfo.FullName);
        }

        [Fact]
        public void EscapedSquareBracketIsNotAllowedInTheName()
            => Assert.False(AssemblyNameInfo.TryParse("Esc\\[aped".AsSpan(), out _));

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
            Assert.Equal(source.GetPublicKeyToken(), fromParsed.GetPublicKeyToken());
        }
    }
}
