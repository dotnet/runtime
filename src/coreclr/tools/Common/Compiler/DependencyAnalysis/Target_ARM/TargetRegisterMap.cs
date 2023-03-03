// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ARM
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
        public readonly Register InterproceduralScratch;
        public readonly Register SP;
        public readonly Register LR;
        public readonly Register PC;

        public TargetRegisterMap(TargetOS os)
        {
            Arg0 = Register.R0;
            Arg1 = Register.R1;
            Arg2 = Register.R2;
            Arg3 = Register.R3;
            Result = Register.R0;
            InterproceduralScratch = Register.R12;
            SP = Register.R13;
            LR = Register.R14;
            PC = Register.R15;
        }
    }
}
