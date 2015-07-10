// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

internal partial class Interop
{
    internal partial class mincore
    {
        internal static uint SetErrorMode(uint uMode)
        {
            // Prompting behavior no longer occurs in all platforms supported
            return 0;
        }
    }
}
