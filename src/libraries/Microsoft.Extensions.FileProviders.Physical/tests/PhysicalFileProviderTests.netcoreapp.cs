// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.FileProviders
{
    public partial class PhysicalFileProviderTests : FileCleanupTestBase
    {
        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink(bool useWildcard)
        {
            // Arrange
            using var rootOfFile = new TempDirectory(GetTestFilePath());
            string filePath = Path.Combine(rootOfFile.Path, GetTestFileName());
            File.WriteAllText(filePath, "v1.1");

            using var rootOfLink = new TempDirectory(GetTestFilePath());
            string linkName = GetTestFileName();
            string linkPath = Path.Combine(rootOfLink.Path, linkName);
            File.CreateSymbolicLink(linkPath, filePath);

            using var provider = new PhysicalFileProvider(rootOfLink.Path) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(useWildcard ? "*" : linkName);

            var tcs = new TaskCompletionSource<bool>();
            token.RegisterChangeCallback(_ => { tcs.TrySetResult(true); }, null);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => tcs.TrySetCanceled());

            // Act
            await Task.Delay(1000); // Wait a second before writing again, see https://github.com/dotnet/runtime/issues/55951.
            File.WriteAllText(filePath, "v1.2");

            // Assert
            Assert.True(await tcs.Task,
                $"Change event was not raised - current time: {DateTime.UtcNow:O}, file LastWriteTimeUtc: {File.GetLastWriteTimeUtc(filePath):O}.");
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [OuterLoop]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink_TargetNotExists(bool useWildcard)
        {
            // Arrange
            using var rootOfLink = new TempDirectory(GetTestFilePath());
            string linkName = GetTestFileName();
            string linkPath = Path.Combine(rootOfLink.Path, linkName);
            File.CreateSymbolicLink(linkPath, "not-existent-file");

            // Act
            using var provider = new PhysicalFileProvider(rootOfLink.Path) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(useWildcard ? "*" : linkName);

            var tcs = new TaskCompletionSource();
            token.RegisterChangeCallback(_ => { Assert.Fail("Change event was raised when it was not expected."); }, null);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => tcs.TrySetCanceled());

            await Assert.ThrowsAsync<TaskCanceledException>(() => tcs.Task);
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink_TargetChanged(bool useWildcard, bool linkWasBroken)
        {
            // Arrange
            using var rootOfFile = new TempDirectory(GetTestFilePath());
            // Create file 2 first as we want to verify that the change is reported regardless of the timestamp being older.
            string file2Path = Path.Combine(rootOfFile.Path, GetTestFileName());
            File.WriteAllText(file2Path, "v2.1");

            string file1Path = Path.Combine(rootOfFile.Path, GetTestFileName());
            if (!linkWasBroken)
            {
                await Task.Delay(1000); // Wait a second before writing again, see https://github.com/dotnet/runtime/issues/55951.
                File.WriteAllText(file1Path, "v1.1");
            }

            using var rootOfLink = new TempDirectory(GetTestFilePath());
            string linkName = GetTestFileName();
            string linkPath = Path.Combine(rootOfLink.Path, linkName);
            File.CreateSymbolicLink(linkPath, file1Path);

            string filter = useWildcard ? "*" : linkName;
            using var provider = new PhysicalFileProvider(rootOfLink.Path) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(filter);

            var tcs = new TaskCompletionSource<bool>();
            token.RegisterChangeCallback(_ => { tcs.TrySetResult(true); }, null);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => tcs.TrySetCanceled());

            // Act - Change link target to file 2.
            File.Delete(linkPath);

            RetryHelper.Execute(() =>
            {
                File.CreateSymbolicLink(linkPath, file2Path); // can fail, presumably due to some latency of delete of linkPath
            }, maxAttempts: 10, retryWhen: e => e is UnauthorizedAccessException);

            // Assert - It should report the change regardless of the timestamp being older.
            Assert.True(await tcs.Task,
                $"Change event was not raised - current time: {DateTime.UtcNow:O}, file1 LastWriteTimeUtc: {File.GetLastWriteTimeUtc(file1Path):O}, file2 LastWriteTime: {File.GetLastWriteTimeUtc(file2Path):O}.");
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateSymbolicLinks))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UsePollingFileWatcher_UseActivePolling_HasChanged_SymbolicLink_TargetDeleted(bool useWildcard)
        {
            // Arrange
            using var rootOfFile = new TempDirectory(GetTestFilePath());

            string filePath = Path.Combine(rootOfFile.Path, GetTestFileName());
            File.WriteAllText(filePath, "v1.1");

            using var rootOfLink = new TempDirectory(GetTestFilePath());
            string linkName = GetTestFileName();
            string linkPath = Path.Combine(rootOfLink.Path, linkName);
            File.CreateSymbolicLink(linkPath, filePath);

            string filter = useWildcard ? "*" : linkName;
            using var provider = new PhysicalFileProvider(rootOfLink.Path) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken token = provider.Watch(filter);

            var tcs = new TaskCompletionSource<bool>();
            token.RegisterChangeCallback(_ => { tcs.TrySetResult(true); }, null);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => tcs.TrySetCanceled());

            // Act
            File.Delete(linkPath);

            // Assert
            Assert.True(await tcs.Task,
                $"Change event was not raised - current time: {DateTime.UtcNow:O}, file LastWriteTimeUtc: {File.GetLastWriteTimeUtc(filePath):O}.");
        }
    }
}
