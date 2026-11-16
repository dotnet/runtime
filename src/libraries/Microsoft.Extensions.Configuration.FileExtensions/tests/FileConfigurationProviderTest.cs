// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.Test;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Configuration.FileExtensions.Test
{
    public class FileConfigurationProviderTest
    {
        private const string PhysicalFileContent = "content on disk";
        private const string TransformedFileContent = "content produced by CreateReadStream";

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

        // FileSystemWatcher is unreliable under load on .NET Framework, making this test flaky
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetFramework))]
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
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var changeCallbackRegistration = token.RegisterChangeCallback(_ => tcs.TrySetResult(true), null);

            Directory.CreateDirectory(missingSubDir);
            await Task.Delay(500);
            Assert.False(tcs.Task.IsCompleted, "Token must not fire when only the directory is created.");

            File.WriteAllText(configFilePath, "{}");

            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }

        [Theory]
        [InlineData(FileProviderKind.Physical, PhysicalFileContent, true)]
        [InlineData(FileProviderKind.DelegatingToPhysical, PhysicalFileContent, true)]
        [InlineData(FileProviderKind.DerivedFromPhysical, TransformedFileContent, false)]
        [InlineData(FileProviderKind.Custom, TransformedFileContent, false)]
        public void LoadOnlyBypassesCreateReadStreamForPhysicalFileInfo(FileProviderKind kind, string expectedContent, bool expectedSynchronousRead)
        {
            using var rootDir = new TempDirectory(Path.Combine(Path.GetTempPath(), $"pfp_cfg_test_{Guid.NewGuid():N}"));
            string fileName = "appsettings.json";
            File.WriteAllText(Path.Combine(rootDir.Path, fileName), PhysicalFileContent);

            IFileProvider fileProvider = kind switch
            {
                FileProviderKind.Physical => new PhysicalFileProvider(rootDir.Path),
                FileProviderKind.DelegatingToPhysical => new DelegatingFileProvider(rootDir.Path),
                FileProviderKind.DerivedFromPhysical => new DerivedPhysicalFileProvider(rootDir.Path),
                _ => new TransformingFileProvider(rootDir.Path)
            };

            using (fileProvider as IDisposable)
            using (var provider = new ContentCapturingFileConfigurationProvider(new FileConfigurationSourceImpl
            {
                Path = fileName,
                FileProvider = fileProvider
            }))
            {
                provider.Load();

                Assert.Equal(expectedContent, provider.Content);
                Assert.Equal(expectedSynchronousRead, provider.ReadSynchronously);
            }
        }

        public enum FileProviderKind
        {
            Physical,
            DelegatingToPhysical,
            DerivedFromPhysical,
            Custom
        }

        // An IFileInfo that reports a physical path but whose content differs from what is stored at
        // that path, mimicking providers that decrypt or otherwise rewrite a file.
        private sealed class TransformingFileInfo : IFileInfo
        {
            public TransformingFileInfo(string physicalPath) => PhysicalPath = physicalPath;

            public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(TransformedFileContent));
            public bool Exists => true;
            public bool IsDirectory => false;
            public DateTimeOffset LastModified => default;
            public long Length => TransformedFileContent.Length;
            public string Name => Path.GetFileName(PhysicalPath);
            public string PhysicalPath { get; }
        }

        private sealed class TransformingFileProvider : IFileProvider
        {
            private readonly string _root;

            public TransformingFileProvider(string root) => _root = root;

            public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;
            public IFileInfo GetFileInfo(string subpath) => new TransformingFileInfo(Path.Combine(_root, subpath));
            public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
        }

        // Surfaces the file information of an inner PhysicalFileProvider unchanged, the way
        // CompositeFileProvider does.
        private sealed class DelegatingFileProvider : IFileProvider, IDisposable
        {
            private readonly PhysicalFileProvider _inner;

            public DelegatingFileProvider(string root) => _inner = new PhysicalFileProvider(root);

            public IDirectoryContents GetDirectoryContents(string subpath) => _inner.GetDirectoryContents(subpath);
            public IFileInfo GetFileInfo(string subpath) => _inner.GetFileInfo(subpath);
            public IChangeToken Watch(string filter) => _inner.Watch(filter);
            public void Dispose() => _inner.Dispose();
        }

        // PhysicalFileProvider.GetFileInfo is not virtual, so a derived provider has to re-implement
        // IFileProvider to change what it returns.
        private sealed class DerivedPhysicalFileProvider : PhysicalFileProvider, IFileProvider
        {
            public DerivedPhysicalFileProvider(string root) : base(root)
            { }

            public new IFileInfo GetFileInfo(string subpath) => new TransformingFileInfo(base.GetFileInfo(subpath).PhysicalPath);
        }

        private sealed class ContentCapturingFileConfigurationProvider : FileConfigurationProvider
        {
            public ContentCapturingFileConfigurationProvider(FileConfigurationSource source)
                : base(source)
            { }

            public string Content { get; private set; }

            // The fast path opens the file without FileOptions.Asynchronous while
            // PhysicalFileInfo.CreateReadStream opens it with, which only Windows reports back through
            // FileStream.IsAsync; on Unix that property is always false for a regular file. The
            // transforming file infos in this test return a MemoryStream, so the stream type tells the
            // two paths apart everywhere else.
            public bool ReadSynchronously { get; private set; }

            public override void Load(Stream stream)
            {
                ReadSynchronously = stream is FileStream { IsAsync: false };

                using var reader = new StreamReader(stream);
                Content = reader.ReadToEnd();
            }
        }

        public class FileInfoImpl : IFileInfo
        {
            public FileInfoImpl(string physicalPath, bool exists = true) =>
                (PhysicalPath, Exists) = (physicalPath, exists);

            public Stream CreateReadStream() =>
                new FileStream(PhysicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1, FileOptions.SequentialScan);

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
