// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.LoongArch64
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
        public readonly Register Arg4;
        public readonly Register Arg5;
        public readonly Register Arg6;
        public readonly Register Arg7;
        public readonly Register IntraProcedureCallScratch1;
        public readonly Register Result;

        public TargetRegisterMap(TargetOS os)
        {
            Arg0 = Register.R4;
            Arg1 = Register.R5;
            Arg2 = Register.R6;
            Arg3 = Register.R7;
            Arg4 = Register.R8;
            Arg5 = Register.R9;
            Arg6 = Register.R11;
            Arg7 = Register.R12;
            IntraProcedureCallScratch1 = Register.R21;
            Result = Register.R4;
        }
    }
}
