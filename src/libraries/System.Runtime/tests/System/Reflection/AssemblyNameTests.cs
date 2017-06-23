// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public static class AssemblyNameTests
    {
        [Fact]
        public static void Verify_CultureName()
        {
            AssemblyName an = new AssemblyName("MyAssemblyName");
            Assert.Null(an.CultureName);
        }

        [Fact]
        public static void Verify_CodeBase()
        {
            AssemblyName n = new AssemblyName("MyAssemblyName");
            Assert.Null(n.CodeBase);

            n.CodeBase = System.IO.Directory.GetCurrentDirectory();
            Assert.NotNull(n.CodeBase);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "AssemblyName.CodeBase and EscapedCodeBase not supported on UapAot")]
        public static void Verify_EscapedCodeBase()
        {
            AssemblyName n = new AssemblyName("MyAssemblyName");
            Assert.Null(n.EscapedCodeBase);

            n.CodeBase = @"file:///d:/temp/MyAssemblyName1.dll";
            Assert.NotNull(n.EscapedCodeBase);
            Assert.Equal(n.EscapedCodeBase, n.CodeBase);

            n.CodeBase = @"file:///c:/program files/MyAssemblyName.dll";
            Assert.Equal(n.EscapedCodeBase, Uri.EscapeUriString(n.CodeBase));
        }

        [Fact]
        public static void Verify_HashAlgorithm()
        {
            AssemblyName an = new AssemblyName("MyAssemblyName");
            Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.None, an.HashAlgorithm);

            an.HashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1;
            Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1, an.HashAlgorithm);
        }

        [Fact]
        public static void Verify_VersionCompatibility()
        {
            AssemblyName an = new AssemblyName("MyAssemblyName");
            Assert.Equal(System.Configuration.Assemblies.AssemblyVersionCompatibility.SameMachine, an.VersionCompatibility);

            an.VersionCompatibility = System.Configuration.Assemblies.AssemblyVersionCompatibility.SameProcess;
            Assert.Equal(System.Configuration.Assemblies.AssemblyVersionCompatibility.SameProcess, an.VersionCompatibility);
        }

        [Fact]
        public static void Clone()
        {
            AssemblyName an1 = new AssemblyName("MyAssemblyName");
            an1.Flags = AssemblyNameFlags.PublicKey | AssemblyNameFlags.EnableJITcompileOptimizer;

            object an2 = an1.Clone();
            Assert.Equal(an1.FullName, ((AssemblyName)an2).FullName);
            Assert.Equal(AssemblyNameFlags.PublicKey | AssemblyNameFlags.EnableJITcompileOptimizer, ((AssemblyName)an2).Flags);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "AssemblyName.GetAssemblyName() not supported on UapAot")]
        public static void GetAssemblyName()
        {
            AssertExtensions.Throws<ArgumentNullException>("assemblyFile", () => AssemblyName.GetAssemblyName(null));
            AssertExtensions.Throws<ArgumentException>("path", null, () => AssemblyName.GetAssemblyName(string.Empty));
            Assert.Throws<System.IO.FileNotFoundException>(() => AssemblyName.GetAssemblyName("IDontExist"));

            using (var tempFile = new TempFile(Path.GetTempFileName(), 42))
            {
                Assert.Throws<System.BadImageFormatException>(() => AssemblyName.GetAssemblyName(tempFile.Path));
            }

            Assembly a = typeof(AssemblyNameTests).Assembly;
            Assert.Equal(new AssemblyName(a.FullName).ToString(), AssemblyName.GetAssemblyName(a.Location).ToString());
        }

        public static IEnumerable<object[]> ReferenceMatchesDefinition_TestData()
        {
            yield return new object[] { new AssemblyName(typeof(AssemblyNameTests).Assembly.FullName), new AssemblyName(typeof(AssemblyNameTests).Assembly.FullName), true };
            yield return new object[] { new AssemblyName(typeof(AssemblyNameTests).Assembly.FullName), new AssemblyName("System.Runtime"), false };
        }

        [Theory]
        [MemberData(nameof(ReferenceMatchesDefinition_TestData))]
        public static void ReferenceMatchesDefinition(AssemblyName a1, AssemblyName a2, bool expected)
        {
            Assert.Equal(expected, AssemblyName.ReferenceMatchesDefinition(a1, a2));
        }
    }
}
