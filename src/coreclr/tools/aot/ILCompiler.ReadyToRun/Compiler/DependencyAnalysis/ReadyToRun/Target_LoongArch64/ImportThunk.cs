// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
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
            if (_thunkKind == Kind.Eager)
            {
                // branch to helper
                instructionEncoder.EmitJMP(_helperCell);
                return;
            }

            instructionEncoder.Builder.RequireInitialPointerAlignment();
            Debug.Assert(instructionEncoder.Builder.CountBytes == 0);

            instructionEncoder.Builder.EmitReloc(factory.ModuleImport, RelocType.IMAGE_REL_BASED_DIR64);

            Debug.Assert(instructionEncoder.Builder.CountBytes == ((ISymbolNode)this).Offset);

            if (relocsOnly)
            {
                // When doing relocs only, we don't need to generate the actual instructions
                // as they will be ignored. Just emit the jump so we record the dependency.
                instructionEncoder.EmitJMP(_helperCell);
                return;
            }

            switch (_thunkKind)
            {
                case Kind.DelayLoadHelper:
                case Kind.VirtualStubDispatch:
                case Kind.DelayLoadHelperWithExistingIndirectionCell:
                {
                    // T8 contains indirection cell
                    // Do nothing T8=R20 contains our first param

                    // ori T0=R12, R0, #index
                    int index = _containingImportSection.IndexFromBeginningOfArray;
                    instructionEncoder.EmitMOV(Register.R12, checked((ushort)index));

                    int offset = -instructionEncoder.Builder.CountBytes;

                    // get pc
                    // pcaddi T1=R13, 0
                    instructionEncoder.EmitPCADDI(Register.R13);

                    // load Module* -> T1
                    instructionEncoder.EmitLD(Register.R13, Register.R13, offset);

                    // ld_d R13, R13, 0
                    instructionEncoder.EmitLD(Register.R13, Register.R13, 0);
                    break;
                }

                case Kind.Lazy:
                {
                    int offset = -instructionEncoder.Builder.CountBytes;
                    // get pc
                    // pcaddi R5, 0
                    instructionEncoder.EmitPCADDI(Register.R5);

                    // load Module* -> R5=A1
                    instructionEncoder.EmitLD(Register.R5, Register.R5, offset);

                    // ld_d R5, R5, 0
                    instructionEncoder.EmitLD(Register.R5, Register.R5, 0);
                    break;
                }

                default:
                    throw new NotImplementedException();
            }

            // branch to helper
            instructionEncoder.EmitJMP(_helperCell);
        }
    }
}
