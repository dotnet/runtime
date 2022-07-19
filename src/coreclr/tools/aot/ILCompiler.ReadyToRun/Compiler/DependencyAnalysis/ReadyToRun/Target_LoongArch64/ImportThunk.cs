// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.LoongArch64;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This node emits a thunk calling DelayLoad_Helper with a given instance signature
    /// to populate its indirection cell.
    /// </summary>
    public partial class ImportThunk
    {
        protected override void EmitCode(NodeFactory factory, ref LoongArch64Emitter instructionEncoder, bool relocsOnly)
        {

            switch (_thunkKind)
            {
                case Kind.Eager:
                    break;

                case Kind.DelayLoadHelper:
                case Kind.VirtualStubDispatch:
                    // T8 contains indirection cell
                    // Do nothing T8=R20 contains our first param

                    if (!relocsOnly)
                    {
                        // movz T0=R12, #index
                        int index = _containingImportSection.IndexFromBeginningOfArray;
                        instructionEncoder.EmitMOV(Register.R12, checked((ushort)index));
                    }

                    // get pc
                    // pcaddi T1=R13, 0
                    instructionEncoder.EmitPC(Register.R13);

                    // load Module* -> T1
                    instructionEncoder.EmitLD(Register.R13, Register.R13, 0x24);

                    // ld_d R13, R13, 0
                    instructionEncoder.EmitLD(Register.R13, Register.R13, 0);
                    break;

                case Kind.Lazy:
                    // get pc
                    // pcaddi R5, 0
                    instructionEncoder.EmitPC(Register.R5);

                    // load Module* -> R5=A1
                    instructionEncoder.EmitLD(Register.R5, Register.R5, 0x24);

                    // ld_d R5, R5, 0
                    instructionEncoder.EmitLD(Register.R5, Register.R5, 0);
                    break;

                default:
                    throw new NotImplementedException();
            }

            // branch to helper
            instructionEncoder.EmitJMP(_helperCell);

            // Emit relocation for the Module* load above
            if (_thunkKind != Kind.Eager)
                instructionEncoder.Builder.EmitReloc(factory.ModuleImport, RelocType.IMAGE_REL_BASED_DIR64);
        }
    }
}
