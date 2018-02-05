// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Enumeration;
using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class SpecialDirectoryTests : FileSystemTest
    {
        protected virtual string[] GetNames(string directory, EnumerationOptions options)
        {
            return new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => new string(entry.FileName),
                options).ToArray();
        }

        [Fact]
        public void SkippingHiddenFiles()
        {
            string[] paths = GetNames(TestDirectory, new EnumerationOptions { ReturnSpecialDirectories = true });
            Assert.Contains(".", paths);
            Assert.Contains("..", paths);
        }
    }

    public class SpecialDirectoryTests_DirectoryInfo_GetDirectories : SpecialDirectoryTests
    {
        protected override string[] GetNames(string directory, EnumerationOptions options)
        {
            return new DirectoryInfo(directory).GetDirectories("*", options).Select(i => i.Name).ToArray();
        }
    }
}
