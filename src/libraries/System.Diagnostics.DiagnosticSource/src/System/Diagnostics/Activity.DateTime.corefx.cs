// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    partial class Activity
    {
        /// <summary>
        /// Returns high resolution (~1 usec) current UTC DateTime.
        /// </summary>
        internal static DateTime GetUtcNow()
        {
            // .NET Core CLR gives accurate UtcNow
            return DateTime.UtcNow;
        }
    }
}
