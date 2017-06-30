// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal static bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode)
        {
            // Prompting behavior no longer occurs in all platforms supported
            lpOldMode = 0;
            return true;
        }
    }
}
