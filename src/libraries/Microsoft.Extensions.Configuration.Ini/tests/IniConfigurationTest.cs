// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.Ini.Test
{
    public class IniConfigurationTest
    {
        [Fact]
        public void CanLoadValidIniFromStreamProvider()
        {
            var ini = @"[DefaultConnection]
ConnectionString=TestConnectionString
Provider=SqlClient
[Data:Inventory]
ConnectionString=AnotherTestConnectionString
SubHeader:Provider=MySql";
            var config = new ConfigurationBuilder().AddIniStream(TestStreamHelpers.StringToStream(ini)).Build();

            Assert.Equal("TestConnectionString", config["defaultconnection:ConnectionString"]);
            Assert.Equal("SqlClient", config["DEFAULTCONNECTION:PROVIDER"]);
            Assert.Equal("AnotherTestConnectionString", config["Data:Inventory:CONNECTIONSTRING"]);
            Assert.Equal("MySql", config["Data:Inventory:SubHeader:Provider"]);
        }

        [Fact]
        public void ReloadThrowsFromIniStreamProvider()
        {
            var ini = @"[DefaultConnection]
ConnectionString=TestConnectionString
Provider=SqlClient
[Data:Inventory]
ConnectionString=AnotherTestConnectionString
SubHeader:Provider=MySql";
            var config = new ConfigurationBuilder().AddIniStream(TestStreamHelpers.StringToStream(ini)).Build();
            Assert.Throws<InvalidOperationException>(() => config.Reload());
        }

        [Fact]
        public void LoadKeyValuePairsFromValidIniFile()
        {
            var ini = @"[DefaultConnection]
ConnectionString=TestConnectionString
Provider=SqlClient
[Data:Inventory]
ConnectionString=AnotherTestConnectionString
SubHeader:Provider=MySql";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("TestConnectionString", iniConfigSrc.Get("defaultconnection:ConnectionString"));
            Assert.Equal("SqlClient", iniConfigSrc.Get("DEFAULTCONNECTION:PROVIDER"));
            Assert.Equal("AnotherTestConnectionString", iniConfigSrc.Get("Data:Inventory:CONNECTIONSTRING"));
            Assert.Equal("MySql", iniConfigSrc.Get("Data:Inventory:SubHeader:Provider"));
        }

        [Fact]
        public void LoadMethodCanHandleEmptyValue()
        {
            var ini = @"DefaultKey=";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal(string.Empty, iniConfigSrc.Get("DefaultKey"));
        }

        [Fact]
        public void LoadKeyValuePairsFromValidIniFileWithQuotedValues()
        {
            var ini = "[DefaultConnection]\n" +
                      "ConnectionString=\"TestConnectionString\"\n" +
                      "Provider=\"SqlClient\"\n" +
                      "[Data:Inventory]\n" +
                      "ConnectionString=\"AnotherTestConnectionString\"\n" +
                      "Provider=\"MySql\"";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("TestConnectionString", iniConfigSrc.Get("DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", iniConfigSrc.Get("DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", iniConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", iniConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void DoubleQuoteIsPartOfValueIfNotPaired()
        {
            var ini = "[ConnectionString]\n" +
                      "DefaultConnection=\"TestConnectionString\n" +
                      "Provider=SqlClient\"";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("\"TestConnectionString", iniConfigSrc.Get("ConnectionString:DefaultConnection"));
            Assert.Equal("SqlClient\"", iniConfigSrc.Get("ConnectionString:Provider"));
        }

        [Fact]
        public void DoubleQuoteIsPartOfValueIfAppearInTheMiddleOfValue()
        {
            var ini = "[ConnectionString]\n" +
                      "DefaultConnection=Test\"Connection\"String\n" +
                      "Provider=Sql\"Client";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("Test\"Connection\"String", iniConfigSrc.Get("ConnectionString:DefaultConnection"));
            Assert.Equal("Sql\"Client", iniConfigSrc.Get("ConnectionString:Provider"));
        }

        [Fact]
        public void LoadKeyValuePairsFromValidIniFileWithoutSectionHeader()
        {
            var ini = @"
            DefaultConnection:ConnectionString=TestConnectionString
            DefaultConnection:Provider=SqlClient
            Data:Inventory:ConnectionString=AnotherTestConnectionString
            Data:Inventory:Provider=MySql
            ";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("TestConnectionString", iniConfigSrc.Get("DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", iniConfigSrc.Get("DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", iniConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", iniConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void SupportAndIgnoreComments()
        {
            var ini = @"
            ; Comments
            [DefaultConnection]
            # Comments
            ConnectionString=TestConnectionString
            / Comments
            Provider=SqlClient
            [Data:Inventory]
            ConnectionString=AnotherTestConnectionString
            Provider=MySql
            ";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());

            iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini));

            Assert.Equal("TestConnectionString", iniConfigSrc.Get("DefaultConnection:ConnectionString"));
            Assert.Equal("SqlClient", iniConfigSrc.Get("DefaultConnection:Provider"));
            Assert.Equal("AnotherTestConnectionString", iniConfigSrc.Get("Data:Inventory:ConnectionString"));
            Assert.Equal("MySql", iniConfigSrc.Get("Data:Inventory:Provider"));
        }

        [Fact]
        public void ThrowExceptionWhenFoundInvalidLine()
        {
            var ini = @"
ConnectionString
            ";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());
            var expectedMsg = SR.Format(SR.Error_UnrecognizedLineFormat, "ConnectionString");

            var exception = Assert.Throws<FormatException>(() => iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenFoundBrokenSectionHeader()
        {
            var ini = @"
[ConnectionString
DefaultConnection=TestConnectionString
            ";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());
            var expectedMsg = SR.Format(SR.Error_UnrecognizedLineFormat, "[ConnectionString");

            var exception = Assert.Throws<FormatException>(() => iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingNullAsFilePath()
        {
            var expectedMsg = new ArgumentException(SR.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AddIniFile(path: null));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenPassingEmptyStringAsFilePath()
        {
            var expectedMsg = new ArgumentException(SR.Error_InvalidFilePath, "path").Message;

            var exception = Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AddIniFile(string.Empty));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenKeyIsDuplicated()
        {
            var ini = @"
            [Data:DefaultConnection]
            ConnectionString=TestConnectionString
            Provider=SqlClient
            [Data]
            DefaultConnection:ConnectionString=AnotherTestConnectionString
            Provider=MySql
            ";
            var iniConfigSrc = new IniConfigurationProvider(new IniConfigurationSource());
            var expectedMsg = SR.Format(SR.Error_KeyIsDuplicated, "Data:DefaultConnection:ConnectionString");

            var exception = Assert.Throws<FormatException>(() => iniConfigSrc.Load(TestStreamHelpers.StringToStream(ini)));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50867", TestPlatforms.Android)]
        public void IniConfiguration_Throws_On_Missing_Configuration_File()
        {
            var exception = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddIniFile("NotExistingConfig.ini").Build());

            // Assert
            Assert.StartsWith($"The configuration file 'NotExistingConfig.ini' was not found and is not optional. The expected physical path was '", exception.Message);
        }

        [Fact]
        public void IniConfiguration_Does_Not_Throw_On_Optional_Configuration()
        {
            var config = new ConfigurationBuilder().AddIniFile("NotExistingConfig.ini", optional: true).Build();
        }
    }
}
