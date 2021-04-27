// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.Configuration.Ini.Test
{
    public class IniConfigurationExtensionsTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddIniFile_ThrowsIfFilePathIsNullOrEmpty(string path)
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            var ex = Assert.Throws<ArgumentException>(
                () => IniConfigurationExtensions.AddIniFile(configurationBuilder, path));
            Assert.Equal("path", ex.ParamName);
            Assert.StartsWith("File path must be a non-empty string.", ex.Message);
        }

        [Fact]
        public void AddIniFile_ThrowsIfFileDoesNotExistAtPath()
        {
            // Arrange
            var path = "file-does-not-exist.ini";
 
            // Act and Assert
            var ex = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddIniFile(path).Build());
            Assert.StartsWith($"The configuration file '{path}' was not found and is not optional. The expected physical path was '", ex.Message);
        }

        [Fact]
        public void AddIniFile_DoesNotThrowsIfFileDoesNotExistAtPathAndOptional()
        {
            // Arrange
            var path = "file-does-not-exist.ini";

            // Act and Assert
            new ConfigurationBuilder().AddIniFile(path, optional: true).Build();
        }

    }
}
