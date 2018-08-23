// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.DotNet.XUnitExtensions;
using System.Linq;
namespace System.IO.Compression.Tests
{
    public class ZipFile_ExtractToDirectoryFiltered : ZipFileTestBase
    {
        [Fact]
        public static void ExtractFilteredZip()
        {
            using (TempDirectory destinationDirectory = new TempDirectory())
            {
                ZipFile.ExtractToDirectory("Filter.zip", destinationDirectory.Path, (path) => path.EndsWith(".txt"));
                Assert.True(File.Exists(Path.Combine(destinationDirectory.Path, ".txt")));
                Assert.True(File.Exists(Path.Combine(destinationDirectory.Path, ".pkg.txt")));
                Assert.True(File.Exists(Path.Combine(destinationDirectory.Path, ".txt.txt")));
                Assert.True(Directory.EnumerateFileSystemEntries(destinationDirectory.Path).Count() == 3);
            }
        }

    }
}

