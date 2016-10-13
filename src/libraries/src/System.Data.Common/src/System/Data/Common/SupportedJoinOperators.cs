// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Common
{
    [Flags]
    public enum SupportedJoinOperators
    {
        None = 0x00000000,
        Inner = 0x00000001,
        LeftOuter = 0x00000002,
        RightOuter = 0x00000004,
        FullOuter = 0x00000008
    }
}


