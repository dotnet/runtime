// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.Test;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Configuration.FileExtensions.Test
{
    public class FileConfigurationProviderTest
    {
        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void ProviderDisposesChangeTokenRegistration()
        {
            var changeToken = new ConfigurationRootTest.ChangeToken();
            var fileProviderMock = new Mock<IFileProvider>();
            fileProviderMock.Setup(fp => fp.Watch(It.IsAny<string>())).Returns(changeToken);

            var provider = new FileConfigurationProviderImpl(new FileConfigurationSourceImpl
            {
                FileProvider = fileProviderMock.Object,
                ReloadOnChange = true,
            });

            Assert.NotEmpty(changeToken.Callbacks);

            provider.Dispose();

            Assert.Empty(changeToken.Callbacks);
        }

        public static readonly IEnumerable<object[]> ProviderThrowsInvalidDataExceptionInput = new[]
        {
            new object[] { @$"C:{Path.DirectorySeparatorChar}{Guid.NewGuid()}{Path.DirectorySeparatorChar}configuration.txt" },
            new object[] { @$"{Path.DirectorySeparatorChar}{Guid.NewGuid()}{Path.DirectorySeparatorChar}configuration.txt" }
        };

        // Moq heavily utilizes RefEmit, which does not work on most aot workloads
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52319", TestPlatforms.Android)]
        public void ProviderThrowsInvalidDataExceptionWhenLoadFails()
        {
            var tempFile = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFile, "Test::FileData");

                var fileProviderMock = new Mock<IFileProvider>();
                fileProviderMock.Setup(fp => fp.Watch(It.IsAny<string>())).Returns(new ConfigurationRootTest.ChangeToken());
                fileProviderMock.Setup(fp => fp.GetFileInfo(It.IsAny<string>())).Returns(new FileInfoImpl(tempFile));

                var source = new FileConfigurationSourceImpl
                {
                    FileProvider = fileProviderMock.Object,
                    ReloadOnChange = true,
                };
                var provider = new ThrowOnLoadFileConfigurationProviderImpl(source);

                var exception = Assert.Throws<InvalidDataException>(() => provider.Load());
                Assert.Contains($"Failed to load configuration from file '{tempFile}'", exception.Message);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [MemberData(nameof(ProviderThrowsInvalidDataExceptionInput))]
        public void ProviderThrowsFileNotFoundExceptionWhenNotFound(string physicalPath)
        {
            var fileProviderMock = new Mock<IFileProvider>();
            fileProviderMock.Setup(fp => fp.Watch(It.IsAny<string>())).Returns(new ConfigurationRootTest.ChangeToken());
            fileProviderMock.Setup(fp => fp.GetFileInfo(It.IsAny<string>())).Returns(new FileInfoImpl(physicalPath, false));

            var source = new FileConfigurationSourceImpl
            {
                FileProvider = fileProviderMock.Object,
                ReloadOnChange = true,
            };
            var provider = new FileConfigurationProviderImpl(source);

            var exception = Assert.Throws<FileNotFoundException>(() => provider.Load());
            Assert.Contains(physicalPath, exception.Message);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [MemberData(nameof(ProviderThrowsInvalidDataExceptionInput))]
        public void ProviderThrowsDirectoryNotFoundExceptionWhenNotFound(string physicalPath)
        {
            var fileProviderMock = new Mock<IFileProvider>();
            fileProviderMock.Setup(fp => fp.Watch(It.IsAny<string>())).Returns(new ConfigurationRootTest.ChangeToken());
            fileProviderMock.Setup(fp => fp.GetFileInfo(It.IsAny<string>())).Returns(new FileInfoImpl(physicalPath));

            var source = new FileConfigurationSourceImpl
            {
                FileProvider = fileProviderMock.Object,
                ReloadOnChange = true,
            };
            var provider = new FileConfigurationProviderImpl(source);

            var exception = Assert.Throws<DirectoryNotFoundException>(() => provider.Load());
            Assert.Contains(physicalPath, exception.Message);
        }

        [Fact]
        public async Task ResolveFileProvider_WithMissingParentDirectory_WatchTokenFiresWhenFileCreated()
        {
            // Verify the fix for https://github.com/dotnet/runtime/issues/116713:
            // When the parent of the config file does not yet exist, Watch() should return a change token
            // that fires when the target file is created (via a non-recursive pending watcher),
            // rather than adding recursive watches on the entire ancestor directory tree.
            using var rootDir = new TempDirectory(Path.Combine(Path.GetTempPath(), $"pfp_cfg_test_{Guid.NewGuid():N}"));
            string missingSubDir = Path.Combine(rootDir.Path, "subdir");
            string configFilePath = Path.Combine(missingSubDir, "appsettings.json");

            var source = new FileConfigurationSourceImpl
            {
                Path = configFilePath,
                Optional = true,
                ReloadOnChange = true,
                ReloadDelay = 0,
            };

            // ResolveFileProvider sets FileProvider to the directory containing the file path,
            // even if that directory does not yet exist on disk.
            source.ResolveFileProvider();

            Assert.NotNull(source.FileProvider);
            using var physicalProvider = Assert.IsType<PhysicalFileProvider>(source.FileProvider);
            Assert.Equal(missingSubDir + Path.DirectorySeparatorChar, physicalProvider.Root);

            // The configuration Path is reduced to the file name relative to the provider root.
            // Verify that the intermediate directory name is not part of Path.
            Assert.DoesNotContain("subdir", source.Path, StringComparison.OrdinalIgnoreCase);

            // Watch() must return a valid (non-null) change token even though the directory is missing.
            var token = source.FileProvider.Watch(source.Path);
            Assert.NotNull(token);

            // The token should fire only when the target file is created, not when just the directory appears.
            var tcs = new TaskCompletionSource<bool>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            cts.Token.Register(() => tcs.TrySetCanceled());
            token.RegisterChangeCallback(_ => tcs.TrySetResult(true), null);

            Directory.CreateDirectory(missingSubDir);
            await Task.Delay(500);
            Assert.False(tcs.Task.IsCompleted, "Token must not fire when only the directory is created.");

            File.WriteAllText(configFilePath, "{}");

            Assert.True(await tcs.Task, "Change token did not fire after the target file was created.");
        }

        public class FileInfoImpl : IFileInfo
        {
            public FileInfoImpl(string physicalPath, bool exists = true) =>
                (PhysicalPath, Exists) = (physicalPath, exists);

            public Stream CreateReadStream() => new MemoryStream();
            public bool Exists { get; set; }
            public bool IsDirectory => false;
            public DateTimeOffset LastModified => default;
            public long Length => default;
            public string Name => default;
            public string PhysicalPath { get; }
        }

        public class FileConfigurationProviderImpl : FileConfigurationProvider
        {
            public FileConfigurationProviderImpl(FileConfigurationSource source)
                : base(source)
            { }

            public override void Load(Stream stream)
            { }
        }

        public class ThrowOnLoadFileConfigurationProviderImpl : FileConfigurationProvider
        {
            public ThrowOnLoadFileConfigurationProviderImpl(FileConfigurationSource source)
                : base(source)
            { }

            public override void Load(Stream stream) => throw new Exception("This is a test exception.");
        }

        public class FileConfigurationSourceImpl : FileConfigurationSource
        {
            public override IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                EnsureDefaults(builder);
                return new FileConfigurationProviderImpl(this);
            }
        }
    }
}
