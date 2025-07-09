// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
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

        [Theory]
        [InlineData("System.IO.Pipelines.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001004b86c4cb78549b34bab61a3b1800e23bfeb5b3ec390074041536a7e3cbd97f5f04cf0f857155a8928eaa29ebfd11cfbbad3ba70efea7bda3226c6a8d370a4cd303f714486b6ebc225985a638471e6ef571cc92a4613c00b8fa65d61ccee0cbe5f36330c9a01f4183559f1bef24cc2917c6d913e3a541333a1d05d9bed22b38cb")]
        [InlineData("System.IO.Pipelines.Tests, PublicKey=null")]
        public void FullNameContainsPublicKey(string withPublicKey)
        {
            AssemblyNameInfo assemblyNameInfo = AssemblyNameInfo.Parse(withPublicKey.AsSpan());
            Assert.Equal(withPublicKey, assemblyNameInfo.FullName);

            AssemblyNameInfo roundTrip = AssemblyNameInfo.Parse(assemblyNameInfo.FullName.AsSpan());
            Assert.Equal(withPublicKey, roundTrip.FullName);
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

        [Fact]
        public void EmptyInputIsInvalid()
        {
            Assert.False(AssemblyNameInfo.TryParse("".AsSpan(), out _));
            Assert.Throws<ArgumentException>(() => AssemblyNameInfo.Parse("".AsSpan()));
        }

        [Theory]
        [InlineData("aA")]
        [InlineData("B-BB")]
        public void ToAssemblyNameReturnsSameCultureNameAsCultureInfo(string input)
        {
            AssemblyNameInfo assemblyNameInfo = AssemblyNameInfo.Parse($"test,culture={input}".AsSpan());
            Assert.Equal(input, assemblyNameInfo.CultureName);
            // When converting to AssemblyName, the culture name is lower-cased
            // by the CultureInfo ctor that calls CultureData.GetCultureData
            // which lowers the name for caching and normalization purposes.
            // It lowers only the part before the `-` character,
            // but not for all configurations (Full Framework, Nano and more).
            Assert.Equal(new CultureInfo(input).Name, assemblyNameInfo.ToAssemblyName().CultureName);
        }

        [Theory]
        // "c" is an invalid culture identifier in Full Framework
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [InlineData('c')]
        [InlineData('C')]
        public void Culture_C_IsMappedToInvariantCulture(char culture)
        {
            AssemblyNameInfo assemblyNameInfo = AssemblyNameInfo.Parse($"name,culture={culture}".AsSpan());
            Assert.Equal(culture.ToString(), assemblyNameInfo.CultureName);
            Assert.Equal("", assemblyNameInfo.ToAssemblyName().CultureName);
            Assert.Equal(CultureInfo.InvariantCulture, assemblyNameInfo.ToAssemblyName().CultureInfo);
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
            Assert.Equal(source.GetPublicKeyToken(), fromParsed.GetPublicKeyToken());
        }
    }
}
