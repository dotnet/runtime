// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Configuration.UserSecrets.Test
{
    public class PathHelperTest
    {
        [Fact]
        public void Gives_Correct_Secret_Path()
        {
            var userSecretsId = "abcxyz123";
            var actualSecretPath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);

            var appData = Environment.GetEnvironmentVariable("APPDATA");
            var root = appData
                ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var expectedSecretPath = !string.IsNullOrEmpty(appData)
                ? Path.Combine(root, "Microsoft", "UserSecrets", userSecretsId, "secrets.json")
                : Path.Combine(root, "Microsoft", "User-secrets", userSecretsId, "secrets.json");

            Assert.Equal(expectedSecretPath, actualSecretPath);
        }

        [Fact]
        public void Throws_If_UserSecretId_Contains_Invalid_Characters()
        {
            foreach (var character in Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()))
            {
                var id = "Test" + character;
                Assert.Throws<InvalidOperationException>(() => PathHelper.GetSecretsPathFromSecretsId(id));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows))]
        public void Secret_Path_On_Non_Windows_Uses_LocalApplicationData()
        {
            var userSecretsId = "test-secrets-id";
            var secretPath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            Assert.Contains(localAppData, secretPath);
            Assert.Contains("Microsoft", secretPath);
            Assert.Contains("User-secrets", secretPath);
            Assert.EndsWith(Path.Combine(userSecretsId, "secrets.json"), secretPath);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows))]
        public void Secret_Path_On_Windows_Uses_AppData()
        {
            var userSecretsId = "test-secrets-id";
            var secretPath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
            var appData = Environment.GetEnvironmentVariable("APPDATA");

            if (!string.IsNullOrEmpty(appData))
            {
                Assert.Contains(appData, secretPath);
                Assert.Contains("Microsoft", secretPath);
                Assert.Contains("UserSecrets", secretPath);
                Assert.EndsWith(Path.Combine(userSecretsId, "secrets.json"), secretPath);
            }
        }
    }
}
