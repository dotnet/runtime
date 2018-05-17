// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class RemovedDirectoryTests : FileSystemTest
    {
        private class DirectoryFinishedEnumerator : FileSystemEnumerator<string>
        {
            public DirectoryFinishedEnumerator(string directory, EnumerationOptions options)
                : base(directory, options)
            {
            }

            protected override string TransformEntry(ref FileSystemEntry entry)
            {
                return entry.FileName.ToString();
            }

            public delegate void DirectoryFinishedDelegate(ReadOnlySpan<char> directory);

            public DirectoryFinishedDelegate DirectoryFinishedAction { get; set; }

            protected override void OnDirectoryFinished(ReadOnlySpan<char> directory)
            {
                DirectoryFinishedAction?.Invoke(directory);
            }
        }

        [Fact]
        public void RemoveDirectoryBeforeRecursion()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            DirectoryInfo testSubdirectory = Directory.CreateDirectory(Path.Combine(testDirectory.FullName, "Subdirectory"));

            using (var enumerator = new DirectoryFinishedEnumerator(testDirectory.FullName, new EnumerationOptions { RecurseSubdirectories = true }))
            {
                enumerator.DirectoryFinishedAction = (d) => testSubdirectory.Delete();
                Assert.Throws<DirectoryNotFoundException>(() => { while (enumerator.MoveNext()); });
            }
        }
    }
}
