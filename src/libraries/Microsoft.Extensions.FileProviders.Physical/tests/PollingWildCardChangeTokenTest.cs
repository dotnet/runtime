// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using Microsoft.Extensions.FileProviders.Physical.Internal;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Extensions.FileProviders.Physical
{
    public class PollingWildCardChangeTokenTest
    {
        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
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

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
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

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
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

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
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

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
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

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
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

        public static TheoryData<Exception> DirectoryScanFailures => new TheoryData<Exception>
        {
            new IOException("Host is down"),
            new UnauthorizedAccessException(),
            new SecurityException(),
        };

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [MemberData(nameof(DirectoryScanFailures))]
        public void HasChanged_ReturnsFalseIfTheDirectoryCannotBeScanned(Exception exception)
        {
            // Arrange
            var directoryInfo = new Mock<DirectoryInfoBase>();
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile("1.txt") });
            var clock = new TestClock();
            var token = new TestablePollingWildCardChangeToken(directoryInfo.Object, "**/*.txt", clock);

            var scanCount = 0;
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Callback(() => scanCount++)
                .Throws(exception);

            // Act - 1
            clock.Increment();
            var result1 = token.HasChanged;

            // Assert - 1
            Assert.False(result1);
            Assert.Equal(1, scanCount);

            // Act - 2: the polling interval is still honored while the failure persists.
            var result2 = token.HasChanged;

            // Assert - 2
            Assert.False(result2);
            Assert.Equal(1, scanCount);
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [MemberData(nameof(DirectoryScanFailures))]
        public void HasChanged_ReturnsFalseIfFileTimestampCannotBeRead(Exception exception)
        {
            // Arrange
            var filePath = "1.txt";
            var directoryInfo = new Mock<DirectoryInfoBase>();
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile(filePath) });
            var clock = new TestClock();
            var token = new TestablePollingWildCardChangeToken(directoryInfo.Object, "**/*.txt", clock);
            int initialReadCount = token.LastWriteCount;
            token.LastWriteException = exception;

            // Act - 1
            clock.Increment();
            var result1 = token.HasChanged;

            // Assert - 1
            Assert.False(result1);
            Assert.Equal(initialReadCount + 1, token.LastWriteCount);

            // Act - 2: the polling interval is still honored while the failure persists.
            var result2 = token.HasChanged;

            // Assert - 2
            Assert.False(result2);
            Assert.Equal(initialReadCount + 1, token.LastWriteCount);

            // Act - 3: timestamp reads recover and changes made during the failure are detected.
            token.LastWriteException = null;
            token.FileTimestampLookup[filePath] = clock.UtcNow.AddMilliseconds(1);
            clock.Increment();
            var result3 = token.HasChanged;

            // Assert - 3
            Assert.True(result3);
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void HasChanged_ReturnsTrueIfFilesChangedWhileTheDirectoryCouldNotBeScanned()
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
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Throws(new IOException("Host is down"));
            clock.Increment();
            var result1 = token.HasChanged;

            // Assert - 1
            Assert.False(result1);

            // Act - 2: the directory can be scanned again and a file was added in the meantime.
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile(filePath1), CreateFile(filePath2) });
            clock.Increment();
            var result2 = token.HasChanged;

            // Assert - 2
            Assert.True(result2);
        }

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void HasChanged_ReturnsFalseOnTheFirstScanThatSucceedsIfTheDirectoryCouldNotBeScannedWhenCreated()
        {
            // Arrange
            var filePath = "1.txt";
            var directoryInfo = new Mock<DirectoryInfoBase>();
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Throws(new IOException("Host is down"));
            var clock = new TestClock();
            var token = new TestablePollingWildCardChangeToken(directoryInfo.Object, "**/*.txt", clock);

            // Act: the directory can be scanned again. Its files are seen for the first time and carry a
            // timestamp later than the failed scan, but without a baseline that isn't a change.
            directoryInfo.Setup(d => d.EnumerateFileSystemInfos())
                .Returns(new[] { CreateFile(filePath) });
            token.FileTimestampLookup[filePath] = clock.UtcNow.AddMilliseconds(1);
            clock.Increment();
            var result = token.HasChanged;

            // Assert
            Assert.False(result);
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

            public Exception LastWriteException { get; set; }

            public int LastWriteCount { get; private set; }

            protected override DateTime GetLastWriteUtc(string path)
            {
                LastWriteCount++;
                if (LastWriteException is not null)
                {
                    throw LastWriteException;
                }

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
