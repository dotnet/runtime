// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    public abstract class AssemblyStubNode : ObjectNode, ISymbolDefinitionNode
    {
        public AssemblyStubNode()
        {
        }

        /// <summary>
        /// Gets a value indicating whether the stub's address is visible from managed code
        /// and could be a target of a managed calli.
        /// </summary>
        protected virtual bool IsVisibleFromManagedCode => true;

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.TextSection;

        public override bool StaticDependenciesAreComputed => true;

        public abstract void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);
        public int Offset => 0;
        public override bool IsShareable => false;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            // If the address is expected to be visible from managed code, we need to align
            // at the managed code boundaries to prevent the stub from being confused with
            // a fat fuction pointer. Otherwise we can align tighter.
            int alignment = IsVisibleFromManagedCode ?
                factory.Target.MinimumFunctionAlignment :
                factory.Target.MinimumCodeAlignment;

            switch (factory.Target.Architecture)
            {
                case TargetArchitecture.X64:
                    X64.X64Emitter x64Emitter = new X64.X64Emitter(factory, relocsOnly);
                    EmitCode(factory, ref x64Emitter, relocsOnly);
                    x64Emitter.Builder.RequireInitialAlignment(alignment);
                    x64Emitter.Builder.AddSymbol(this);
                    return x64Emitter.Builder.ToObjectData();

                case TargetArchitecture.X86:
                    X86.X86Emitter x86Emitter = new X86.X86Emitter(factory, relocsOnly);
                    EmitCode(factory, ref x86Emitter, relocsOnly);
                    x86Emitter.Builder.RequireInitialAlignment(alignment);
                    x86Emitter.Builder.AddSymbol(this);
                    return x86Emitter.Builder.ToObjectData();

                case TargetArchitecture.ARM:
                    ARM.ARMEmitter armEmitter = new ARM.ARMEmitter(factory, relocsOnly);
                    EmitCode(factory, ref armEmitter, relocsOnly);
                    armEmitter.Builder.RequireInitialAlignment(alignment);
                    armEmitter.Builder.AddSymbol(this);
                    return armEmitter.Builder.ToObjectData();

                case TargetArchitecture.ARM64:
                    ARM64.ARM64Emitter arm64Emitter = new ARM64.ARM64Emitter(factory, relocsOnly);
                    EmitCode(factory, ref arm64Emitter, relocsOnly);
                    arm64Emitter.Builder.RequireInitialAlignment(alignment);
                    arm64Emitter.Builder.AddSymbol(this);
                    return arm64Emitter.Builder.ToObjectData();

                case TargetArchitecture.LoongArch64:
                    LoongArch64.LoongArch64Emitter loongarch64Emitter = new LoongArch64.LoongArch64Emitter(factory, relocsOnly);
                    EmitCode(factory, ref loongarch64Emitter, relocsOnly);
                    loongarch64Emitter.Builder.RequireInitialAlignment(alignment);
                    loongarch64Emitter.Builder.AddSymbol(this);
                    return loongarch64Emitter.Builder.ToObjectData();

                case TargetArchitecture.RiscV64:
                    RiscV64.RiscV64Emitter riscv64Emitter = new RiscV64.RiscV64Emitter(factory, relocsOnly);
                    EmitCode(factory, ref riscv64Emitter, relocsOnly);
                    riscv64Emitter.Builder.RequireInitialAlignment(alignment);
                    riscv64Emitter.Builder.AddSymbol(this);
                    return riscv64Emitter.Builder.ToObjectData();

                default:
                    throw new NotImplementedException();
            }
        }

        protected abstract void EmitCode(NodeFactory factory, ref X64.X64Emitter instructionEncoder, bool relocsOnly);
        protected abstract void EmitCode(NodeFactory factory, ref X86.X86Emitter instructionEncoder, bool relocsOnly);
        protected abstract void EmitCode(NodeFactory factory, ref ARM.ARMEmitter instructionEncoder, bool relocsOnly);
        protected abstract void EmitCode(NodeFactory factory, ref ARM64.ARM64Emitter instructionEncoder, bool relocsOnly);
        protected abstract void EmitCode(NodeFactory factory, ref LoongArch64.LoongArch64Emitter instructionEncoder, bool relocsOnly);
        protected abstract void EmitCode(NodeFactory factory, ref RiscV64.RiscV64Emitter instructionEncoder, bool relocsOnly);
    }
}
