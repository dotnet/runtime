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
        public void UsePollingFileWatcher_UseActivePolling_HasChanged(bool useWildcard)
        {
            // Arrange
            using var root = new DisposableFileSystem();
            string fileName = Path.GetRandomFileName();
            string filePath = Path.Combine(root.RootPath, fileName);
            File.WriteAllText(filePath, "v1.1");

            using var provider = new PhysicalFileProvider(root.RootPath) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(useWildcard ? "*" : fileName);
            Assert.False(token.HasChanged);

            // Act
            File.WriteAllText(filePath, "v1.2");
            Thread.Sleep(GetTokenPollingInterval(token));

            // Assert
            Assert.True(token.HasChanged);
        }

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
        [InlineData(false)]
        [InlineData(true)]
        public void UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink_TargetChanged(bool useWildcard)
        {
            // Arrange
            using var rootOfFile = new DisposableFileSystem();
            string file1Path = Path.Combine(rootOfFile.RootPath, Path.GetRandomFileName());
            File.WriteAllText(file1Path, "v1.1");

            string file2Path = Path.Combine(rootOfFile.RootPath, Path.GetRandomFileName());
            File.WriteAllText(file2Path, "v2.1");

            using var rootOfLink = new DisposableFileSystem();
            string linkName = Path.GetRandomFileName();
            string linkPath = Path.Combine(rootOfLink.RootPath, linkName);
            File.CreateSymbolicLink(linkPath, file1Path);

            string filter = useWildcard ? "*" : linkName;
            using var provider = new PhysicalFileProvider(rootOfLink.RootPath) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(filter);
            Assert.False(token.HasChanged);

            // Act 1 - Change file 1's content.
            File.WriteAllText(file1Path, "v1.2");
            Thread.Sleep(GetTokenPollingInterval(token));

            // Assert 1
            Assert.True(token.HasChanged);

            // Act 2 - Change link target to file 2.
            token = provider.Watch(filter); // Once HasChanged is true, the value will always be true. Get a new change token.
            Assert.False(token.HasChanged);
            File.Delete(linkPath);
            File.CreateSymbolicLink(linkPath, file2Path);
            Thread.Sleep(GetTokenPollingInterval(token));

            // Assert 2
            Assert.True(token.HasChanged); // It should report the change regardless of the timestamp being older.

            // Act 3 - Change file 2's content.
            token = provider.Watch(filter);
            Assert.False(token.HasChanged);
            File.WriteAllText(file2Path, "v2.2");
            Thread.Sleep(GetTokenPollingInterval(token));

            // Assert 3
            Assert.True(token.HasChanged);
        }

        private int GetTokenPollingInterval(IChangeToken changeToken)
        {
            TimeSpan pollingInterval = (changeToken as CompositeChangeToken).ChangeTokens[1] switch
            {
                PollingWildCardChangeToken wildcardChangeToken => wildcardChangeToken.PollingInterval,
                PollingFileChangeToken => PollingFileChangeToken.PollingInterval,
                _ => throw new InvalidOperationException()
            };

            return (int)pollingInterval.TotalMilliseconds;
        }
    }
}
