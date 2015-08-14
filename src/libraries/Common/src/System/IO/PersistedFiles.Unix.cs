// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.IO
{
    internal static class PersistedFiles
    {
        // If we ever need system persisted data, /etc/dotnet/corefx/
        private const string TopLevelDirectory = "dotnet";
        // User persisted data, ~/.dotnet/corefx/
        private const string TopLevelUserDirectory = "." + TopLevelDirectory;
        private const string SecondLevelDirectory = "corefx";

        private static string s_userProductDirectory;

        /// <summary>
        /// Get the location of where to persist information for a particular aspect of the framework,
        /// such as "cryptography".
        /// </summary>
        /// <param name="featureName">The directory name for the feature</param>
        /// <returns>A path within the user's home directory for persisting data for the feature</returns>
        internal static string GetUserFeatureDirectory(string featureName)
        {
            if (s_userProductDirectory == null)
            {
                EnsureUserDirectories();
            }

            return Path.Combine(s_userProductDirectory, featureName);
        }

        /// <summary>
        /// Get the location of where to persist information for a particular aspect of a feature of
        /// the framework, such as "x509stores" within "cryptography".
        /// </summary>
        /// <param name="featureName">The directory name for the feature</param>
        /// <param name="subFeatureName">The directory name for the sub-feature</param>
        /// <returns>A path within the user's home directory for persisting data for the sub-feature</returns>
        internal static string GetUserFeatureDirectory(string featureName, string subFeatureName)
        {
            if (s_userProductDirectory == null)
            {
                EnsureUserDirectories();
            }

            return Path.Combine(s_userProductDirectory, featureName, subFeatureName);
        }

        /// <summary>
        /// Get the location of where to persist information for a particular aspect of the framework,
        /// with a lot of hierarchy, such as ["cryptography", "x509stores", "my"]
        /// </summary>
        /// <param name="featurePathParts">A non-empty set of directories to use for the storage hierarchy</param>
        /// <returns>A path within the user's home directory for persisting data for the feature</returns>
        internal static string GetUserFeatureDirectory(params string[] featurePathParts)
        {
            Debug.Assert(featurePathParts != null);
            Debug.Assert(featurePathParts.Length > 0);

            if (s_userProductDirectory == null)
            {
                EnsureUserDirectories();
            }

            return Path.Combine(s_userProductDirectory, Path.Combine(featurePathParts));
        }

        private static void EnsureUserDirectories()
        {
            // TODO (2820): Use getpwuid_r(geteuid(), ...) instead of the HOME environment variable
            string userHomeDirectory = Environment.GetEnvironmentVariable("HOME");

            if (string.IsNullOrEmpty(userHomeDirectory))
            {
                throw new InvalidOperationException(SR.PersistedFiles_NoHomeDirectory);
            }

            s_userProductDirectory = Path.Combine(
                userHomeDirectory,
                TopLevelUserDirectory,
                SecondLevelDirectory);
        }
    }
}
