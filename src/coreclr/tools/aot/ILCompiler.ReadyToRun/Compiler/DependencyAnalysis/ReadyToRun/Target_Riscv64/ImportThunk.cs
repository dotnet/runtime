// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.Riscv64;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This node emits a thunk calling DelayLoad_Helper with a given instance signature
    /// to populate its indirection cell.
    /// </summary>
    public partial class ImportThunk
    {
        protected override void EmitCode(NodeFactory factory, ref Riscv64Emitter instructionEncoder, bool relocsOnly)
        {

            switch (_thunkKind)
            {
                case Kind.Eager:
                    break;

                case Kind.DelayLoadHelper:
                case Kind.VirtualStubDispatch:
                    // T8 contains indirection cell

                    if (!relocsOnly)
                    {
                        // ori a4, r0, #index
                        int index = _containingImportSection.IndexFromBeginningOfArray;
                        instructionEncoder.EmitMOV(Register.X14, checked((ushort)index));
                    }

                    // get pc
                    // auipc T1=X6, 0
                    instructionEncoder.EmitPC(Register.X6);

                    // load Module* -> T1
                    instructionEncoder.EmitLD(Register.X6, Register.X6, 0x18);

                    // ld_d X6, X6, 0
                    instructionEncoder.EmitLD(Register.X6, Register.X6, 0);
                    break;

                case Kind.Lazy:
                    // get pc
                    instructionEncoder.EmitPC(Register.X11);

                    // load Module* -> a1
                    instructionEncoder.EmitLD(Register.X11, Register.X11, 0x18);

                    // ld a1, a1, 0
                    instructionEncoder.EmitLD(Register.X11, Register.X11, 0);
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
