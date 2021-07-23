// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.FileProviders
{
    public partial class PhysicalFileProviderTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56190", TestPlatforms.AnyUnix)]
        public async Task UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink(bool useWildcard)
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

            var tcs = new TaskCompletionSource();
            token.RegisterChangeCallback(_ => { tcs.TrySetResult(); }, null);

            // Act
            await Task.Delay(1000); // Wait a second before writing again, see https://github.com/dotnet/runtime/issues/55951.
            File.WriteAllText(filePath, "v1.2");

            // Assert
            Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(30)),
                $"Change event was not raised - current time: {DateTime.UtcNow:O}, file LastWriteTimeUtc: {File.GetLastWriteTimeUtc(filePath):O}.");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56190", TestPlatforms.AnyUnix)]
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

            var tcs = new TaskCompletionSource();
            token.RegisterChangeCallback(_ => { tcs.TrySetResult(); }, null);

            // Assert
            Assert.False(tcs.Task.Wait(TimeSpan.FromSeconds(30)),
                "Change event was raised when it was not expected.");
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56190", TestPlatforms.AnyUnix)]
        public async Task UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink_TargetChanged(bool useWildcard, bool linkWasBroken)
        {
            // Arrange
            using var rootOfFile = new DisposableFileSystem();
            // Create file 2 first as we want to verify that the change is reported regardless of the timestamp being older.
            string file2Path = Path.Combine(rootOfFile.RootPath, Path.GetRandomFileName());
            File.WriteAllText(file2Path, "v2.1");

            string file1Path = Path.Combine(rootOfFile.RootPath, Path.GetRandomFileName());
            if (!linkWasBroken)
            {
                await Task.Delay(1000); // Wait a second before writing again, see https://github.com/dotnet/runtime/issues/55951.
                File.WriteAllText(file1Path, "v1.1");
            }

            using var rootOfLink = new DisposableFileSystem();
            string linkName = Path.GetRandomFileName();
            string linkPath = Path.Combine(rootOfLink.RootPath, linkName);
            File.CreateSymbolicLink(linkPath, file1Path);

            string filter = useWildcard ? "*" : linkName;
            using var provider = new PhysicalFileProvider(rootOfLink.RootPath) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(filter);

            var tcs = new TaskCompletionSource();
            token.RegisterChangeCallback(_ => { tcs.TrySetResult(); }, null);

            // Act - Change link target to file 2.
            File.Delete(linkPath);
            File.CreateSymbolicLink(linkPath, file2Path);

            // Assert - It should report the change regardless of the timestamp being older.
            Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(30)),
                $"Change event was not raised - current time: {DateTime.UtcNow:O}, file1 LastWriteTimeUtc: {File.GetLastWriteTimeUtc(file1Path):O}, file2 LastWriteTime: {File.GetLastWriteTimeUtc(file2Path):O}.");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/56190", TestPlatforms.AnyUnix)]
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

            var tcs = new TaskCompletionSource();
            token.RegisterChangeCallback(_ => { tcs.TrySetResult(); }, null);

            // Act
            File.Delete(linkPath);

            // Assert
            Assert.True(tcs.Task.Wait(TimeSpan.FromSeconds(30)),
                $"Change event was not raised - current time: {DateTime.UtcNow:O}, file LastWriteTimeUtc: {File.GetLastWriteTimeUtc(filePath):O}.");
        }
    }
}
