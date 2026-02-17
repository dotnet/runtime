// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.DotEnv.Test;

public class DotEnvConfigurationTest
{
    [Fact]
    public void LoadKeyValuePairsFromValidDotEnv()
    {
        var dotenv = """
            # This is a comment
            DATABASE_HOST=localhost
            DATABASE_PORT=5432
            DATABASE_USER=myuser
            DATABASE_PASSWORD=mypassword
            DEBUG=true
            EMPTY_VALUE=
            """;

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        Assert.Equal("localhost", dotenvProvider.Get("DATABASE_HOST"));
        Assert.Equal("5432", dotenvProvider.Get("DATABASE_PORT"));
        Assert.Equal("myuser", dotenvProvider.Get("DATABASE_USER"));
        Assert.Equal("mypassword", dotenvProvider.Get("DATABASE_PASSWORD"));
        Assert.Equal("true", dotenvProvider.Get("DEBUG"));
        Assert.Null(dotenvProvider.Get("EMPTY_VALUE"));
    }

    [Fact]
    public void LoadKeyValuePairsFromValidDotEnvWithQuotes()
    {
        var dotenv = """
            # Test various quote scenarios
            SINGLE_QUOTED='single quoted value'
            DOUBLE_QUOTED="double quoted value"
            QUOTED_WITH_SPACES="value with spaces"
            QUOTED_EMPTY_DOUBLE=""
            QUOTED_EMPTY_SINGLE=''
            ESCAPED_QUOTES="value with "quotes""
            """;

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        Assert.Equal("single quoted value", dotenvProvider.Get("SINGLE_QUOTED"));
        Assert.Equal("double quoted value", dotenvProvider.Get("DOUBLE_QUOTED"));
        Assert.Equal("value with spaces", dotenvProvider.Get("QUOTED_WITH_SPACES"));
        Assert.Equal("", dotenvProvider.Get("QUOTED_EMPTY_DOUBLE"));
        Assert.Equal("", dotenvProvider.Get("QUOTED_EMPTY_SINGLE"));
        Assert.Equal("value with \"quotes\"", dotenvProvider.Get("ESCAPED_QUOTES"));
    }

    [Fact]
    public void LoadKeyValuePairsWithComments()
    {
        var dotenv = """
            # This is a full line comment
            KEY1=value1
            KEY2=value2 # This is an inline comment
            # Another comment
            KEY3=value3
            """;

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        Assert.Equal("value1", dotenvProvider.Get("KEY1"));
        Assert.Equal("value2", dotenvProvider.Get("KEY2"));
        Assert.Equal("value3", dotenvProvider.Get("KEY3"));
    }

    [Fact]
    public void IgnoreInvalidLines()
    {
        var dotenv = """
            VALID_KEY=valid_value
            INVALID_LINE_NO_EQUALS
            =INVALID_LINE_NO_KEY
            KEY_WITH_EMPTY_VALUE=
            ANOTHER_VALID_KEY=another_value
            """;

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        Assert.Equal("valid_value", dotenvProvider.Get("VALID_KEY"));
        Assert.Null(dotenvProvider.Get("KEY_WITH_EMPTY_VALUE"));
        Assert.Equal("another_value", dotenvProvider.Get("ANOTHER_VALID_KEY"));
        Assert.Null(dotenvProvider.Get("INVALID_LINE_NO_EQUALS"));
    }

    [Fact]
    public void LoadEmptyValue()
    {
        var dotenv = "EMPTY=";

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        Assert.Null(dotenvProvider.Get("EMPTY"));
    }

    [Fact]
    public void LoadEscapeSequences()
    {
        var dotenv = """
            NEWLINE="line1\nline2"
            TAB="column1\tcolumn2"
            QUOTE="say "hello"..."
            BACKSLASH="file\\path"
            FORWARD_SLASH="path/to/file"
            """;

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        Assert.Equal("line1\nline2", dotenvProvider.Get("NEWLINE"));
        Assert.Equal("column1\tcolumn2", dotenvProvider.Get("TAB"));
        Assert.Equal("say \"hello\"...", dotenvProvider.Get("QUOTE"));
        Assert.Equal(@"file\path", dotenvProvider.Get("BACKSLASH"));
        Assert.Equal("path/to/file", dotenvProvider.Get("FORWARD_SLASH"));
    }

    [Fact]
    public void ThrowExceptionWhenFileNotFoundAndNotOptional()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddDotEnvFile("NotExistingFile.env", optional: false);

        var exception = Assert.Throws<FileNotFoundException>(() => configurationBuilder.Build());

        Assert.Contains("NotExistingFile.env", exception.Message);
    }

    [Fact]
    public void DoesNotThrowExceptionWhenFileNotFoundAndOptional()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddDotEnvFile("NotExistingFile.env", optional: true);

        var config = configurationBuilder.Build();

        Assert.NotNull(config);
    }

    [Fact]
    public void DotEnvConfigurationProviderToString()
    {
        var provider = new DotEnvConfigurationProvider(new DotEnvConfigurationSource { Path = "Test.env" });

        Assert.Equal("DotEnvConfigurationProvider for 'Test.env' (Required)", provider.ToString());
    }

    [Fact]
    public void KeysAreCaseInsensitive()
    {
        var dotenv = """
            Key=Value1
            KEY=Value2
            kEy=Value3
            """;

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        // Last value should win
        Assert.Equal("Value3", dotenvProvider.Get("Key"));
        Assert.Equal("Value3", dotenvProvider.Get("KEY"));
        Assert.Equal("Value3", dotenvProvider.Get("kEy"));
    }

    [Fact]
    public void LoadEnvironmentVariableExpansion()
    {
        // Arrange - set up some environment variables
        var originalHost = Environment.GetEnvironmentVariable("TEST_HOST");
        var originalPort = Environment.GetEnvironmentVariable("TEST_PORT");
        var originalUser = Environment.GetEnvironmentVariable("TEST_USER");

        try
        {
            Environment.SetEnvironmentVariable("TEST_HOST", "localhost");
            Environment.SetEnvironmentVariable("TEST_PORT", "5432");
            Environment.SetEnvironmentVariable("TEST_USER", "myuser");

            var dotenv = """
                # Test environment variable expansion
                DATABASE_URL=${TEST_HOST}:${TEST_PORT}
                CONNECTION_STRING=host=${TEST_HOST} port=${TEST_PORT} user=${TEST_USER}
                MIXED_VALUE=prefix_${TEST_HOST}_suffix
                DOUBLE_QUOTED="${TEST_HOST}:${TEST_PORT}"
                SINGLE_QUOTED='${TEST_HOST}:${TEST_PORT}'
                DOLLAR_VAR=$TEST_HOST
                UNDEFINED_VAR=${UNDEFINED_VARIABLE}
                """;

            var dotenvConfigSrc = new DotEnvStreamConfigurationSource
            {
                Stream = TestStreamHelpers.StringToStream(dotenv)
            };

            var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
            dotenvProvider.Load();

            // Assert
            Assert.Equal("localhost:5432", dotenvProvider.Get("DATABASE_URL"));
            Assert.Equal("host=localhost port=5432 user=myuser", dotenvProvider.Get("CONNECTION_STRING"));
            Assert.Equal("prefix_localhost_suffix", dotenvProvider.Get("MIXED_VALUE"));
            Assert.Equal("localhost:5432", dotenvProvider.Get("DOUBLE_QUOTED"));
            Assert.Equal("${TEST_HOST}:${TEST_PORT}", dotenvProvider.Get("SINGLE_QUOTED")); // Single quotes preserve literal
            Assert.Equal("localhost", dotenvProvider.Get("DOLLAR_VAR"));
            Assert.Equal("${UNDEFINED_VARIABLE}", dotenvProvider.Get("UNDEFINED_VAR")); // Undefined vars stay as-is
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_HOST", originalHost);
            Environment.SetEnvironmentVariable("TEST_PORT", originalPort);
            Environment.SetEnvironmentVariable("TEST_USER", originalUser);
        }
    }

    [Fact]
    public void EnvironmentVariableExpansionWithEscapedDollar()
    {
        var dotenv = """
            LITERAL_DOLLAR=\$NOT_EXPANDED
            ESCAPED_VAR="prefix \${NOT_A_VAR} suffix"
            """;

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        Assert.Equal("\\$NOT_EXPANDED", dotenvProvider.Get("LITERAL_DOLLAR"));
        Assert.Equal("prefix \\${NOT_A_VAR} suffix", dotenvProvider.Get("ESCAPED_VAR"));
    }

    [Fact]
    public void EnvironmentVariableExpansionEdgeCases()
    {
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("EMPTY_VAR", "");
            Environment.SetEnvironmentVariable("SPACE_VAR", "value with spaces");

            var dotenv = """
                EMPTY_EXPANSION=${EMPTY_VAR}
                SPACE_EXPANSION=${SPACE_VAR}
                PARTIAL_MATCH=${PARTIAL
                NESTED_BRACES=${{INVALID}}
                JUST_DOLLAR=$
                EMPTY_BRACES=${}
                """;

            var dotenvConfigSrc = new DotEnvStreamConfigurationSource
            {
                Stream = TestStreamHelpers.StringToStream(dotenv)
            };

            var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
            dotenvProvider.Load();

            Assert.Equal("", dotenvProvider.Get("EMPTY_EXPANSION"));
            Assert.Equal("value with spaces", dotenvProvider.Get("SPACE_EXPANSION"));
            Assert.Equal("${PARTIAL", dotenvProvider.Get("PARTIAL_MATCH")); // Invalid syntax preserved
            Assert.Equal("${{INVALID}}", dotenvProvider.Get("NESTED_BRACES")); // Invalid syntax preserved
            Assert.Equal("$", dotenvProvider.Get("JUST_DOLLAR"));
            Assert.Equal("${}", dotenvProvider.Get("EMPTY_BRACES")); // Empty var name preserved
        }
        finally
        {
            Environment.SetEnvironmentVariable("EMPTY_VAR", null);
            Environment.SetEnvironmentVariable("SPACE_VAR", null);
        }
    }

    [Fact]
    public void HappyPathWithManyPermutationsAndStandardVariableInterpolation()
    {
        var dotenv = """
            # Basic settings
            APP_NAME=SuperApp
            PORT=8080
            DEBUG=true

            # DB config
            DB_HOST=localhost
            DB_PORT=5432
            DB_USER=admin
            DB_PASS="s3cret!"

            # Array-like list
            ALLOWED_ORIGINS=*,example.com

            # Multiline string (escaped)
            CERTIFICATE="-----BEGIN CERT-----\nMIID...snip...\n-----END CERT-----"

            # Reference another variable
            API_HOST=api.example.com
            API_URL=https://${API_HOST}/v1
            """;

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        Assert.Equal("SuperApp", dotenvProvider.Get("APP_NAME"));
        Assert.Equal("8080", dotenvProvider.Get("PORT"));
        Assert.Equal("true", dotenvProvider.Get("DEBUG"));
        Assert.Equal("localhost", dotenvProvider.Get("DB_HOST"));
        Assert.Equal("5432", dotenvProvider.Get("DB_PORT"));
        Assert.Equal("admin", dotenvProvider.Get("DB_USER"));
        Assert.Equal("s3cret!", dotenvProvider.Get("DB_PASS"));
        Assert.Equal("*,example.com", dotenvProvider.Get("ALLOWED_ORIGINS"));
        Assert.Equal("-----BEGIN CERT-----\nMIID...snip...\n-----END CERT-----", dotenvProvider.Get("CERTIFICATE"));
        Assert.Equal("api.example.com", dotenvProvider.Get("API_HOST"));
        Assert.Equal("https://api.example.com/v1", dotenvProvider.Get("API_URL"));
    }

    [Fact]
    public void LoadInternalVariableInterpolation()
    {
        var dotenv = """
            # Test internal variable interpolation
            HOST=example.com
            PORT=8080
            DATABASE_NAME=myapp
            API_VERSION=v1
            
            # Variables referencing other variables defined in the same file
            API_URL=https://${HOST}/${API_VERSION}
            DATABASE_URL=postgres://${HOST}:${PORT}/${DATABASE_NAME}
            FULL_ENDPOINT=${API_URL}/users
            MIXED_INTERNAL_EXTERNAL=internal_${HOST}_external_${PATH}
            
            # Double-quoted internal variables
            QUOTED_URL="https://${HOST}:${PORT}/api"
            
            # Single-quoted should not expand (literal)
            LITERAL_URL='https://${HOST}:${PORT}/api'
            
            # Undefined internal variable should remain as-is
            UNDEFINED_INTERNAL=${UNDEFINED_VAR}
            """;

        var dotenvConfigSrc = new DotEnvStreamConfigurationSource
        {
            Stream = TestStreamHelpers.StringToStream(dotenv)
        };

        var dotenvProvider = new DotEnvStreamConfigurationProvider(dotenvConfigSrc);
        dotenvProvider.Load();

        // Assert internal variable interpolation
        Assert.Equal("example.com", dotenvProvider.Get("HOST"));
        Assert.Equal("8080", dotenvProvider.Get("PORT"));
        Assert.Equal("myapp", dotenvProvider.Get("DATABASE_NAME"));
        Assert.Equal("v1", dotenvProvider.Get("API_VERSION"));
        
        // Assert interpolated values
        Assert.Equal("https://example.com/v1", dotenvProvider.Get("API_URL"));
        Assert.Equal("postgres://example.com:8080/myapp", dotenvProvider.Get("DATABASE_URL"));
        Assert.Equal("https://example.com/v1/users", dotenvProvider.Get("FULL_ENDPOINT"));
        
        // Mixed internal and external (PATH is an external env var)
        var expectedMixed = $"internal_example.com_external_{Environment.GetEnvironmentVariable("PATH")}";

        Assert.Equal(expectedMixed, dotenvProvider.Get("MIXED_INTERNAL_EXTERNAL"));
        
        // Double-quoted should expand
        Assert.Equal("https://example.com:8080/api", dotenvProvider.Get("QUOTED_URL"));
        
        // Single-quoted should not expand (literal)
        Assert.Equal("https://${HOST}:${PORT}/api", dotenvProvider.Get("LITERAL_URL"));
        
        // Undefined internal variable should remain as-is
        Assert.Equal("${UNDEFINED_VAR}", dotenvProvider.Get("UNDEFINED_INTERNAL"));
    }
}