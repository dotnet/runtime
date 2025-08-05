// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics;

[Flags]
internal enum DsesActivityEvents
{
    None = 0x00,
    ActivityStart = 0x01,
    ActivityStop = 0x02,
    All = ActivityStart | ActivityStop,
}
