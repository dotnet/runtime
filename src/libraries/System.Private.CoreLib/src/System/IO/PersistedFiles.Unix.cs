// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO
{
    internal static partial class PersistedFiles
    {
        private static string? s_userProductDirectory;

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

            return Path.Combine(s_userProductDirectory!, featureName);
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

            return Path.Combine(s_userProductDirectory!, featureName, subFeatureName);
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

            return Path.Combine(s_userProductDirectory!, Path.Combine(featurePathParts));
        }

        private static void EnsureUserDirectories()
        {
            string? userHomeDirectory = GetHomeDirectory();

            if (string.IsNullOrEmpty(userHomeDirectory))
            {
                throw new InvalidOperationException(SR.PersistedFiles_NoHomeDirectory);
            }

            s_userProductDirectory = Path.Combine(
                userHomeDirectory,
                TopLevelHiddenDirectory,
                SecondLevelDirectory);
        }

        /// <summary>Gets the current user's home directory.</summary>
        /// <returns>The path to the home directory, or null if it could not be determined.</returns>
        internal static string? GetHomeDirectory() => Interop.Sys.GetHomeDirectory();
    }
}
