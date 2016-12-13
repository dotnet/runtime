// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.DirectoryServices.AccountManagement
{
    public enum GroupScope
    {
        Local = 0,
        Global = 1,
        Universal = 2
    }

    // The AD "groupType" bit flags corresponding to the above
    internal static class ADGroupScope
    {
        internal const int Local = 0x00000004;
        internal const int Global = 0x00000002;
        internal const int Universal = 0x00000008;
    }
}

