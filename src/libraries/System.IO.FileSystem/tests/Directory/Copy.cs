// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Tests;
using System.Threading;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public sealed class Directory_CopyDirectory : FileSystemTest
    {
        [Fact]
        public void Copy()
        {
            const string sourceDirName = $"Test_source_{nameof(Copy)}";
            const string destDirName = $"Test_target_{nameof(Copy)}";
            CancellationToken cancellationToken = new();

            // Prerequisites
            // Create source folder
            if (Directory.Exists(sourceDirName))
            {
                Directory.Delete(sourceDirName, true);
            }
            Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            if (Directory.Exists(destDirName))
            {
                Directory.Delete(destDirName, true);
            }
            Directory.CreateDirectory(destDirName);

            // Create file which does not exist in target folder
            string firstPath = Path.Combine(sourceDirName, "first.txt");
            using (StreamWriter firstStreamSource = File.CreateText(firstPath))
            {
                firstStreamSource.WriteLine("Test");
            }

            // Create file which does exist in target folder
            string secondPathSource = Path.Combine(sourceDirName, "test.txt");
            using (StreamWriter secondStreamSource = File.CreateText(secondPathSource))
            {
                secondStreamSource.WriteLine("Test");
            }

            string secondPathDest = Path.Combine(destDirName, "test.txt");
            using (StreamWriter secondStreamDest = File.CreateText(secondPathDest))
            {
                secondStreamDest.WriteLine("Test");
            }

            // Do file system operation
            Directory.Copy(sourceDirName, destDirName, false, false, cancellationToken);

            // Validate cancellation
            Assert.True(cancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void Copy_SkipExistingFiles()
        {
            const string sourceDirName = $"Test_source_{nameof(Copy_SkipExistingFiles)}";
            const string destDirName = $"Test_target_{nameof(Copy_SkipExistingFiles)}";
            CancellationToken cancellationToken = new();

            // Prerequisites
            // Create source folder
            if (Directory.Exists(sourceDirName))
            {
                Directory.Delete(sourceDirName, true);
            }
            Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            if (Directory.Exists(destDirName))
            {
                Directory.Delete(destDirName, true);
            }
            Directory.CreateDirectory(destDirName);

            // Create file which does not exist in target folder
            string firstPath = Path.Combine(sourceDirName, "first.txt");
            using (StreamWriter firstStreamSource = File.CreateText(firstPath))
            {
                firstStreamSource.WriteLine("Test");
            }

            // Create file which does exist in target folder
            string secondPathSource = Path.Combine(sourceDirName, "test.txt");
            using (StreamWriter secondStreamSource = File.CreateText(secondPathSource))
            {
                secondStreamSource.WriteLine("Test");
            }

            string secondPathDest = Path.Combine(destDirName, "test.txt");
            using (StreamWriter secondStreamDest = File.CreateText(secondPathDest))
            {
                secondStreamDest.WriteLine("Test");
            }

            // Do file system operation
            Directory.Copy(sourceDirName, destDirName, false, true, cancellationToken);

            // Validate cancellation
            Assert.False(cancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void Copy_Recursive()
        {
            const string sourceDirName = $"Test_source_{nameof(Copy_Recursive)}";
            const string destDirName = $"Test_target_{nameof(Copy_Recursive)}";
            CancellationToken cancellationToken = new();

            // Prerequisites
            // Create source folder
            if (Directory.Exists(sourceDirName))
            {
                Directory.Delete(sourceDirName, true);
            }
            Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            if (Directory.Exists(destDirName))
            {
                Directory.Delete(destDirName, true);
            }
            Directory.CreateDirectory(destDirName);

            string sourceTopLevelFilePath = Path.Combine(sourceDirName, "top-level.txt");
            using (StreamWriter sourceTopLevelFileStream = File.CreateText(sourceTopLevelFilePath))
            {
                sourceTopLevelFileStream.WriteLine("Test");
            }

            string sourceSecondLevelDirectoryPath = Path.Combine(sourceDirName, "subdir");
            Directory.CreateDirectory(sourceSecondLevelDirectoryPath);

            string sourceThirdLevelFilePath = Path.Combine(sourceSecondLevelDirectoryPath, "test.txt");
            using (StreamWriter sourceThirdLevelFileStream = File.CreateText(sourceThirdLevelFilePath))
            {
                sourceThirdLevelFileStream.WriteLine("Test");
            }

            // Do file system opreation
            Directory.Copy(sourceDirName, destDirName, true, false, cancellationToken);

            // Validate cancellation
            Assert.False(cancellationToken.IsCancellationRequested);

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
            const string sourceDirName = $"Test_source_{nameof(Copy_NonRecursive)}";
            const string destDirName = $"Test_target_{nameof(Copy_NonRecursive)}";
            CancellationToken cancellationToken = new();

            // Prerequisites
            // Create source folder
            if (Directory.Exists(sourceDirName))
            {
                Directory.Delete(sourceDirName, true);
            }
            Directory.CreateDirectory(sourceDirName);

            // Create destination folder
            if (Directory.Exists(destDirName))
            {
                Directory.Delete(destDirName, true);
            }
            Directory.CreateDirectory(destDirName);

            string sourceTopLevelFilePath = Path.Combine(sourceDirName, "top-level.txt");
            using (StreamWriter sourceTopLevelFileStream = File.CreateText(sourceTopLevelFilePath))
            {
                sourceTopLevelFileStream.WriteLine("Test");
            }

            string sourceSecondLevelDirectoryPath = Path.Combine(sourceDirName, "subdir");
            Directory.CreateDirectory(sourceSecondLevelDirectoryPath);

            string sourceThirdLevelFilePath = Path.Combine(sourceSecondLevelDirectoryPath, "test.txt");
            using (StreamWriter sourceThirdLevelFileStream = File.CreateText(sourceThirdLevelFilePath))
            {
                sourceThirdLevelFileStream.WriteLine("Test");
            }

            // Do file system opreation
            Directory.Copy(sourceDirName, destDirName, false, false, cancellationToken);

            // Validate cancellation
            Assert.False(cancellationToken.IsCancellationRequested);

            // Validate copied files
            string destTopLevelFilePath = Path.Combine(destDirName, "top-level.txt");
            string destSecondLevelDirectoryPath = Path.Combine(destDirName, "subdir");
            string destThirdLevelFilePath = Path.Combine(destSecondLevelDirectoryPath, "test.txt");

            Assert.True(File.Exists(destTopLevelFilePath));

            // False because we only want to copy top level files (= non-recursive)
            Assert.False(File.Exists(destThirdLevelFilePath));
        }
    }
}
