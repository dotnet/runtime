// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests
{
    public class ErrorHandlingTests : FileSystemTest
    {
        private class IgnoreErrors : FileSystemEnumerator<string>
        {
            public IgnoreErrors(string directory)
                : base(directory)
            { }

            public int ErrorCount { get; private set; }
            public string DirectoryFinished { get; private set; }

            protected override string TransformEntry(ref FileSystemEntry entry)
                => entry.FileName.ToString();

            protected override bool ContinueOnError(int error)
            {
                ErrorCount++;
                return true;
            }

            protected override void OnDirectoryFinished(ReadOnlySpan<char> directory)
                => DirectoryFinished = directory.ToString();
        }

        private class LastError : FileSystemEnumerator<string>
        {
            public LastError(string directory)
                : base(directory)
            { }

            public int Error { get; private set; }

            protected override string TransformEntry(ref FileSystemEntry entry)
                => entry.FileName.ToString();

            protected override bool ContinueOnError(int error)
            {
                Error = error;
                return true;
            }
        }

        [Fact]
        public void OpenErrorDoesNotHappenAgainOnMoveNext()
        {
            // What we're checking for here is that we don't try to enumerate when we
            // couldn't even open the root directory (e.g. open the handle again, try
            // to get data, etc.)
            using (IgnoreErrors ie = new IgnoreErrors(Path.GetRandomFileName()))
            {
                Assert.Equal(1, ie.ErrorCount);
                Assert.False(ie.MoveNext());
                Assert.Equal(1, ie.ErrorCount);

                // Since we didn't start, the directory shouldn't finish.
                Assert.Null(ie.DirectoryFinished);
            }
        }

        [Fact]
        public void NotFoundErrorIsExpected()
        {
            // Make sure we're returning the native error as expected (and not the PAL error on Unix)
            using (LastError le = new LastError(Path.GetRandomFileName()))
            {
                // Conveniently ERROR_FILE_NOT_FOUND and ENOENT are both 0x2
                Assert.Equal(2, le.Error);
            }
        }

        [Fact]
        public void DeleteDirectoryAfterOpening()
        {
            // We shouldn't prevent the directory from being deleted, even though we've
            // opened (and are holding) the handle. On Windows this means we've opened
            // the handle with file share of delete.
            DirectoryInfo info = Directory.CreateDirectory(GetTestFilePath());
            using (IgnoreErrors ie = new IgnoreErrors(info.FullName))
            {
                Assert.Equal(0, ie.ErrorCount);
                Directory.Delete(info.FullName);
                Assert.False(ie.MoveNext());

                // This doesn't cause an error as the directory is still valid until the
                // the enumerator is closed (as we have an open handle)
                Assert.Equal(0, ie.ErrorCount);
                Assert.Equal(info.FullName, ie.DirectoryFinished);
            }
        }

	[Fact]
	public void VariableLengthFileNames_AllCreatableFilesAreEnumerable()
	{
	    DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
	    var names = new List<string>();

	    for (int length = 1; length < 10_000; length++) // arbitrarily large limit for the test
	    {
	        string name = new string('a', length);
	        try { File.Create(Path.Join(testDirectory.FullName, name)).Dispose(); }
	        catch { break; }
	        names.Add(name);
	    }
	    Assert.InRange(names.Count, 1, int.MaxValue);
	    Assert.Equal(names.OrderBy(n => n), Directory.GetFiles(testDirectory.FullName).Select(n => Path.GetFileName(n)).OrderBy(n => n));
	}

	[Fact]
	public void VariableLengthDirectoryNames_AllCreatableDirectoriesAreEnumerable()
	{
	    DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
	    var names = new List<string>();

	    for (int length = 1; length < 10_000; length++) // arbitrarily large limit for the test
	    {
	        string name = new string('a', length);
	        try { Directory.CreateDirectory(Path.Join(testDirectory.FullName, name)); }
	        catch { break; }
	        names.Add(name);
	    }
	    Assert.InRange(names.Count, 1, int.MaxValue);
	    Assert.Equal(names.OrderBy(n => n), Directory.GetDirectories(testDirectory.FullName).Select(n => Path.GetFileName(n)).OrderBy(n => n));
	}
    }
}
