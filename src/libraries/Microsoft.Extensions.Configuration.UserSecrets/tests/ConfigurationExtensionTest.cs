// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Configuration.UserSecrets.Test;
using Newtonsoft.Json.Linq;
using Xunit;

[assembly: UserSecretsId(ConfigurationExtensionTest.TestSecretsId)]

namespace Microsoft.Extensions.Configuration.UserSecrets.Test
{
    public class ConfigurationExtensionTest : IDisposable
    {
        public const string TestSecretsId = "d6076a6d3ab24c00b2511f10a56c68cc";

        private List<string> _tmpDirectories = new List<string>();

        private void SetSecret(string id, string key, string value)
        {
            var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(id);

            var dir = Path.GetDirectoryName(secretsFilePath);
            Directory.CreateDirectory(dir);
            _tmpDirectories.Add(dir);

            var secrets = new ConfigurationBuilder()
                .AddJsonFile(secretsFilePath, optional: true)
                .Build()
                .AsEnumerable()
                .Where(i => i.Value != null)
                .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);

            secrets[key] = value;

            var contents = new JObject();
            if (secrets != null)
            {
                foreach (var secret in secrets.AsEnumerable())
                {
                    contents[secret.Key] = secret.Value;
                }
            }

            File.WriteAllText(secretsFilePath, contents.ToString(), Encoding.UTF8);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void AddUserSecrets_FindsAssemblyAttribute()
        {
            var randValue = Guid.NewGuid().ToString();
            var configKey = "MyDummySetting";

            SetSecret(TestSecretsId, configKey, randValue);
            var config = new ConfigurationBuilder()
                .AddUserSecrets(typeof(ConfigurationExtensionTest).Assembly)
                .Build();

            Assert.Equal(randValue, config[configKey]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void AddUserSecrets_FindsAssemblyAttributeFromType()
        {
            var randValue = Guid.NewGuid().ToString();
            var configKey = "MyDummySetting";

            SetSecret(TestSecretsId, configKey, randValue);
            var config = new ConfigurationBuilder()
                .AddUserSecrets<ConfigurationExtensionTest>()
                .Build();

            Assert.Equal(randValue, config[configKey]);
        }

        [Fact]
        public void AddUserSecrets_ThrowsIfAssemblyAttributeFromType()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new ConfigurationBuilder().AddUserSecrets<string>());
            Assert.Equal(SR.Format(SR.Error_Missing_UserSecretsIdAttribute, typeof(string).Assembly.GetName().Name),
                ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() =>
                new ConfigurationBuilder().AddUserSecrets(typeof(JObject).Assembly));
            Assert.Equal(SR.Format(SR.Error_Missing_UserSecretsIdAttribute, typeof(JObject).Assembly.GetName().Name),
                ex.Message);
        }


        [Fact]
        public void AddUserSecrets_DoesNotThrowsIfOptional()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<string>(optional: true)
                .AddUserSecrets(typeof(List<>).Assembly, optional: true)
                .Build();

            Assert.Empty(config.AsEnumerable());
        }

        [Fact]
        public void AddUserSecrets_DoesThrowsIfNotOptionalAndSecretDoesNotExist()
        {
            var secretId = Assembly.GetExecutingAssembly().GetName().Name;
            var secretPath = PathHelper.GetSecretsPathFromSecretsId(secretId);
            if (File.Exists(secretPath))
            {
                File.Delete(secretPath);
            }

            Assert.Throws<FileNotFoundException>(() => new ConfigurationBuilder().AddUserSecrets(Assembly.GetExecutingAssembly(), false).Build());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void AddUserSecrets_With_SecretsId_Passed_Explicitly()
        {
            var userSecretsId = Guid.NewGuid().ToString();
            SetSecret(userSecretsId, "Facebook:PLACEHOLDER", "value1");

            var builder = new ConfigurationBuilder().AddUserSecrets(userSecretsId);
            var configuration = builder.Build();

            Assert.Equal("value1", configuration["Facebook:PLACEHOLDER"]);
        }

        [Fact]
        public void AddUserSecrets_Does_Not_Fail_On_Non_Existing_File()
        {
            var userSecretsId = Guid.NewGuid().ToString();
            var secretFilePath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
            var builder = new ConfigurationBuilder().AddUserSecrets(userSecretsId);

            var configuration = builder.Build();
            Assert.Null(configuration["Facebook:PLACEHOLDER"]);
            Assert.False(File.Exists(secretFilePath));
        }

        public void Dispose()
        {
            foreach (var dir in _tmpDirectories)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch
                {
                    Console.WriteLine("Failed to delete " + dir);
                }
            }
        }
    }
}
