// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.ARM;

namespace ILCompiler.DependencyAnalysis
{
    public partial class UnboxingStubNode
    {
        protected override void EmitCode(NodeFactory factory, ref ARMEmitter encoder, bool relocsOnly)
        {
            encoder.EmitADD(encoder.TargetRegister.Arg0, (byte)factory.Target.PointerSize); // add r0, sizeof(void*);
            encoder.EmitJMP(GetUnderlyingMethodEntrypoint(factory)); // b methodEntryPoint
        }
    }
}
