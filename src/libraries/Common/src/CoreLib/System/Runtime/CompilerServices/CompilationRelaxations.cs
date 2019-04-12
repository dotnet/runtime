// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Runtime.CompilerServices
{
    /// IMPORTANT: Keep this in sync with corhdr.h
    [Flags]
    public enum CompilationRelaxations : int
    {
        NoStringInterning = 0x0008  // Start in 0x0008, we had other non public flags in this enum before,
                                    // so we'll start here just in case somebody used them. This flag is only
                                    // valid when set for Assemblies.
    }
}
