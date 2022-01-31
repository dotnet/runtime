// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Wasm.Build.Tests
{
    [Flags]
    public enum RunHost
    {
        None = 0,
        V8 = 1,
        Chrome = 2,
        Safari = 4,
        Firefox = 8,
        NodeJS = 16,

        All = V8 | NodeJS | Chrome//| Firefox//Safari
    }
}
