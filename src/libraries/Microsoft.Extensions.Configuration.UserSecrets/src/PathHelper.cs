// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            return InternalGetSecretsPathFromSecretsId(userSecretsId, throwIfNoRoot: true);
        }

        /// <summary>
        /// <para>
        /// Returns the path to the JSON file that stores user secrets or throws exception if not found.
        /// </para>
        /// <para>
        /// This uses the current user profile to locate the secrets file on disk in a location outside of source control.
        /// </para>
        /// </summary>
        /// <param name="userSecretsId">The user secret ID.</param>
        /// <param name="throwIfNoRoot">specifies if an exception should be thrown when no root for user secrets is found</param>
        /// <returns>The full path to the secret file.</returns>
        internal static string InternalGetSecretsPathFromSecretsId(string userSecretsId, bool throwIfNoRoot)
        {
            if (string.IsNullOrEmpty(userSecretsId))
            {
                throw new ArgumentException(SR.Common_StringNullOrEmpty, nameof(userSecretsId));
            }

            int badCharIndex = userSecretsId.IndexOfAny(Path.GetInvalidFileNameChars());
            if (badCharIndex != -1)
            {
                throw new InvalidOperationException(
                    string.Format(
                        SR.Error_Invalid_Character_In_UserSecrets_Id,
                        userSecretsId[badCharIndex],
                        badCharIndex));
            }

            const string userSecretsFallbackDir = "DOTNET_USER_SECRETS_FALLBACK_DIR";

            // For backwards compat, this checks env vars first before using Env.GetFolderPath
            string? appData = Environment.GetEnvironmentVariable("APPDATA");
            string? root = appData                                                                   // On Windows it goes to %APPDATA%\Microsoft\UserSecrets\
                       ?? Environment.GetEnvironmentVariable("HOME")                             // On Mac/Linux it goes to ~/.microsoft/usersecrets/
                       ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                       ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                       ?? Environment.GetEnvironmentVariable(userSecretsFallbackDir);            // this fallback is an escape hatch if everything else fails

            if (string.IsNullOrEmpty(root))
            {
                if (throwIfNoRoot)
                {
                    throw new InvalidOperationException(SR.Format(SR.Error_Missing_UserSecretsLocation, userSecretsFallbackDir));
                }

                return string.Empty;
            }

            return !string.IsNullOrEmpty(appData)
                ? Path.Combine(root, "Microsoft", "UserSecrets", userSecretsId, SecretsFileName)
                : Path.Combine(root, ".microsoft", "usersecrets", userSecretsId, SecretsFileName);
        }
    }
}
