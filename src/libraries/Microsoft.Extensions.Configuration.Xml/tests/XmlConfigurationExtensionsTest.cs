// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace Microsoft.Extensions.Configuration.Xml.Test
{
    public class XmlConfigurationExtensionsTest
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50870", TestPlatforms.Android)]
        public void AddXmlFile_ThrowsIfFileDoesNotExistAtPath()
        {
            var config = new ConfigurationBuilder().AddXmlFile("NotExistingConfig.xml");

            // Arrange
            // Act and Assert
            var ex = Assert.Throws<FileNotFoundException>(() => config.Build());
            Assert.StartsWith($"The configuration file 'NotExistingConfig.xml' was not found and is not optional. The expected physical path was '", ex.Message);
        }
    }
}
