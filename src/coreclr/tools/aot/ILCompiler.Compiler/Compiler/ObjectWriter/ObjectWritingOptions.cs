// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.ObjectWriter
{
    [Flags]
    public enum ObjectWritingOptions
    {
        GenerateDebugInfo = 0x01,
        ControlFlowGuard = 0x02,
        UseDwarf5 = 0x4,
    }
}
