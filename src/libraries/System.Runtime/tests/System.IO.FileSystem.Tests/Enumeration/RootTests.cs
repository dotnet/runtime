// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class RootTests
    {
        private class DirectoryRecursed : FileSystemEnumerator<string>
        {
            public string LastDirectory { get; private set; }

            public DirectoryRecursed(string directory, EnumerationOptions options)
                : base(directory, options)
            {
            }

            protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
                => !entry.IsDirectory;

            protected override string TransformEntry(ref FileSystemEntry entry)
                => entry.ToFullPath();

            protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
            {
                LastDirectory = new string(entry.Directory);
                return false;
            }
        }

        private class OriginalRootDirectoryEnumerator : FileSystemEnumerator<string>
        {
            public string CapturedOriginalRootDirectory { get; private set; }

            public OriginalRootDirectoryEnumerator(string directory, EnumerationOptions options)
                : base(directory, options)
            {
            }

            protected override bool ShouldIncludeEntry(ref FileSystemEntry entry) => true;

            protected override string TransformEntry(ref FileSystemEntry entry)
            {
                CapturedOriginalRootDirectory = new string(entry.OriginalRootDirectory);
                return entry.ToFullPath();
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Android, "Test could not work on android since accessing '/' isn't allowed.")]
        public void CanRecurseFromRoot()
        {
            string root = Path.GetPathRoot(Path.GetTempPath());

            using (var recursed = new DirectoryRecursed(root, new EnumerationOptions { AttributesToSkip = FileAttributes.System, RecurseSubdirectories = true }))
            {
                while (recursed.MoveNext())
                {
                    if (recursed.LastDirectory != null)
                    {
                        Assert.Equal(root, recursed.LastDirectory);
                        return;
                    }

                    // Should start with the root and shouldn't have a separator after the root
                    Assert.StartsWith(root, recursed.Current);
                    Assert.True(recursed.Current.LastIndexOf(Path.DirectorySeparatorChar) < root.Length,
                        $"should have no separators past the root '{root}' in '{recursed.Current}'");
                }

                Assert.NotNull(recursed.LastDirectory);
            }
        }

        [Theory]
        [MemberData(nameof(TestData.WindowsTrailingProblematicFileNames), MemberType = typeof(TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void OriginalRootDirectoryPreservesInput(string trailingCharacters)
        {
            // OriginalRootDirectory should preserve the exact input path provided by the user,
            // including trailing spaces and periods. This is important for backward compatibility with
            // code that relies on the exact format of the original path when using FileSystemEnumerator directly.
            // Note: This tests direct FileSystemEnumerator usage, not Directory.GetFiles which goes through
            // NormalizeInputs and trims trailing spaces/periods.

            DirectoryInfo testDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            try
            {
                // Create a test file
                string testFile = Path.Combine(testDir.FullName, "test.txt");
                File.WriteAllText(testFile, "test");

                string pathWithTrailingCharacters = testDir.FullName + trailingCharacters;

                using (var enumerator = new OriginalRootDirectoryEnumerator(
                    pathWithTrailingCharacters,
                    new EnumerationOptions { RecurseSubdirectories = false }))
                {
                    if (enumerator.MoveNext())
                    {
                        // OriginalRootDirectory should match the input path exactly
                        Assert.Equal(pathWithTrailingCharacters, enumerator.CapturedOriginalRootDirectory);
                    }
                }
            }
            finally
            {
                testDir.Delete(true);
            }
        }
    }
}
