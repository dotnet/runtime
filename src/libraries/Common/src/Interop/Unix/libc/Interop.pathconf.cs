// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        /// <summary>
        /// Gets a pathconf value by name.  If the cached value is less than zero (meaning not yet initialized),
        /// pathconf is used to retrieve the value, which is then stored into the field.
        /// If the field is greater than or equal to zero, it's value is returned.
        /// </summary>
        /// <param name="cachedValue">The field used to cache the pathconf value.</param>
        /// <param name="pathConfName">The name of the pathconf value.</param>
        /// <param name="defaultValue">The default value to use in case pathconf fails.</param>
        internal static void GetPathConfValue(ref int cachedValue, int pathConfName, int defaultValue)
        {
            if (cachedValue < 0) // benign race condition on cached value
            {
                int result = Interop.libc.pathconf("/", pathConfName);
                cachedValue = result >= 0 ? result : defaultValue;
            }
        }

        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern int pathconf(string path, int name);
    }
}
