// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ARM64
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
            Arg0 = Register.X0;
            Arg1 = Register.X1;
            Arg2 = Register.X2;
            Arg3 = Register.X3;
            Arg4 = Register.X4;
            Arg5 = Register.X5;
            Arg6 = Register.X6;
            Arg7 = Register.X7;
            IntraProcedureCallScratch1 = Register.X16;
            Result = Register.X0;
        }
    }
}
