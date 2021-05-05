// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration.Test;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Configuration.FileExtensions.Test
{
    public class FileConfigurationProviderTest
    {
        [Fact]
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

        [Fact]
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

        [Theory]
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

        [Theory]
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
