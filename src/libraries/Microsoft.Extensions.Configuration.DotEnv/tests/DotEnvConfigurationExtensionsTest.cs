// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.DotEnv.Test;

public class DotEnvConfigurationExtensionsTest
{
    [Fact]
    public void AddDotEnvFile_ThrowsIfFilePathIsNull()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();

        // Act and Assert
        var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddDotEnvFile(path: null!));

        Assert.Equal("path", ex.ParamName);
    }

    [Fact]
    public void AddDotEnvFile_ThrowsIfFilePathIsEmpty()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();

        // Act and Assert
        var ex = Assert.Throws<ArgumentException>(() => configurationBuilder.AddDotEnvFile(""));

        Assert.Equal("path", ex.ParamName);
    }

    [Fact]
    public void AddDotEnvFile_ThrowsIfFileDoesNotExistAtPath()
    {
        // Arrange
        var path = "file-does-not-exist.env";

        // Act and Assert
        var ex = Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddDotEnvFile(path).Build());
        Assert.Contains(path, ex.Message);
    }

    [Fact]
    public void AddDotEnvStream_ThrowsIfStreamIsNull()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();

        // Act and Assert
        var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddDotEnvStream(stream: null!));
        Assert.Equal("stream", ex.ParamName);
    }

    [Fact]
    public void AddDotEnvStream_ReadsDataFromStream()
    {
        // Arrange
        var dotenv = """
            KEY1=Value1
            KEY2=Value2
            """;

        var stream = TestStreamHelpers.StringToStream(dotenv);

        // Act
        var config = new ConfigurationBuilder()
            .AddDotEnvStream(stream)
            .Build();

        // Assert
        Assert.Equal("Value1", config["KEY1"]);
        Assert.Equal("Value2", config["KEY2"]);
    }

    [Fact]
    public void AddDotEnvFile_SetsUpConfigurationProvider()
    {
        // Arrange & Act
        var configurationBuilder = new ConfigurationBuilder()
            .AddDotEnvFile(source =>
            {
                source.Path = "test.env";
                source.Optional = true;
            });

        var providers = configurationBuilder.Sources;

        // Assert
        Assert.Single(providers);
        Assert.IsType<DotEnvConfigurationSource>(providers[0]);
        
        var dotEnvConfigurationSource = providers[0] as DotEnvConfigurationSource;
        Assert.Equal("test.env", dotEnvConfigurationSource!.Path);
        Assert.True(dotEnvConfigurationSource.Optional);
    }

    [Fact]
    public void AddDotEnvFile_SetPath()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();

        // Act
        configurationBuilder.AddDotEnvFile("test.env", optional: true);
        var providers = configurationBuilder.Sources;

        // Assert
        Assert.Single(providers);
        var provider = Assert.IsType<DotEnvConfigurationSource>(providers[0]);
        Assert.Equal("test.env", provider.Path);
        Assert.True(provider.Optional);
    }

    [Fact]
    public void AddDotEnvFile_SetOptionalToFalseByDefault()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();

        // Act
        configurationBuilder.AddDotEnvFile("test.env");
        var providers = configurationBuilder.Sources;

        // Assert
        Assert.Single(providers);
        var provider = Assert.IsType<DotEnvConfigurationSource>(providers[0]);
        Assert.False(provider.Optional);
    }

    [Fact]
    public void AddDotEnvFile_SetReloadOnChangeToFalseByDefault()
    {
        // Arrange
        var configurationBuilder = new ConfigurationBuilder();

        // Act
        configurationBuilder.AddDotEnvFile("test.env");
        var providers = configurationBuilder.Sources;

        // Assert
        Assert.Single(providers);
        var provider = Assert.IsType<DotEnvConfigurationSource>(providers[0]);
        Assert.False(provider.ReloadOnChange);
    }

    [Fact]
    public void AddDotEnvFiles_LaterFilesOverrideEarlierOnes()
    {
        // Arrange
        var baseEnv = """
            DATABASE_HOST=base-host
            DATABASE_PORT=5432
            API_KEY=base-key
            """;

        var overrideEnv = """
            DATABASE_HOST=override-host
            DEBUG=true
            """;

        var baseStream = TestStreamHelpers.StringToStream(baseEnv);
        var overrideStream = TestStreamHelpers.StringToStream(overrideEnv);

        // Act
        var config = new ConfigurationBuilder()
            .AddDotEnvStream(baseStream)
            .AddDotEnvStream(overrideStream)
            .Build();

        // Assert
        Assert.Equal("override-host", config["DATABASE_HOST"]);
        Assert.Equal("5432", config["DATABASE_PORT"]);
        Assert.Equal("base-key", config["API_KEY"]);
        Assert.Equal("true", config["DEBUG"]);
    }
}
