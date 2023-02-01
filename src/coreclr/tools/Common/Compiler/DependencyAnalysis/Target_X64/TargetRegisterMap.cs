// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.X64
{
    /// <summary>
    /// Maps logical registers to physical registers on a specified OS.
    /// </summary>
    public struct TargetRegisterMap
    {
        public readonly Register Arg0;
        public readonly Register Arg1;
        public readonly Register Arg2;
        public readonly Register Arg3;
        public readonly Register Result;

        public TargetRegisterMap(TargetOS os)
        {
            Arg0 = os == TargetOS.Windows ? Register.RCX : Register.RDI;
            Arg1 = os == TargetOS.Windows ? Register.RDX : Register.RSI;
            Arg2 = os == TargetOS.Windows ? Register.R8  : Register.RDX;
            Arg3 = os == TargetOS.Windows ? Register.R9  : Register.RCX;
            Result = Register.RAX;
        }
    }
}
