// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.X86;

namespace ILCompiler.DependencyAnalysis
{
    public partial class JumpStubNode
    {
        protected override void EmitCode(NodeFactory factory, ref X86Emitter encoder, bool relocsOnly)
        {
            if (!_target.RepresentsIndirectionCell)
            {
                encoder.EmitJMP(_target);
            }
            else
            {
                encoder.EmitMOV(encoder.TargetRegister.Result, _target);
                encoder.EmitJMP(_target);
            }
        }
    }
}
