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

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55821", TestPlatforms.Android)]
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
    }
}
