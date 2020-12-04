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
            switch (os)
            {
                case TargetOS.Windows:
                    Arg0 = Register.RCX;
                    Arg1 = Register.RDX;
                    Arg2 = Register.R8;
                    Arg3 = Register.R9;
                    Result = Register.RAX;
                    break;

                case TargetOS.Linux:
                case TargetOS.OSX:
                case TargetOS.FreeBSD:
                case TargetOS.SunOS:
                case TargetOS.NetBSD:
                    Arg0 = Register.RDI;
                    Arg1 = Register.RSI;
                    Arg2 = Register.RDX;
                    Arg3 = Register.RCX;
                    Result = Register.RAX;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
