// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration.Test;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
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
