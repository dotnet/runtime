// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            var root = Environment.GetEnvironmentVariable("APPDATA") ??         // On Windows it goes to %APPDATA%\Microsoft\UserSecrets\
                        Environment.GetEnvironmentVariable("HOME");             // On Mac/Linux it goes to ~/.microsoft/usersecrets/

            var expectedSecretPath = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPDATA")) ?
                Path.Combine(root, "Microsoft", "UserSecrets", userSecretsId, "secrets.json") :
                Path.Combine(root, ".microsoft", "usersecrets", userSecretsId, "secrets.json");

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
    }
}
