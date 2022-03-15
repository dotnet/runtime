// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Tests;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public sealed class CopyTo : FileSystemTest
    {
        [Fact]
        public void Copy()
        {
            string sourceDirName = GetTestFilePath();
            string destDirName = GetTestFilePath();
            CancellationToken cancellationToken = new();

            // Prerequisites
            // Create source folder
            if (Directory.Exists(sourceDirName))
            {
                Directory.Delete(sourceDirName, true);
            }
            var sourceFolder = Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            if (Directory.Exists(destDirName))
            {
                Directory.Delete(destDirName, true);
            }
            Directory.CreateDirectory(destDirName);

            // Create file which does not exist in target folder
            string firstPath = Path.Combine(sourceDirName, "first.txt");
            File.WriteAllText(firstPath, "Test");

            // Create file which does exist in target folder
            string secondPathSource = Path.Combine(sourceDirName, "test.txt");
            File.WriteAllText(secondPathSource, "Test");

            string secondPathDest = Path.Combine(destDirName, "test.txt");
            File.WriteAllText(secondPathDest, "Test");

            // Do file system operation
            bool result = sourceFolder.CopyTo(destDirName, false, cancellationToken: cancellationToken);
            Assert.True(result);
        }

        [Fact]
        public void Copy_SkipExistingFiles()
        {
            string sourceDirName = GetTestFilePath();
            string destDirName = GetTestFilePath();
            CancellationToken cancellationToken = new();

            // Prerequisites
            // Create source folder
            var sourceFolder = Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            Directory.CreateDirectory(destDirName);

            // Create file which does not exist in target folder
            string firstPath = Path.Combine(sourceDirName, "first.txt");
            File.WriteAllText(firstPath, "Test");

            // Create file which does exist in target folder
            string secondPathSource = Path.Combine(sourceDirName, "test.txt");
            File.WriteAllText(secondPathSource, "Test");

            string secondPathDest = Path.Combine(destDirName, "test.txt");
            File.WriteAllText(secondPathDest, "Test");

            // Do file system operation
            bool result = sourceFolder.CopyTo(destDirName, false, true, cancellationToken);
            Assert.True(result);
        }

        [Fact]
        public void Copy_Recursive()
        {
            string sourceDirName = GetTestFilePath();
            string destDirName = GetTestFilePath();
            CancellationToken cancellationToken = new();

            // Prerequisites
            // Create source folder
            var sourceFolder = Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            Directory.CreateDirectory(destDirName);

            string sourceTopLevelFilePath = Path.Combine(sourceDirName, "top-level.txt");
            File.WriteAllText(sourceTopLevelFilePath, "Test");

            string sourceSecondLevelDirectoryPath = Path.Combine(sourceDirName, "subdir");
            Directory.CreateDirectory(sourceSecondLevelDirectoryPath);

            string sourceThirdLevelFilePath = Path.Combine(sourceSecondLevelDirectoryPath, "test.txt");
            File.WriteAllText(sourceThirdLevelFilePath, "Test");

            // Do file system operation
            bool result = sourceFolder.CopyTo(destDirName, true, false, cancellationToken);
            Assert.True(result);

            // Validate copied files
            string destTopLevelFilePath = Path.Combine(destDirName, "top-level.txt");
            string destSecondLevelDirectoryPath = Path.Combine(destDirName, "subdir");
            string destThirdLevelFilePath = Path.Combine(destSecondLevelDirectoryPath, "test.txt");

            Assert.True(File.Exists(destTopLevelFilePath));
            Assert.True(File.Exists(destThirdLevelFilePath));
        }

        [Fact]
        public void Copy_NonRecursive()
        {
            string sourceDirName = GetTestFilePath();
            string destDirName = GetTestFilePath();
            CancellationToken cancellationToken = new();

            // Prerequisites
            // Create source folder
            var sourceFolder = Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            Directory.CreateDirectory(destDirName);

            string sourceTopLevelFilePath = Path.Combine(sourceDirName, "top-level.txt");
            File.WriteAllText(sourceTopLevelFilePath, "Test");

            string sourceSecondLevelDirectoryPath = Path.Combine(sourceDirName, "subdir");
            Directory.CreateDirectory(sourceSecondLevelDirectoryPath);

            string sourceThirdLevelFilePath = Path.Combine(sourceSecondLevelDirectoryPath, "test.txt");
            File.WriteAllText(sourceThirdLevelFilePath, "Test");

            // Do file system opreation
            bool result = sourceFolder.CopyTo(destDirName, false, false, cancellationToken);
            Assert.True(result);

            // Validate copied files
            string destTopLevelFilePath = Path.Combine(destDirName, "top-level.txt");
            string destSecondLevelDirectoryPath = Path.Combine(destDirName, "subdir");
            string destThirdLevelFilePath = Path.Combine(destSecondLevelDirectoryPath, "test.txt");

            Assert.True(File.Exists(destTopLevelFilePath));

            // False because we only want to copy top level files (= non-recursive)
            Assert.False(File.Exists(destThirdLevelFilePath));
        }

        [Fact]
        public void Copy_Cancellation()
        {
            string sourceDirName = GetTestFilePath();
            string destDirName = GetTestFilePath();
            var cancellationTokenSource = new CancellationTokenSource();

            // Prerequisites
            // Create source folder
            if (Directory.Exists(sourceDirName))
            {
                Directory.Delete(sourceDirName, true);
            }
            var sourceFolder = Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            if (Directory.Exists(destDirName))
            {
                Directory.Delete(destDirName, true);
            }
            Directory.CreateDirectory(destDirName);

            // Create file which does not exist in target folder
            string firstPath = Path.Combine(sourceDirName, "first.txt");
            File.WriteAllText(firstPath, "Test");

            // Create file which does exist in target folder
            string secondPathSource = Path.Combine(sourceDirName, "test.txt");
            File.WriteAllText(secondPathSource, "Test");

            string secondPathDest = Path.Combine(destDirName, "test.txt");
            File.WriteAllText(secondPathDest, "Test");

            // Validate cancellation
            cancellationTokenSource.Cancel();
            Assert.Throws<OperationCanceledException>(() => sourceFolder.CopyTo(destDirName, false, true, cancellationTokenSource.Token));
        }

        [Fact]
        public void Copy_WriteFailure()
        {
            string sourceDirName = GetTestFilePath();
            string destDirName = GetTestFilePath();
            CancellationToken cancellationToken = new();

            // Prerequisites
            // Create source folder
            if (Directory.Exists(sourceDirName))
            {
                Directory.Delete(sourceDirName, true);
            }
            var sourceFolder = Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            if (Directory.Exists(destDirName))
            {
                Directory.Delete(destDirName, true);
            }
            Directory.CreateDirectory(destDirName);

            // Create file which does not exist in target folder
            string firstPath = Path.Combine(sourceDirName, "first.txt");
            File.WriteAllText(firstPath, "Test");

            // Create file which does exist in target folder
            string secondPathSource = Path.Combine(sourceDirName, "test.txt");
            File.WriteAllText(secondPathSource, "Test");

            string secondPathDest = Path.Combine(destDirName, "test.txt");
            File.WriteAllText(secondPathDest, "Test");

            // Leave handle open to tell the operating system that this object is used
            // We need this to validate the exception
            using SafeFileHandle _ = File.OpenHandle(secondPathDest, share: FileShare.None);
            Assert.Throws<UnauthorizedAccessException>(() => sourceFolder.CopyTo(destDirName, false, true, cancellationToken));
        }
    }
}
