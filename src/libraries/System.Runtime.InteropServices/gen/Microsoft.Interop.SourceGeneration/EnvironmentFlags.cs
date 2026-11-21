// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Interop;

[Flags]
public enum EnvironmentFlags
{
    None = 0,
    SkipLocalsInit = 0x1,
    DisableRuntimeMarshalling = 0x2,
    /// <summary>
    /// The compilation uses the updated memory safety rules ("unsafe evolution"), under which an
    /// <c>unsafe</c> modifier on a type has no effect and pointer types need no unsafe context to be named.
    /// </summary>
    UpdatedMemorySafetyRules = 0x4,
}
