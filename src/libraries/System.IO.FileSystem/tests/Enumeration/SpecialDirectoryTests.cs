// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using Xunit;

namespace System.IO.Tests.Enumeration
{

    public class SpecialDirectoryTests : FileSystemTest
    {
        private class DirectoryRecursed : FileSystemEnumerator<string>
        {
            public int ShouldRecurseCalls { get; private set; }

            public DirectoryRecursed(string directory, EnumerationOptions options)
                : base(directory, options)
            {
            }

            protected override string TransformEntry(ref FileSystemEntry entry)
                =>new string(entry.FileName);

            protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
            {
                ShouldRecurseCalls++;
                return base.ShouldRecurseIntoEntry(ref entry);
            }
        }

        [Fact]
        public void SpecialDirectoriesAreNotUpForRecursion()
        {
            using (var recursed = new DirectoryRecursed(TestDirectory, new EnumerationOptions { ReturnSpecialDirectories = true, RecurseSubdirectories = true, AttributesToSkip = 0 }))
            {
                List<string> results = new List<string>();
                while (recursed.MoveNext())
                    results.Add(recursed.Current);

                Assert.Equal(0, recursed.ShouldRecurseCalls);
                Assert.Contains("..", results);
            }
        }
    }

    public class SpecialDirectoryTests_Enumerable : FileSystemTest
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
            // Files that begin with periods are considered hidden on Unix
            string[] paths = GetNames(TestDirectory, new EnumerationOptions { ReturnSpecialDirectories = true, AttributesToSkip = 0 });

            if (!PlatformDetection.IsWindows10Version20348OrGreater)
            {
                // Sometimes this is not returned - presumably an OS bug.
                // This occurs often on Windows 11, very rarely otherwise.
                Assert.Contains(".", paths);
            }

            Assert.Contains("..", paths);
        }
    }

    public class SpecialDirectoryTests_DirectoryInfo_GetDirectories : SpecialDirectoryTests_Enumerable
    {
        protected override string[] GetNames(string directory, EnumerationOptions options)
        {
            return new DirectoryInfo(directory).GetDirectories("*", options).Select(i => i.Name).ToArray();
        }
    }
}
