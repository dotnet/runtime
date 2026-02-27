// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

[Flags]
internal enum GcScanFlags
{
    GC_CALL_INTERIOR = 0x1,
    GC_CALL_PINNED = 0x2,
}
