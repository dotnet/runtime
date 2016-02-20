// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.DependencyModel;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextCsvReaderTests
    {
        private DependencyContext Read(string text)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                return new DependencyContextCsvReader().Read(stream);
            }
        }

        [Fact]
        public void GroupsAssetsCorrectlyIntoLibraries()
        {
            var context = Read(@"
""Package"",""runtime.any.System.AppContext"",""4.1.0-rc2-23811"",""sha512-1"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext.dll""
""Package"",""runtime.any.System.AppContext"",""4.1.0-rc2-23811"",""sha512-1"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.Runtime.dll""
");
            context.RuntimeLibraries.Should().HaveCount(1);
            var library = context.RuntimeLibraries.Single();
            library.LibraryType.Should().Be("Package");
            library.PackageName.Should().Be("runtime.any.System.AppContext");
            library.Version.Should().Be("4.1.0-rc2-23811");
            library.Hash.Should().Be("sha512-1");
            library.Assemblies.Should().HaveCount(2).And
                .Contain(a => a.Path == "lib\\dnxcore50\\System.AppContext.dll").And
                .Contain(a => a.Path == "lib\\dnxcore50\\System.Runtime.dll");
        }

        [Fact]
        public void IgnoresAllButRuntimeAssets()
        {
            var context = Read(@"
""Package"",""runtime.any.System.AppContext"",""4.1.0-rc2-23811"",""sha512-1"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext.dll""
""Package"",""runtime.any.System.AppContext"",""4.1.0-rc2-23811"",""sha512-1"",""native"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext2.so""
");
            context.RuntimeLibraries.Should().HaveCount(1);
            var library = context.RuntimeLibraries.Single();
            library.Assemblies.Should().HaveCount(1).And
                .Contain(a => a.Path == "lib\\dnxcore50\\System.AppContext.dll");
        }

        [Fact]
        public void IgnoresNiDllAssemblies()
        {
            var context = Read(@"
""Package"",""runtime.any.System.AppContext"",""4.1.0-rc2-23811"",""sha512-1"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext.dll""
""Package"",""runtime.any.System.AppContext"",""4.1.0-rc2-23811"",""sha512-1"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext.ni.dll""
");
            context.RuntimeLibraries.Should().HaveCount(1);
            var library = context.RuntimeLibraries.Single();
            library.Assemblies.Should().HaveCount(1).And
                .Contain(a => a.Path == "lib\\dnxcore50\\System.AppContext.dll");
        }

        [Fact]
        public void UsesTypeNameVersionAndHashToGroup()
        {
            var context = Read(@"
""Package"",""runtime.any.System.AppContext"",""4.1.0-rc2-23811"",""sha512-1"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext.dll""
""Package"",""runtime.any.System.AppContext"",""4.1.0-rc2-23812"",""sha512-1"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext.dll""
""Package"",""runtime.any.System.AppContext"",""4.1.0-rc2-23811"",""sha512-2"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext.dll""
""Package"",""runtime.any.System.AppContext2"",""4.1.0-rc2-23811"",""sha512-1"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext.dll""
""Project"",""runtime.any.System.AppContext"",""4.1.0-rc2-23811"",""sha512-1"",""runtime"",""System.AppContext"",""lib\\dnxcore50\\System.AppContext.dll""
");
            context.RuntimeLibraries.Should().HaveCount(5);
        }

        [Theory]
        [InlineData("text")]
        [InlineData(" ")]
        [InlineData("\"")]
        [InlineData(@""",""")]
        [InlineData(@"\\")]
        public void ThrowsFormatException(string intput)
        {
            Assert.Throws<FormatException>(() => Read(intput));
        }
    }
}
