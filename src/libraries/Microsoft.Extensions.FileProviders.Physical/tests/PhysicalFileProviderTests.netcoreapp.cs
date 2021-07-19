// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.FileProviders
{
    public partial class PhysicalFileProviderTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink(bool useWildcard)
        {
            // Arrange
            using var rootOfFile = new DisposableFileSystem();
            string filePath = Path.Combine(rootOfFile.RootPath, Path.GetRandomFileName());
            File.WriteAllText(filePath, "v1.1");

            using var rootOfLink = new DisposableFileSystem();
            string linkName = Path.GetRandomFileName();
            string linkPath = Path.Combine(rootOfLink.RootPath, linkName);
            File.CreateSymbolicLink(linkPath, filePath);

            using var provider = new PhysicalFileProvider(rootOfLink.RootPath) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(useWildcard ? "*" : linkName);
            Assert.False(token.HasChanged);

            // Act
            Thread.Sleep(100); // Wait a bit before writing again, see https://github.com/dotnet/runtime/issues/55951.
            File.WriteAllText(filePath, "v1.2");
            Thread.Sleep(GetTokenPollingInterval(token));

            // Assert
            Assert.True(token.HasChanged);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink_TargetNotExists(bool useWildcard)
        {
            // Arrange
            using var rootOfLink = new DisposableFileSystem();
            string linkName = Path.GetRandomFileName();
            string linkPath = Path.Combine(rootOfLink.RootPath, linkName);
            File.CreateSymbolicLink(linkPath, "not-existent-file");

            // Act
            using var provider = new PhysicalFileProvider(rootOfLink.RootPath) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(useWildcard ? "*" : linkName);

            // Assert
            Assert.False(token.HasChanged);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink_TargetChanged(bool useWildcard, bool fromTargetNonExistent)
        {
            // Arrange
            using var rootOfFile = new DisposableFileSystem();
            // Create file 2 first as we want to verify that the change is reported regardless of the timestamp being older.
            string file2Path = Path.Combine(rootOfFile.RootPath, Path.GetRandomFileName());
            File.WriteAllText(file2Path, "v2.1");

            string file1Path = Path.Combine(rootOfFile.RootPath, Path.GetRandomFileName());
            if (!fromTargetNonExistent)
            {
                Thread.Sleep(100); // Wait a bit before writing again, see https://github.com/dotnet/runtime/issues/55951.
                File.WriteAllText(file1Path, "v1.1");
            }

            using var rootOfLink = new DisposableFileSystem();
            string linkName = Path.GetRandomFileName();
            string linkPath = Path.Combine(rootOfLink.RootPath, linkName);
            File.CreateSymbolicLink(linkPath, file1Path);

            string filter = useWildcard ? "*" : linkName;
            using var provider = new PhysicalFileProvider(rootOfLink.RootPath) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(filter);
            Assert.False(token.HasChanged);

            // Act - Change link target to file 2.
            File.Delete(linkPath);
            File.CreateSymbolicLink(linkPath, file2Path);
            Thread.Sleep(GetTokenPollingInterval(token));

            // Assert
            Assert.True(token.HasChanged); // It should report the change regardless of the timestamp being older.
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink_TargetDeleted(bool useWildcard)
        {
            // Arrange
            using var rootOfFile = new DisposableFileSystem();

            string filePath = Path.Combine(rootOfFile.RootPath, Path.GetRandomFileName());
            File.WriteAllText(filePath, "v1.1");

            using var rootOfLink = new DisposableFileSystem();
            string linkName = Path.GetRandomFileName();
            string linkPath = Path.Combine(rootOfLink.RootPath, linkName);
            File.CreateSymbolicLink(linkPath, filePath);

            string filter = useWildcard ? "*" : linkName;
            using var provider = new PhysicalFileProvider(rootOfLink.RootPath) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(filter);
            Assert.False(token.HasChanged);

            // Act
            File.Delete(linkPath);
            Thread.Sleep(GetTokenPollingInterval(token));

            // Assert
            Assert.True(token.HasChanged);
        }
    }
}
