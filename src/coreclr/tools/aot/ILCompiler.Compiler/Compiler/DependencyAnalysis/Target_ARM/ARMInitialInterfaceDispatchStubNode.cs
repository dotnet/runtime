// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using System;

using ILCompiler.DependencyAnalysis.ARM;
using ILCompiler.DependencyAnalysis.X64;
using ILCompiler.DependencyAnalysis.X86;
using ILCompiler.DependencyAnalysis.ARM64;
using ILCompiler.DependencyAnalysis.LoongArch64;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// On ARM, we use R12 to store the interface dispatch cell. However, the jump through the import address
    /// table to call the runtime interface dispatch helper trashes R12. This stub pushes R12 before making
    /// the runtime call. The ARM runtime interface dispatch code expects this and pops R12 to get the dispatch
    /// cell.
    /// </summary>
    public partial class InitialInterfaceDispatchStubNode : AssemblyStubNode
    {      
        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("_InitialInterfaceDispatchStub");
        }

        public override bool IsShareable => false;

        protected override void EmitCode(NodeFactory factory, ref ARMEmitter instructionEncoder, bool relocsOnly)
        {
            instructionEncoder.EmitPUSH(ARM.Register.R12);
            instructionEncoder.EmitMOV(ARM.Register.R12, factory.ExternSymbol("RhpInitialInterfaceDispatch"));
            instructionEncoder.EmitMOV(ARM.Register.R15, ARM.Register.R12);
        }

        // Only ARM requires a stub
        protected override void EmitCode(NodeFactory factory, ref X86Emitter instructionEncoder, bool relocsOnly)
        {
            throw new NotImplementedException();
        }

        protected override void EmitCode(NodeFactory factory, ref X64Emitter instructionEncoder, bool relocsOnly)
        {
            throw new NotImplementedException();
        }

        protected override void EmitCode(NodeFactory factory, ref ARM64Emitter instructionEncoder, bool relocsOnly)
        {
            throw new NotImplementedException();
        }

        protected override void EmitCode(NodeFactory factory, ref LoongArch64Emitter instructionEncoder, bool relocsOnly)
        {
            throw new NotImplementedException();
        }

        public override int ClassCode => 588185132;
    }
}
