// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders.Physical.Internal;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Extensions.FileProviders.Physical
{
    public class PollingWildCardChangeTokenTest
    {
        [Fact]
        public void HasChanged_ReturnsFalseIfNoFilesExist()
        {
            // Arrange
            var directoryInfo = new Mock<DirectoryInfoBase>();
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(Enumerable.Empty<FileSystemInfoBase>());
            var clock = new TestClock();
            var token = new PollingWildCardChangeToken(directoryInfo.Object, "**/*.txt", clock);

            // Act
            clock.Increment();
            var result = token.HasChanged;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasChanged_ReturnsFalseIfFilesDoNotChange()
        {
            // Arrange
            var filePath = "1.txt";
            var fileInfo = CreateFile(filePath);
            var directoryInfo = new Mock<DirectoryInfoBase>();
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { fileInfo });
            var clock = new TestClock();
            var token = new TestablePollingWildCardChangeToken(directoryInfo.Object, "**/*.txt", clock);

            // Act
            clock.Increment();
            var result = token.HasChanged;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasChanged_ReturnsTrueIfNewFilesWereAdded()
        {
            // Arrange
            var filePath1 = "1.txt";
            var filePath2 = "2.txt";
            var directoryInfo = new Mock<DirectoryInfoBase>();
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile(filePath1) });
            var clock = new TestClock();
            var token = new TestablePollingWildCardChangeToken(directoryInfo.Object, "**/*.txt", clock);

            // Act - 1
            clock.Increment();
            var result1 = token.HasChanged;

            // Assert - 1
            Assert.False(result1);

            // Act - 2
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile(filePath1), CreateFile(filePath2) });

            clock.Increment();
            var result2 = token.HasChanged;

            // Assert - 2
            Assert.True(result2);
        }

        [Fact]
        public void HasChanged_ReturnsTrueIfFilesWereRemoved()
        {
            // Arrange
            var filePath1 = "1.txt";
            var filePath2 = "2.txt";
            var directoryInfo = new Mock<DirectoryInfoBase>();
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile(filePath1), CreateFile(filePath2) });
            var clock = new TestClock();
            var token = new TestablePollingWildCardChangeToken(directoryInfo.Object, "**/*.txt", clock);

            // Act - 1
            clock.Increment();
            var result1 = token.HasChanged;

            // Assert - 1
            Assert.False(result1);

            // Act - 2
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile(filePath1), });
            clock.Increment();
            var result2 = token.HasChanged;

            // Assert - 2
            Assert.True(result2);
        }

        [Fact]
        public void HasChanged_ReturnsTrueIfFilesWereModified()
        {
            // Arrange
            var filePath1 = "1.txt";
            var filePath2 = "2.txt";
            var directoryInfo = new Mock<DirectoryInfoBase>();
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile(filePath1), CreateFile(filePath2) });
            var clock = new TestClock();
            var token = new TestablePollingWildCardChangeToken(directoryInfo.Object, "**/*.txt", clock);

            // Act - 1
            clock.Increment();
            var result1 = token.HasChanged;

            // Assert - 1
            Assert.False(result1);

            // Act - 2
            token.FileTimestampLookup[filePath2] = clock.UtcNow.AddMilliseconds(1);
            clock.Increment();
            var result2 = token.HasChanged;

            // Assert - 2
            Assert.True(result2);
        }

        [Fact]
        public void HasChanged_ReturnsTrueIfFileWasModifiedButRetainedAnOlderTimestamp()
        {
            // Arrange
            var filePath1 = "1.txt";
            var filePath2 = "2.txt";
            var directoryInfo = new Mock<DirectoryInfoBase>();
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile(filePath1), CreateFile(filePath2) });
            var clock = new TestClock();
            var token = new TestablePollingWildCardChangeToken(directoryInfo.Object, "**/*.txt", clock);

            // Act - 1
            clock.Increment();
            var result1 = token.HasChanged;

            // Assert - 1
            Assert.False(result1);

            // Act - 2
            token.FileTimestampLookup[filePath2] = clock.UtcNow.AddMilliseconds(-100);
            clock.Increment();
            var result2 = token.HasChanged;

            // Assert - 2
            Assert.True(result2);
        }

        private static FileInfoBase CreateFile(string filePath)
        {
            var fileInfo = new Mock<FileInfoBase>();
            fileInfo.SetupGet(f => f.FullName)
                .Returns(filePath);
            fileInfo.SetupGet(f => f.Name)
                .Returns(Path.GetFileName(filePath));
            return fileInfo.Object;
        }

        private class TestablePollingWildCardChangeToken : PollingWildCardChangeToken
        {
            public TestablePollingWildCardChangeToken(
                DirectoryInfoBase directoryInfo,
                string pattern,
                IClock clock)
                : base(directoryInfo, pattern, clock)
            {
            }

            public Dictionary<string, DateTime> FileTimestampLookup { get; } =
                new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            protected override DateTime GetLastWriteUtc(string path)
            {
                DateTime value;
                if (!FileTimestampLookup.TryGetValue(path, out value))
                {
                    value = DateTime.MinValue;
                }

                return value;
            }
        }
    }
}
