// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Collections.Generic;

namespace System.IO.IsolatedStorage
{
    public static partial class TestHelper
    {
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
            s_roots.Add(randomUserRoot);

            // Application scope doesn't go under a random dir
            s_roots.Add(Path.Combine(userRoot, hash));
            s_roots.Add(userRoot);

            // https://github.com/dotnet/runtime/issues/2092
            // https://github.com/dotnet/runtime/issues/21742
            if (OperatingSystem.IsWindows()
                && !PlatformDetection.IsInAppContainer)
            {
                s_roots.Add(Helper.GetDataDirectory(IsolatedStorageScope.Machine));
            }

            // We don't expose Roaming yet
            // Helper.GetDataDirectory(IsolatedStorageScope.Roaming);
        }
    }
}
    