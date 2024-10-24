// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        [Flags]
        internal enum EVT_SEEK_FLAGS
        {
            EvtSeekRelativeToFirst = 1,
            EvtSeekRelativeToLast = 2,
            EvtSeekRelativeToCurrent = 3,
            EvtSeekRelativeToBookmark = 4,
            EvtSeekOriginMask = 7,
            EvtSeekStrict = 0x10000
        }
    }
}
