// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.RiscV64;

namespace ILCompiler.DependencyAnalysis
{
    public partial class TentativeMethodNode
    {
        protected override void EmitCode(NodeFactory factory, ref RiscV64Emitter encoder, bool relocsOnly)
        {
            encoder.EmitJMP(GetTarget(factory));
        }
    }
}
