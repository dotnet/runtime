// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.X86;

namespace ILCompiler.DependencyAnalysis
{
    public partial class ReadyToRunGenericHelperNode
    {
        protected sealed override void EmitCode(NodeFactory factory, ref X86Emitter encoder, bool relocsOnly)
        {
            encoder.EmitINT3();
        }
    }
}
