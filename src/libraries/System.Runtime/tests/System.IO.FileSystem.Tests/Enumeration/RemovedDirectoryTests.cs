// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Enumeration;
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
                enumerator.DirectoryFinishedAction = (d) =>
                {
                    if (d.Equals(testDirectory.FullName, StringComparison.OrdinalIgnoreCase)) testSubdirectory.Delete();
                };

                // We shouldn't fail because a directory we found got deleted.
                // No errors here are possible on Windows as by the time of the deletion above, the subdirectory handle is already opened.
                while (enumerator.MoveNext());
            }
        }

        [Fact]
        public void RemoveDirectoryBeforeHandleCreation()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo testFile = new FileInfo(Path.Join(testDirectory.FullName, GetTestFileName()));
            testFile.Create().Dispose();
            DirectoryInfo testSubdirectory = Directory.CreateDirectory(Path.Join(testDirectory.FullName, "Subdirectory"));

            using (var enumerator = new DirectoryFinishedEnumerator(testDirectory.FullName, new EnumerationOptions { RecurseSubdirectories = true }))
            {
                // We shouldn't fail because a directory we found got deleted.
                // If we yank the directory when we have it's data, but BEFORE we create the handle we'll fail internally with not found.
                while (enumerator.MoveNext())
                {
                    // Using a flag file to kill the directory in the middle of processing returned data
                    if (string.Equals(enumerator.Current, testFile.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        testSubdirectory.Delete();
                    }
                };
            }
        }

        [Fact]
        public void RemoveDirectoryBeforeHandleCreationAndReplaceWithFile()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo testFile = new FileInfo(Path.Join(testDirectory.FullName, GetTestFileName()));
            testFile.Create().Dispose();
            DirectoryInfo testSubdirectory = Directory.CreateDirectory(Path.Join(testDirectory.FullName, "Subdirectory"));

            using (var enumerator = new DirectoryFinishedEnumerator(testDirectory.FullName, new EnumerationOptions { RecurseSubdirectories = true }))
            {
                // We shouldn't fail because a directory we found got deleted.
                // If replace the directory with a file when we have it's data, but BEFORE we create the handle we'll fail internally trying to create a directory handle on it.
                while (enumerator.MoveNext())
                {
                    // Using a flag file to replace the directory in the middle of processing returned data
                    if (string.Equals(enumerator.Current, testFile.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        testSubdirectory.Delete();
                        File.Create(testSubdirectory.FullName).Dispose();
                    }
                };
            }
        }
    }
}
