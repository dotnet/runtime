// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
#pragma warning disable CA1052 // make class static
    partial class Activity
#pragma warning restore CA1052
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
