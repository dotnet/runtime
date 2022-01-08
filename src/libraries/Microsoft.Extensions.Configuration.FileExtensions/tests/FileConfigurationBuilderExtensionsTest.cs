// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace Microsoft.Extensions.Configuration.FileExtensions.Test
{
    public class FileConfigurationBuilderExtensionsTest
    {
        [Fact]
        public void SetFileProvider_ThrowsIfBasePathIsNull()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.SetBasePath(basePath: null));
            Assert.Equal("basePath", ex.ParamName);
        }

        [Fact]
        public void SetFileProvider_CheckPropertiesValueOnBuilder()
        {
            var expectedBasePath = Directory.GetCurrentDirectory();
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.SetBasePath(expectedBasePath);
            var physicalProvider = configurationBuilder.GetFileProvider() as PhysicalFileProvider;
            Assert.NotNull(physicalProvider);
            Assert.Equal(EnsureTrailingSlash(expectedBasePath), physicalProvider.Root);
        }

        [Fact]
        public void GetFileProvider_ReturnPhysicalProviderWithBaseDirectoryIfNotSet()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act
            var physicalProvider = configurationBuilder.GetFileProvider() as PhysicalFileProvider;

            string expectedPath;

            expectedPath = AppContext.BaseDirectory;

            Assert.NotNull(physicalProvider);
            Assert.Equal(EnsureTrailingSlash(expectedPath), physicalProvider.Root);
        }

        [Fact]
        public void GetFileProvider_ReturnTheSamePhysicalFileProviderIfNotSet()
        {
            var configurationBuilder = new ConfigurationBuilder();
            Assert.Same(configurationBuilder.GetFileProvider(), configurationBuilder.GetFileProvider());
        }

        [Fact]
        public void EnsureDefault_CreateSharedPhysicalFileProviderWithBaseDirectoryIfNotSet()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var source1 = new FileConfigurationSourceImpl();
            var source2 = new FileConfigurationSourceImpl();

            source1.EnsureDefaults(configurationBuilder);
            source2.EnsureDefaults(configurationBuilder);

            Assert.Same(configurationBuilder.Properties["FileProvider"], source1.FileProvider);
            Assert.Same(configurationBuilder.Properties["FileProvider"], source2.FileProvider);
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (!string.IsNullOrEmpty(path) &&
                path[path.Length - 1] != Path.DirectorySeparatorChar)
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }
    }
}
