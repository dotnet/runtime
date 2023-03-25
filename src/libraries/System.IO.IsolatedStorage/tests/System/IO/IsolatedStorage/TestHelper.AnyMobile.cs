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
            List<string> roots = new List<string>();
            string userRoot = Helper.GetDataDirectory(IsolatedStorageScope.User);
            string randomUserRoot = Helper.GetRandomDirectory(userRoot, IsolatedStorageScope.User);
            roots.Add(randomUserRoot);

            // Application scope doesn't go under a random dir
            roots.Add(userRoot);
            return roots;
        }
    }
}
