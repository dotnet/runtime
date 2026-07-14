// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Text;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis.ARM;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This node emits a thunk calling DelayLoad_Helper with a given instance signature
    /// to populate its indirection cell.
    /// </summary>
    public partial class ImportThunk
    {
        protected override void EmitCode(NodeFactory factory, ref ARM.ARMEmitter instructionEncoder, bool relocsOnly)
        {
            switch (_thunkKind)
            {
                case ImportThunkKind.Eager:
                    // mov r12, [helper]
                    instructionEncoder.EmitMOV(Register.R12, _helperCell);
                    // ldr r12, [r12]
                    instructionEncoder.EmitLDR(Register.R12, Register.R12);
                    // bx r12
                    instructionEncoder.EmitJMP(Register.R12);
                    break;

                case ImportThunkKind.DelayLoadHelper:
                case ImportThunkKind.VirtualStubDispatch:
                case ImportThunkKind.DelayLoadHelperWithExistingIndirectionCell:
                    // r12 contains indirection cell
                    // push r12
                    instructionEncoder.EmitPUSH(Register.R12);

                    if (!relocsOnly)
                    {
                        int index = _containingImportSection.IndexFromBeginningOfArray;
                        // movw r12, #index
                        instructionEncoder.EmitMOV(Register.R12, checked((ushort)index));
                        // push r12
                        instructionEncoder.EmitPUSH(Register.R12);
                    }

                    // mov r12, [module]
                    instructionEncoder.EmitMOV(Register.R12, factory.ModuleImport);
                    // ldr r12, [r12]
                    instructionEncoder.EmitLDR(Register.R12, Register.R12);
                    // push r12
                    instructionEncoder.EmitPUSH(Register.R12);

                    // mov r12, [helper]
                    instructionEncoder.EmitMOV(Register.R12, _helperCell);
                    // ldr r12, [r12]
                    instructionEncoder.EmitLDR(Register.R12, Register.R12);
                    // bx r12
                    instructionEncoder.EmitJMP(Register.R12);
                    break;

                case ImportThunkKind.Lazy:
                    // mov r1, [module]
                    instructionEncoder.EmitMOV(Register.R1, factory.ModuleImport);
                    // ldr r1, [r1]
                    instructionEncoder.EmitLDR(Register.R1, Register.R1);
                    // mov r12, [helper]
                    instructionEncoder.EmitMOV(Register.R12, _helperCell);
                    // ldr r12, [r12]
                    instructionEncoder.EmitLDR(Register.R12, Register.R12);
                    // bx r12
                    instructionEncoder.EmitJMP(Register.R12);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
