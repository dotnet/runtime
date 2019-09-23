// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.Configuration.UserSecrets
{
    /// <summary>
    /// Provides paths for user secrets configuration files.
    /// </summary>
    public class PathHelper
    {
        internal const string SecretsFileName = "secrets.json";

        /// <summary>
        /// <para>
        /// Returns the path to the JSON file that stores user secrets.
        /// </para>
        /// <para>
        /// This uses the current user profile to locate the secrets file on disk in a location outside of source control.
        /// </para>
        /// </summary>
        /// <param name="userSecretsId">The user secret ID.</param>
        /// <returns>The full path to the secret file.</returns>
        public static string GetSecretsPathFromSecretsId(string userSecretsId)
        {
            if (string.IsNullOrEmpty(userSecretsId))
            {
                throw new ArgumentException(Resources.Common_StringNullOrEmpty, nameof(userSecretsId));
            }

            var badCharIndex = userSecretsId.IndexOfAny(Path.GetInvalidFileNameChars());
            if (badCharIndex != -1)
            {
                throw new InvalidOperationException(
                    string.Format(
                        Resources.Error_Invalid_Character_In_UserSecrets_Id,
                        userSecretsId[badCharIndex],
                        badCharIndex));
            }

            const string userSecretsFallbackDir = "DOTNET_USER_SECRETS_FALLBACK_DIR";

            // For backwards compat, this checks env vars first before using Env.GetFolderPath
            var root = Environment.GetEnvironmentVariable("APPDATA")                             // On Windows it goes to %APPDATA%\Microsoft\UserSecrets\
                       ?? Environment.GetEnvironmentVariable("HOME")                             // On Mac/Linux it goes to ~/.microsoft/usersecrets/
                       ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) 
                       ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                       ?? Environment.GetEnvironmentVariable(userSecretsFallbackDir);            // this fallback is an escape hatch if everything else fails

            if (string.IsNullOrEmpty(root))
            {
                throw new InvalidOperationException("Could not determine an appropriate location for storing user secrets. Set the " + userSecretsFallbackDir + " environment variable to a folder where user secrets should be stored.");
            }

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPDATA")))
            {
                return Path.Combine(root, "Microsoft", "UserSecrets", userSecretsId, SecretsFileName);
            }
            else
            {
                return Path.Combine(root, ".microsoft", "usersecrets", userSecretsId, SecretsFileName);
            }
        }
    }
}
