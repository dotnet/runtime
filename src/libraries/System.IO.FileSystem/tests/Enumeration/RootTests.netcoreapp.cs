// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Enumeration;
using System.Linq;
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

            protected override string TransformEntry(ref FileSystemEntry entry)
                => entry.ToFullPath();

            protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
            {
                LastDirectory = new string(entry.Directory); 
                return false;
            }
        }

        [ActiveIssue(27244)]
        [Fact]
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

                    // Should not get back a full path without a single separator (C:\foo.txt or /foo.txt)
                    Assert.Equal(Path.DirectorySeparatorChar, recursed.Current.SingleOrDefault(c => c == Path.DirectorySeparatorChar));
                }

                Assert.NotNull(recursed.LastDirectory);
            }
        }
    }
}
