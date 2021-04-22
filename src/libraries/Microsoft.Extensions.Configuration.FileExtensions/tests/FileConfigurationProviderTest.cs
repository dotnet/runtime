// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        [Theory]
        [InlineData(@"C:\path\to\configuration.txt")]
        [InlineData(@"/path/to/configuration.txt")]
        public void ProviderThrowsInvalidDataExceptionWhenLoadFails(string physicalPath)
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

            var exception = Assert.Throws<InvalidDataException>(() => provider.Load());
            Assert.Contains($"'{physicalPath}'", exception.Message);
            Assert.IsType<DirectoryNotFoundException>(exception.InnerException);
            Assert.Contains("Could not find a part of the path", exception.InnerException.Message);
        }

        public class FileInfoImpl : IFileInfo
        {
            public FileInfoImpl(string physicalPath) => PhysicalPath = physicalPath;
            public Stream CreateReadStream() => new MemoryStream();
            public bool Exists => true;
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
