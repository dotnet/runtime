// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

/// <summary>
/// Mirrors <c>Internal.JitInterface.WasmLowering.LoweringFlags</c>, which this task cannot reference
/// because the type system it lives in does not load on .NET Framework MSBuild. The values are passed
/// through to the signature resolver unchanged, so they must stay in sync.
/// </summary>
[Flags]
internal enum WasmLoweringFlags
{
    /// <summary>
    /// A managed call. The signature gains a 'T' for an instance method and a trailing 'p' for the
    /// portable entry point parameter.
    /// </summary>
    None = 0x0,

    HasGenericContextArg = 0x1,

    IsAsyncCall = 0x2,

    /// <summary>
    /// A native signature: the lowered parameters and return value with no managed calling convention
    /// additions. Used for P/Invoke targets and reverse P/Invoke entry points.
    /// </summary>
    IsUnmanagedCallersOnly = 0x4,
}
