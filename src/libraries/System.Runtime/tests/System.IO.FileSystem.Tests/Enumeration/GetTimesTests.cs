// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Enumeration;
using Xunit;

namespace System.IO.Tests.Enumeration
{
    public class GetTimesTests : FileSystemTest
    {
        private class AllEntries : FileSystemEnumerator<(DateTimeOffset CreationTimeUtc, DateTimeOffset LastAccessTimeUtc, DateTimeOffset LastWriteTimeUtc)>
        {
            public AllEntries(string directory, EnumerationOptions options)
                : base(directory, options)
            {
            }

            protected override (DateTimeOffset CreationTimeUtc, DateTimeOffset LastAccessTimeUtc, DateTimeOffset LastWriteTimeUtc) TransformEntry(ref FileSystemEntry entry)
            {
                return (entry.CreationTimeUtc, entry.LastAccessTimeUtc, entry.LastWriteTimeUtc);
            }
        }

        [Fact]
        public void FileTimesShouldBeUtc()
        {
            DirectoryInfo testDirectory = Directory.CreateDirectory(GetTestFilePath());
            FileInfo fileOne = new FileInfo(Path.Combine(testDirectory.FullName, GetTestFileName()));

            fileOne.Create().Dispose();

            using (var enumerator = new AllEntries(testDirectory.FullName, new EnumerationOptions()))
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(TimeSpan.Zero, enumerator.Current.CreationTimeUtc.Offset);
                Assert.Equal(TimeSpan.Zero, enumerator.Current.LastAccessTimeUtc.Offset);
                Assert.Equal(TimeSpan.Zero, enumerator.Current.LastWriteTimeUtc.Offset);
                Assert.False(enumerator.MoveNext());
            }
        }
    }
}
