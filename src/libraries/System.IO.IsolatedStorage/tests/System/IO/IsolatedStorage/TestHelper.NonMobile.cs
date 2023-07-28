// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Collections.Generic;

namespace System.IO.IsolatedStorage
{
    public static partial class TestHelper
    {
        private static List<string> GetRoots()
        {
            string hash;
            object identity;
            Helper.GetDefaultIdentityAndHash(out identity, out hash, '.');
            List<string> roots = new List<string>();
            string userRoot = Helper.GetDataDirectory(IsolatedStorageScope.User);
            string randomUserRoot = Helper.GetRandomDirectory(userRoot, IsolatedStorageScope.User);
            
            roots.Add(Path.Combine(randomUserRoot, hash));
            // Application scope doesn't go under a random dir
            roots.Add(Path.Combine(userRoot, hash));

            // https://github.com/dotnet/runtime/issues/2092
            // https://github.com/dotnet/runtime/issues/21742
            if (OperatingSystem.IsWindows()
                && !PlatformDetection.IsInAppContainer)
            {
                roots.Add(Helper.GetDataDirectory(IsolatedStorageScope.Machine));
            }

            return roots;
        }

        /// <summary>
        /// The actual root of the store (housekeeping files are kept here in NetFX)
        /// </summary>
        public static string GetIdentityRootDirectory(this IsolatedStorageFile isf)
        {
            return Path.GetDirectoryName(isf.GetUserRootDirectory().TrimEnd(Path.DirectorySeparatorChar));           
        }
    }
}
    