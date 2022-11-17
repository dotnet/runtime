// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.X86
{
    /// <summary>
    /// Maps logical registers to physical registers on a specified OS.
    /// </summary>
    public struct TargetRegisterMap
    {
        public readonly Register Arg0;
        public readonly Register Arg1;
        public readonly Register Result;

        public TargetRegisterMap(TargetOS os)
        {
            switch (os)
            {
                case TargetOS.Windows:
                case TargetOS.Linux:
                    Arg0 = Register.ECX;
                    Arg1 = Register.EDX;
                    Result = Register.EAX;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
