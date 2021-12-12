// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Tests;
using System.Threading;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public sealed class CopyTo : Directory_CreateDirectory
    {
        [Fact]
        public void SimpleCopyToTest()
        {
            CancellationToken cancellationToken = new();
            const string sourceFolderPath = $"test_source_{nameof(SimpleCopyToTest)}";
            const string destFolderPath = $"test_dest_{nameof(SimpleCopyToTest)}";

            // Prerequisites
            var sourceFolder = Directory.CreateDirectory(sourceFolderPath);
            var destFolder = Directory.CreateDirectory(destFolderPath);

            // Fill source folder with test files
            const int n = 10;
            for (int i = 0; i < n; i++)
            {
                string fileName = Path.Combine(sourceFolderPath, $"{i}.txt");

                using StreamWriter writer = File.CreateText(fileName);
                writer.WriteLine("Lorem ipsum dolor sit a test");
            }

            // Do file system operation
            sourceFolder.CopyTo(destFolderPath, false, false, cancellationToken);

            // Validate cancellation
            Assert.False(cancellationToken.IsCancellationRequested);

            // Validate existing files
            Assert.Equal(n, destFolder.GetFiles().Length);
        }
    }
}
