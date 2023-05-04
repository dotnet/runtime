// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    [Flags]
    internal enum MethodSpecifier
    {
        None = 0x0,
        // Root methods
        Bind = 0x1,
        Get = 0x2,
        Configure = 0x4,
        // Helper methods
        BindCore = 0x8,
        HasValueOrChildren = 0x10,
        HasChildren = 0x20,
    }
}
