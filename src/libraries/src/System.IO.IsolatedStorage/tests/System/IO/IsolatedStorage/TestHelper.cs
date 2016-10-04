// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.IO.IsolatedStorage
{
    public static class TestHelper
    {
        private static PropertyInfo s_rootDirectoryProperty;
        private static List<string> s_roots;

        static TestHelper()
        {
            s_rootDirectoryProperty = typeof(IsolatedStorageFile).GetProperty("RootDirectory", BindingFlags.NonPublic | BindingFlags.Instance);

            s_roots = new List<string>();

            string hash;
            object identity;
            Helper.GetDefaultIdentityAndHash(out identity, out hash, '.');

            string userRoot = Helper.GetDataDirectory(IsolatedStorageScope.User);
            string randomUserRoot = Helper.GetRandomDirectory(userRoot, IsolatedStorageScope.User);
            s_roots.Add(Path.Combine(randomUserRoot, hash));

            // Application scope doesn't go under a random dir
            s_roots.Add(Path.Combine(userRoot, hash));

            // https://github.com/dotnet/corefx/issues/11124
            // Need to implement ACLing for machine scope first
            // Helper.GetDataDirectory(IsolatedStorageScope.Machine);

            // We don't expose Roaming yet
            // Helper.GetDataDirectory(IsolatedStorageScope.Roaming);
        }

        /// <summary>
        /// Where the user's files go
        /// </summary>
        public static string GetUserRootDirectory(this IsolatedStorageFile isf)
        {
            // CoreFX and NetFX use the same internal property
            return (string)s_rootDirectoryProperty.GetValue(isf);
        }

        /// <summary>
        /// The actual root of the store (housekeeping files are kept here in NetFX)
        /// </summary>
        public static string GetIdentityRootDirectory(this IsolatedStorageFile isf)
        {
            return Path.GetDirectoryName(isf.GetUserRootDirectory().TrimEnd(Path.DirectorySeparatorChar));
        }

        public static void WipeStores()
        {
            foreach (string root in s_roots)
            {
                try
                {
                    if (Directory.Exists(root))
                    {
                        Directory.Delete(root, recursive: true);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to wipe stores: {e.Message}");
                }
            }
        }
    }
}
