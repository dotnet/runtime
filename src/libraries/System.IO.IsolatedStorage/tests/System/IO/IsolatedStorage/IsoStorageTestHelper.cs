// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.IO.IsolatedStorage
{
    public static class IsoStorageTestHelper
    {
        private static PropertyInfo s_rootDirectoryProperty;

        static IsoStorageTestHelper()
        {
            s_rootDirectoryProperty = typeof(IsolatedStorageFile).GetProperty("RootDirectory", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static string GetRootDirectory(this IsolatedStorageFile isf)
        {
            // CoreFX and NetFX use the same internal property
            return (string)s_rootDirectoryProperty.GetValue(isf);
        }
    }
}
