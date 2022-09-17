// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.LoongArch64;

namespace ILCompiler.DependencyAnalysis
{
    public partial class UnboxingStubNode
    {
        protected override void EmitCode(NodeFactory factory, ref LoongArch64Emitter encoder, bool relocsOnly)
        {
            // addi.d a0, a0, sizeof(void*);
            encoder.EmitADD(encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg0, factory.Target.PointerSize);
            encoder.EmitJMP(GetUnderlyingMethodEntrypoint(factory)); // b methodEntryPoint
        }
    }
}
