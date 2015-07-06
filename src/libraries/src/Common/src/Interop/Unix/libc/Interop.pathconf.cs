// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        /// <summary>The maximum path length for the system.  -1 if it hasn't yet been initialized.</summary>
        private static int s_maxPath = -1;
        /// <summary>The maximum name length for the system.  -1 if it hasn't yet been initialized.</summary>
        private static int s_maxName = -1;

        internal static int MaxPath
        {
            get { return Interop.libc.GetPathConfValue(ref s_maxPath, Interop.libc.PathConfNames._PC_PATH_MAX, Interop.libc.DEFAULT_PC_PATH_MAX); }
        }

        internal static int MaxName
        {
            get { return Interop.libc.GetPathConfValue(ref s_maxName, Interop.libc.PathConfNames._PC_NAME_MAX, Interop.libc.DEFAULT_PC_NAME_MAX); }
        }

        /// <summary>
        /// Gets a pathconf value by name.  If the cached value is less than zero (meaning not yet initialized),
        /// pathconf is used to retrieve the value, which is then stored into the field.
        /// If the field is greater than or equal to zero, it's value is returned.
        /// </summary>
        /// <param name="cachedValue">The field used to cache the pathconf value.</param>
        /// <param name="pathConfName">The name of the pathconf value.</param>
        /// <param name="defaultValue">The default value to use in case pathconf fails.</param>
        private static int GetPathConfValue(ref int cachedValue, int pathConfName, int defaultValue)
        {
            if (cachedValue < 0) // benign race condition on cached value
            {
                int result = Interop.libc.pathconf("/", pathConfName);
                cachedValue = result >= 0 ? result : defaultValue;
            }
            return cachedValue;
        }

        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern int pathconf(string path, int name);
    }
}
