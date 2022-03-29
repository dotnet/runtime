// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.ARM;

namespace ILCompiler.DependencyAnalysis
{
    public partial class JumpStubNode
    {
        protected override void EmitCode(NodeFactory factory, ref ARMEmitter encoder, bool relocsOnly)
        {
            if (!_target.RepresentsIndirectionCell)
            {
                encoder.EmitJMP(_target); // b methodEntryPoint
            }
            else
            {
                encoder.EmitMOV(encoder.TargetRegister.InterproceduralScratch, _target);
                encoder.EmitJMP(encoder.TargetRegister.InterproceduralScratch);
            }
        }
    }
}
