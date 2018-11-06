// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Configuration.Ini;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Xml;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationTests : IDisposable
    {
        private readonly DisposableFileSystem _fileSystem;
        private readonly PhysicalFileProvider _fileProvider;
        private readonly string _basePath;
        private readonly string _iniFile;
        private readonly string _xmlFile;
        private readonly string _jsonFile;
        private static readonly string _iniConfigFileContent =
            @"IniKey1=IniValue1
[IniKey2]
# Comments
IniKey3=IniValue2
; Comments
IniKey4=IniValue3
IniKey5:IniKey6=IniValue4
/Comments
[CommonKey1:CommonKey2]
IniKey7=IniValue5
CommonKey3:CommonKey4=IniValue6";
        private static readonly string _xmlConfigFileContent =
            @"<settings XmlKey1=""XmlValue1"">
  <!-- Comments -->
  <XmlKey2 XmlKey3=""XmlValue2"">
    <!-- Comments -->
    <XmlKey4>XmlValue3</XmlKey4>
    <XmlKey5 Name=""XmlKey6"">XmlValue4</XmlKey5>
  </XmlKey2>
  <CommonKey1 Name=""CommonKey2"" XmlKey7=""XmlValue5"">
    <!-- Comments -->
    <CommonKey3 CommonKey4=""XmlValue6"" />
  </CommonKey1>
</settings>";
        private static readonly string _jsonConfigFileContent =
            @"{
  ""JsonKey1"": ""JsonValue1"",
  ""Json.Key2"": {
    ""JsonKey3"": ""JsonValue2"",
    ""Json.Key4"": ""JsonValue3"",
    ""JsonKey5:JsonKey6"": ""JsonValue4""
  },
  ""CommonKey1"": {
    ""CommonKey2"": {
      ""JsonKey7"": ""JsonValue5"",
      ""CommonKey3:CommonKey4"": ""JsonValue6""
    }
  }
}";
        private static readonly Dictionary<string, string> _memConfigContent = new Dictionary<string, string>
            {
                { "MemKey1", "MemValue1" },
                { "MemKey2:MemKey3", "MemValue2" },
                { "MemKey2:MemKey4", "MemValue3" },
                { "MemKey2:MemKey5:MemKey6", "MemValue4" },
                { "CommonKey1:CommonKey2:MemKey7", "MemValue5" },
                { "CommonKey1:CommonKey2:CommonKey3:CommonKey4", "MemValue6" }
            };

        public ConfigurationTests()
        {
            _fileSystem = new DisposableFileSystem();
            _fileProvider = new PhysicalFileProvider(_fileSystem.RootPath);
            _basePath = AppContext.BaseDirectory ?? string.Empty;

            _iniFile = Path.GetRandomFileName();
            _xmlFile = Path.GetRandomFileName();
            _jsonFile = Path.GetRandomFileName();
        }

        [Fact]
        public void MissingFileIncludesAbsolutePathIfPhysicalFileProvider()
        {
            var error = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddIniFile("missing.ini").Build());
            Assert.True(error.Message.Contains(_basePath), error.Message);
            error = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddJsonFile("missing.json").Build());
            Assert.True(error.Message.Contains(_basePath), error.Message);
            error = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddXmlFile("missing.xml").Build());
            Assert.True(error.Message.Contains(_basePath), error.Message);
        }

        private class NotVeryGoodFileProvider : IFileProvider
        {
            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                return null;
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                return null;
            }

            public IChangeToken Watch(string filter)
            {
                return null;
            }
        }

        private class MissingFile : IFileInfo
        {
            public bool Exists
            {
                get
                {
                    return false;
                }
            }

            public bool IsDirectory
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public DateTimeOffset LastModified
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public long Length
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string Name
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public string PhysicalPath
            {
                get
                {
                    return null;
                }
            }

            public Stream CreateReadStream()
            {
                throw new NotImplementedException();
            }
        }

        private class AlwaysMissingFileProvider : IFileProvider
        {
            public IDirectoryContents GetDirectoryContents(string subpath)
            {
                return null;
            }

            public IFileInfo GetFileInfo(string subpath)
            {
                return new MissingFile();
            }

            public IChangeToken Watch(string filter)
            {
                return null;
            }
        }

        private void WriteTestFiles()
        {
            _fileSystem.WriteFile(_iniFile, _iniConfigFileContent);
            _fileSystem.WriteFile(_xmlFile, _xmlConfigFileContent);
            _fileSystem.WriteFile(_jsonFile, _jsonConfigFileContent);
        }

        private IConfigurationBuilder CreateBuilder()
        {
            return new ConfigurationBuilder().SetFileProvider(_fileProvider);
        }

        [Fact]
        public void MissingFileDoesNotIncludesAbsolutePathIfWithNullFileInfo()
        {
            var provider = new NotVeryGoodFileProvider();
            var error = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddIniFile(provider, "missing.ini", optional: false, reloadOnChange: false).Build());
            Assert.False(error.Message.Contains(_basePath), error.Message);
            error = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddJsonFile(provider, "missing.json", optional: false, reloadOnChange: false).Build());
            Assert.False(error.Message.Contains(_basePath), error.Message);
            error = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddXmlFile(provider, "missing.xml", optional: false, reloadOnChange: false).Build());
            Assert.False(error.Message.Contains(_basePath), error.Message);
        }

        [Fact]
        public void MissingFileDoesNotIncludesAbsolutePathIfWithNoPhysicalPath()
        {
            var provider = new AlwaysMissingFileProvider();
            var error = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddIniFile(provider, "missing.ini", optional: false, reloadOnChange: false).Build());
            Assert.False(error.Message.Contains(_basePath), error.Message);
            error = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddJsonFile(provider, "missing.json", optional: false, reloadOnChange: false).Build());
            Assert.False(error.Message.Contains(_basePath), error.Message);
            error = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddXmlFile(provider, "missing.xml", optional: false, reloadOnChange: false).Build());
            Assert.False(error.Message.Contains(_basePath), error.Message);
        }

        [Fact]
        public void LoadAndCombineKeyValuePairsFromDifferentConfigurationProviders()
        {
            // Arrange
            _fileSystem.WriteFile(_iniFile, _iniConfigFileContent);
            _fileSystem.WriteFile(_xmlFile, _xmlConfigFileContent);
            _fileSystem.WriteFile(_jsonFile, _jsonConfigFileContent);
            var config = CreateBuilder()
                .AddIniFile(_iniFile)
                .AddJsonFile(_jsonFile)
                .AddXmlFile(_xmlFile)
                .AddInMemoryCollection(_memConfigContent)
                .Build();

            // Assert
            Assert.Equal("IniValue1", config["IniKey1"]);
            Assert.Equal("IniValue2", config["IniKey2:IniKey3"]);
            Assert.Equal("IniValue3", config["IniKey2:IniKey4"]);
            Assert.Equal("IniValue4", config["IniKey2:IniKey5:IniKey6"]);
            Assert.Equal("IniValue5", config["CommonKey1:CommonKey2:IniKey7"]);

            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("JsonValue2", config["Json.Key2:JsonKey3"]);
            Assert.Equal("JsonValue3", config["Json.Key2:Json.Key4"]);
            Assert.Equal("JsonValue4", config["Json.Key2:JsonKey5:JsonKey6"]);
            Assert.Equal("JsonValue5", config["CommonKey1:CommonKey2:JsonKey7"]);

            Assert.Equal("XmlValue1", config["XmlKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey2:XmlKey3"]);
            Assert.Equal("XmlValue3", config["XmlKey2:XmlKey4"]);
            Assert.Equal("XmlValue4", config["XmlKey2:XmlKey5:XmlKey6"]);
            Assert.Equal("XmlValue5", config["CommonKey1:CommonKey2:XmlKey7"]);

            Assert.Equal("MemValue1", config["MemKey1"]);
            Assert.Equal("MemValue2", config["MemKey2:MemKey3"]);
            Assert.Equal("MemValue3", config["MemKey2:MemKey4"]);
            Assert.Equal("MemValue4", config["MemKey2:MemKey5:MemKey6"]);
            Assert.Equal("MemValue5", config["CommonKey1:CommonKey2:MemKey7"]);

            Assert.Equal("MemValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);
        }

        [Fact]
        public void LoadAndCombineKeyValuePairsFromDifferentConfigurationProvidersWithAbsolutePath()
        {
            // Arrange
            WriteTestFiles();
            var configurationBuilder = new ConfigurationBuilder();

            // Act
            configurationBuilder.AddIniFile(Path.Combine(_fileSystem.RootPath, _iniFile));
            configurationBuilder.AddJsonFile(Path.Combine(_fileSystem.RootPath, _jsonFile));
            configurationBuilder.AddXmlFile(Path.Combine(_fileSystem.RootPath, _xmlFile));
            configurationBuilder.AddInMemoryCollection(_memConfigContent);

            var config = configurationBuilder.Build();

            // Assert
            Assert.Equal("IniValue1", config["IniKey1"]);
            Assert.Equal("IniValue2", config["IniKey2:IniKey3"]);
            Assert.Equal("IniValue3", config["IniKey2:IniKey4"]);
            Assert.Equal("IniValue4", config["IniKey2:IniKey5:IniKey6"]);
            Assert.Equal("IniValue5", config["CommonKey1:CommonKey2:IniKey7"]);

            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("JsonValue2", config["Json.Key2:JsonKey3"]);
            Assert.Equal("JsonValue3", config["Json.Key2:Json.Key4"]);
            Assert.Equal("JsonValue4", config["Json.Key2:JsonKey5:JsonKey6"]);
            Assert.Equal("JsonValue5", config["CommonKey1:CommonKey2:JsonKey7"]);

            Assert.Equal("XmlValue1", config["XmlKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey2:XmlKey3"]);
            Assert.Equal("XmlValue3", config["XmlKey2:XmlKey4"]);
            Assert.Equal("XmlValue4", config["XmlKey2:XmlKey5:XmlKey6"]);
            Assert.Equal("XmlValue5", config["CommonKey1:CommonKey2:XmlKey7"]);

            Assert.Equal("MemValue1", config["MemKey1"]);
            Assert.Equal("MemValue2", config["MemKey2:MemKey3"]);
            Assert.Equal("MemValue3", config["MemKey2:MemKey4"]);
            Assert.Equal("MemValue4", config["MemKey2:MemKey5:MemKey6"]);
            Assert.Equal("MemValue5", config["CommonKey1:CommonKey2:MemKey7"]);

            Assert.Equal("MemValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);
        }

        [Fact]
        public void CanOverrideValuesWithNewConfigurationProvider()
        {
            // Arrange
            WriteTestFiles();
            var configurationBuilder = CreateBuilder();

            // Act & Assert
            configurationBuilder.AddIniFile(_iniFile);
            var config = configurationBuilder.Build();
            Assert.Equal("IniValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);

            configurationBuilder.AddJsonFile(_jsonFile);
            config = configurationBuilder.Build();
            Assert.Equal("JsonValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);

            configurationBuilder.AddXmlFile(_xmlFile);
            config = configurationBuilder.Build();
            Assert.Equal("XmlValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);

            configurationBuilder.AddInMemoryCollection(_memConfigContent);
            config = configurationBuilder.Build();
            Assert.Equal("MemValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);
        }

        private IConfigurationRoot BuildConfig()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddIniFile(Path.GetFileName(_iniFile));
            configurationBuilder.AddJsonFile(Path.GetFileName(_jsonFile));
            configurationBuilder.AddXmlFile(Path.GetFileName(_xmlFile));
            return configurationBuilder.Build();
        }

        public class TestIniSourceProvider : IniConfigurationProvider, IConfigurationSource
        {
            public TestIniSourceProvider(string path)
                : base(new IniConfigurationSource { Path = path })
            { }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                Source.FileProvider = builder.GetFileProvider();
                return this;
            }
        }

        public class TestJsonSourceProvider : JsonConfigurationProvider, IConfigurationSource
        {
            public TestJsonSourceProvider(string path)
                : base(new JsonConfigurationSource { Path = path })
            { }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                Source.FileProvider = builder.GetFileProvider();
                return this;
            }
        }

        public class TestXmlSourceProvider : XmlConfigurationProvider, IConfigurationSource
        {
            public TestXmlSourceProvider(string path)
                : base(new XmlConfigurationSource { Path = path })
            { }

            public IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                Source.FileProvider = builder.GetFileProvider();
                return this;
            }
        }

        [Fact]
        public void OnLoadErrorWillBeCalledOnJsonParseError()
        {
            // Arrange
            File.WriteAllText(Path.Combine(_basePath, "error.json"), @"{""JsonKey1"": ");

            FileConfigurationProvider provider = null;
            Exception jsonError = null;
            Action<FileLoadExceptionContext> jsonLoadError = c =>
            {
                jsonError = c.Exception;
                provider = c.Provider;
            };

            try
            {
                new ConfigurationBuilder().AddJsonFile("error.json")
                    .SetFileLoadExceptionHandler(jsonLoadError)
                    .Build();
            }
            catch (FormatException e)
            {
                Assert.Equal(e, jsonError);
            }
            Assert.NotNull(provider);
        }

        [Fact]
        public void OnLoadErrorWillBeCalledOnXmlParseError()
        {
            // Arrange
            _fileSystem.WriteFile("error.xml", @"gobblygook");

            FileConfigurationProvider provider = null;
            Exception error = null;
            Action<FileLoadExceptionContext> loadError = c =>
            {
                error = c.Exception;
                provider = c.Provider;
            };

            try
            {
                CreateBuilder().AddJsonFile("error.xml")
                    .SetFileLoadExceptionHandler(loadError)
                    .Build();
            }
            catch (FormatException e)
            {
                Assert.Equal(e, error);
            }
            Assert.NotNull(provider);
        }

        [Fact]
        public void OnLoadErrorWillBeCalledOnIniLoadError()
        {
            // Arrange
            _fileSystem.WriteFile("error.ini", @"IniKey1=IniValue1
IniKey1=IniValue2");

            FileConfigurationProvider provider = null;
            Exception error = null;
            Action<FileLoadExceptionContext> loadError = c =>
            {
                error = c.Exception;
                provider = c.Provider;
            };

            try
            {
                CreateBuilder().AddIniFile("error.ini")
                    .SetFileLoadExceptionHandler(loadError)
                    .Build();
            }
            catch (FormatException e)
            {
                Assert.Equal(e, error);
            }
            Assert.NotNull(provider);
        }

        [Fact]
        public void OnLoadErrorCanIgnoreErrors()
        {
            // Arrange
            _fileSystem.WriteFile("error.json", @"{""JsonKey1"": ");

            FileConfigurationProvider provider = null;
            Action<FileLoadExceptionContext> jsonLoadError = c =>
            {
                provider = c.Provider;
                c.Ignore = true;
            };

            CreateBuilder()
                .AddJsonFile(s =>
                {
                    s.Path = "error.json";
                    s.OnLoadException = jsonLoadError;
                })
                .Build();

            Assert.NotNull(provider);
        }

        [Fact]
        public void CanSetValuesAndReloadValues()
        {
            // Arrange
            WriteTestFiles();
            var configurationBuilder = CreateBuilder();
            configurationBuilder.Add(new TestIniSourceProvider(_iniFile));
            configurationBuilder.Add(new TestJsonSourceProvider(_jsonFile));
            configurationBuilder.Add(new TestXmlSourceProvider(_xmlFile));

            var config = configurationBuilder.Build();

            // Act & Assert
            // Set value
            config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"] = "NewValue";

            // All config sources must be updated
            foreach (var provider in configurationBuilder.Sources)
            {
                Assert.Equal("NewValue",
                    (provider as FileConfigurationProvider).Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));
            }

            // Recover values by reloading
            config.Reload();

            Assert.Equal("XmlValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);

            // Set value with indexer
            config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"] = "NewValue";

            // All config sources must be updated
            foreach (var provider in configurationBuilder.Sources)
            {
                Assert.Equal("NewValue",
                    (provider as FileConfigurationProvider).Get("CommonKey1:CommonKey2:CommonKey3:CommonKey4"));
            }

            // Recover values by reloading
            config.Reload();
            Assert.Equal("XmlValue6", config["CommonKey1:CommonKey2:CommonKey3:CommonKey4"]);
        }

        [Fact]
        public async Task ReloadOnChangeWorksAfterError()
        {
            _fileSystem.WriteFile("reload.json", @"{""JsonKey1"": ""JsonValue1""}");
            var config = CreateBuilder()
                .AddJsonFile("reload.json", optional: false, reloadOnChange: true)
                .Build();
            Assert.Equal("JsonValue1", config["JsonKey1"]);

            // Introduce an error and make sure the old key is removed
            _fileSystem.WriteFile("reload.json", @"{""JsonKey1"": ");
            
            var i = 0;
            while (i < 10 && config["JsonKey1"] != null)
            {
                await Task.Delay(2000); // wait for notification
                i++;
            }
            
            Assert.Null(config["JsonKey1"]);

            // Update the file again to make sure the config is updated
            _fileSystem.WriteFile("reload.json", @"{""JsonKey1"": ""JsonValue2""}");
            
            i = 0;
            while (i < 10 && config["JsonKey1"] == null)
            {
                await Task.Delay(1100); // wait for notification
                i++;
            }
            
            Assert.Equal("JsonValue2", config["JsonKey1"]);
        }

        [Fact]
        public async Task TouchingFileWillReload()
        {
            // Arrange
            _fileSystem.WriteFile("reload.json", @"{""JsonKey1"": ""JsonValue1""}");
            _fileSystem.WriteFile("reload.ini", @"IniKey1 = IniValue1");
            _fileSystem.WriteFile("reload.xml", @"<settings XmlKey1=""XmlValue1""/>");

            var config = CreateBuilder()
                .AddIniFile("reload.ini", optional: false, reloadOnChange: true)
                .AddJsonFile("reload.json", optional: false, reloadOnChange: true)
                .AddXmlFile("reload.xml", optional: false, reloadOnChange: true)
                .Build();

            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("IniValue1", config["IniKey1"]);
            Assert.Equal("XmlValue1", config["XmlKey1"]);

            var token = config.GetReloadToken();

            // Act & Assert
            // Update files
            _fileSystem.WriteFile("reload.json", @"{""JsonKey1"": ""JsonValue2""}");
            _fileSystem.WriteFile("reload.ini", @"IniKey1 = IniValue2");
            _fileSystem.WriteFile("reload.xml", @"<settings XmlKey1=""XmlValue2""/>");

            await Task.Delay(2000);

            Assert.Equal("JsonValue2", config["JsonKey1"]);
            Assert.Equal("IniValue2", config["IniKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey1"]);
            Assert.True(token.HasChanged);
        }

        [Fact]
        public async Task CreatingOptionalFileInNonExistentDirectoryWillReload()
        {
            var directory = Path.GetRandomFileName();
            var jsonRootRelativeFile = Path.Combine(directory, Path.GetRandomFileName());
            var jsonAbsoluteFile = Path.Combine(_fileSystem.RootPath, jsonRootRelativeFile);

            // Arrange
            var config = new ConfigurationBuilder()
                .AddJsonFile(jsonAbsoluteFile, optional: true, reloadOnChange: true)
                .Build();

            Assert.Null(config["JsonKey1"]);

            var createToken = config.GetReloadToken();
            Assert.False(createToken.HasChanged);

            _fileSystem.CreateFolder(directory);

            await Task.Delay(500);

            _fileSystem.WriteFile(jsonRootRelativeFile, @"{""JsonKey1"": ""JsonValue1""}");

            await Task.Delay(2500);

            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.True(createToken.HasChanged);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DeletingFilesThatRedefineKeysWithReload(bool optional)
        {
            // Arrange
            _fileSystem.WriteFile(_jsonFile, @"{""Key"": ""JsonValue1""}");
            _fileSystem.WriteFile(_iniFile, @"Key = IniValue1");
            _fileSystem.WriteFile(_xmlFile, @"<settings Key=""XmlValue1""/>");

            // Act & Assert
            var config = CreateBuilder()
                .AddXmlFile(_xmlFile, optional, reloadOnChange: true)
                .AddJsonFile(_jsonFile, optional, reloadOnChange: true)
                .AddIniFile(_iniFile, optional, reloadOnChange: true)
                .Build();
            Assert.Equal("IniValue1", config["Key"]);

            // Delete files and ensure order is preserved
            var token = config.GetReloadToken();
            _fileSystem.DeleteFile(_iniFile);
            await Task.Delay(2000);
            Assert.Equal("JsonValue1", config["Key"]);
            Assert.True(token.HasChanged);

            token = config.GetReloadToken();
            _fileSystem.DeleteFile(_jsonFile);
            await Task.Delay(2000);
            Assert.Equal("XmlValue1", config["Key"]);
            Assert.True(token.HasChanged);

            token = config.GetReloadToken();
            _fileSystem.DeleteFile(_xmlFile);
            await Task.Delay(2000);
            Assert.Null(config["Key"]);
            Assert.True(token.HasChanged);

            token = config.GetReloadToken();
            _fileSystem.WriteFile(_jsonFile, @"{""Key"": ""JsonValue1""}");
            await Task.Delay(2000);
            Assert.Equal("JsonValue1", config["Key"]);
            Assert.True(token.HasChanged);

            // Adding a file earlier in the chain has no effect
            token = config.GetReloadToken();
            _fileSystem.WriteFile(_xmlFile, @"<settings Key=""XmlValue1""/>");
            await Task.Delay(2000);
            Assert.Equal("JsonValue1", config["Key"]);
            Assert.True(token.HasChanged);

            token = config.GetReloadToken();
            _fileSystem.WriteFile(_iniFile, @"Key = IniValue1");
            await Task.Delay(2000);
            Assert.Equal("IniValue1", config["Key"]);
            Assert.True(token.HasChanged);
        }


        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DeletingFileWillReload(bool optional)
        {
            // Arrange
            _fileSystem.WriteFile(_jsonFile, @"{""JsonKey1"": ""JsonValue1""}");
            _fileSystem.WriteFile(_iniFile, @"IniKey1 = IniValue1");
            _fileSystem.WriteFile(_xmlFile, @"<settings XmlKey1=""XmlValue1""/>");

            // Act & Assert
            var config = CreateBuilder()
                .AddXmlFile(_xmlFile, optional, reloadOnChange: true)
                .AddJsonFile(_jsonFile, optional, reloadOnChange: true)
                .AddIniFile(_iniFile, optional, reloadOnChange: true)
                .Build();

            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("IniValue1", config["IniKey1"]);
            Assert.Equal("XmlValue1", config["XmlKey1"]);

            var token = config.GetReloadToken();

            // Delete files
            _fileSystem.DeleteFile(_jsonFile);
            _fileSystem.DeleteFile(_iniFile);
            _fileSystem.DeleteFile(_xmlFile);

            await Task.Delay(3300);

            Assert.Null(config["JsonKey1"]);
            Assert.Null(config["IniKey1"]);
            Assert.Null(config["XmlKey1"]);
            Assert.True(token.HasChanged);
        }

        [Fact]
        public async Task CreatingWritingDeletingCreatingFileWillReload()
        {
            // Arrange
            var config = CreateBuilder()
                .AddIniFile(_iniFile, optional: true, reloadOnChange: true)
                .AddJsonFile(_jsonFile, optional: true, reloadOnChange: true)
                .AddXmlFile(_xmlFile, optional: true, reloadOnChange: true)
                .Build();

            Assert.Null(config["JsonKey1"]);
            Assert.Null(config["IniKey1"]);
            Assert.Null(config["XmlKey1"]);

            var createToken = config.GetReloadToken();

            _fileSystem.WriteFile(_jsonFile, @"{""JsonKey1"": ""JsonValue1""}");
            _fileSystem.WriteFile(_iniFile, @"IniKey1 = IniValue1");
            _fileSystem.WriteFile(_xmlFile, @"<settings XmlKey1=""XmlValue1""/>");

            await Task.Delay(2000);

            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("IniValue1", config["IniKey1"]);
            Assert.Equal("XmlValue1", config["XmlKey1"]);
            Assert.True(createToken.HasChanged);

            var writeToken = config.GetReloadToken();

            _fileSystem.WriteFile(_jsonFile, @"{""JsonKey1"": ""JsonValue2""}");
            _fileSystem.WriteFile(_iniFile, @"IniKey1 = IniValue2");
            _fileSystem.WriteFile(_xmlFile, @"<settings XmlKey1=""XmlValue2""/>");

            await Task.Delay(2000);

            Assert.Equal("JsonValue2", config["JsonKey1"]);
            Assert.Equal("IniValue2", config["IniKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey1"]);
            Assert.True(writeToken.HasChanged);

            var deleteToken = config.GetReloadToken();

            // Act & Assert
            // Delete files
            _fileSystem.DeleteFile(_jsonFile);
            _fileSystem.DeleteFile(_iniFile);
            _fileSystem.DeleteFile(_xmlFile);

            await Task.Delay(2000);

            Assert.Null(config["JsonKey1"]);
            Assert.Null(config["IniKey1"]);
            Assert.Null(config["XmlKey1"]);
            Assert.True(deleteToken.HasChanged);

            var createAgainToken = config.GetReloadToken();

            _fileSystem.WriteFile(_jsonFile, @"{""JsonKey1"": ""JsonValue1""}");
            _fileSystem.WriteFile(_iniFile, @"IniKey1 = IniValue1");
            _fileSystem.WriteFile(_xmlFile, @"<settings XmlKey1=""XmlValue1""/>");

            await Task.Delay(2000);

            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("IniValue1", config["IniKey1"]);
            Assert.Equal("XmlValue1", config["XmlKey1"]);
            Assert.True(createAgainToken.HasChanged);
        }

        [Fact]
        public void LoadIncorrectJsonFile_ThrowFormatException()
        {
            // Arrange
            var json = @"{
                'name': 'test',
                'address': {
                    'street': 'Something street' /*Missing comma*/
                    'zipcode': '12345'
                }
            }";
            _fileSystem.WriteFile(_jsonFile, json);

            // Act & Assert
            var exception = Assert.Throws<FormatException>(() => CreateBuilder().AddJsonFile(_jsonFile).Build());
            Assert.NotNull(exception.Message);
        }

        [Fact]
        public void SetBasePathCalledMultipleTimesForEachSourceLastOneWins()
        {
            // Arrange
            var builder = new ConfigurationBuilder();
            var jsonConfigFilePath = "test.json";
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), jsonConfigFilePath), _jsonConfigFileContent);
            var xmlConfigFilePath = "test.xml";
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), xmlConfigFilePath), _xmlConfigFileContent);

            // Act
            builder.AddXmlFile("test.xml")
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("test.json");

            var config = builder.Build();

            // Assert
            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("JsonValue2", config["Json.Key2:JsonKey3"]);
            Assert.Equal("JsonValue3", config["Json.Key2:Json.Key4"]);
            Assert.Equal("JsonValue4", config["Json.Key2:JsonKey5:JsonKey6"]);
            Assert.Equal("JsonValue5", config["CommonKey1:CommonKey2:JsonKey7"]);

            Assert.Equal("XmlValue1", config["XmlKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey2:XmlKey3"]);
            Assert.Equal("XmlValue3", config["XmlKey2:XmlKey4"]);
            Assert.Equal("XmlValue4", config["XmlKey2:XmlKey5:XmlKey6"]);

            File.Delete(jsonConfigFilePath);
        }

        [Fact]
        public void GetDefaultBasePathForSources()
        {
            // Arrange
            var builder = new ConfigurationBuilder();

            var jsonConfigFilePath = Path.Combine(_basePath, "test.json");
            var xmlConfigFilePath = Path.Combine(_basePath, "xmltest.xml");
            File.WriteAllText(jsonConfigFilePath, _jsonConfigFileContent);
            File.WriteAllText(xmlConfigFilePath, _xmlConfigFileContent);

            // Act
            builder.AddJsonFile("test.json").AddXmlFile("xmltest.xml");

            var config = builder.Build();

            // Assert
            Assert.Equal("JsonValue1", config["JsonKey1"]);
            Assert.Equal("JsonValue2", config["Json.Key2:JsonKey3"]);
            Assert.Equal("JsonValue3", config["Json.Key2:Json.Key4"]);
            Assert.Equal("JsonValue4", config["Json.Key2:JsonKey5:JsonKey6"]);
            Assert.Equal("JsonValue5", config["CommonKey1:CommonKey2:JsonKey7"]);

            Assert.Equal("XmlValue1", config["XmlKey1"]);
            Assert.Equal("XmlValue2", config["XmlKey2:XmlKey3"]);
            Assert.Equal("XmlValue3", config["XmlKey2:XmlKey4"]);
            Assert.Equal("XmlValue4", config["XmlKey2:XmlKey5:XmlKey6"]);

            File.Delete(jsonConfigFilePath);
            File.Delete(xmlConfigFilePath);
        }

        [Fact]
        public void CanEnumerateProviders()
        {
            // Arrange
            var config = CreateBuilder()
                .AddIniFile(_iniFile, optional: true, reloadOnChange: true)
                .AddJsonFile(_jsonFile, optional: true, reloadOnChange: true)
                .AddXmlFile(_xmlFile, optional: true, reloadOnChange: true)
                .Build();

            var providers = config.Providers;
            Assert.Equal(3, providers.Count());
            Assert.NotNull(providers.Single(p => p is JsonConfigurationProvider));
            Assert.NotNull(providers.Single(p => p is XmlConfigurationProvider));
            Assert.NotNull(providers.Single(p => p is IniConfigurationProvider));
        }

        public void Dispose()
        {
            _fileProvider.Dispose();
            _fileSystem.Dispose();
        }
    }
}
