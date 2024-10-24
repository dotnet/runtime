// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        [Flags]
        internal enum EVT_SUBSCRIBE_FLAGS
        {
            EvtSubscribeToFutureEvents = 1,
            EvtSubscribeStartAtOldestRecord = 2,
            EvtSubscribeStartAfterBookmark = 3,
            EvtSubscribeTolerateQueryErrors = 0x1000,
            EvtSubscribeStrict = 0x10000
        }
    }
}
