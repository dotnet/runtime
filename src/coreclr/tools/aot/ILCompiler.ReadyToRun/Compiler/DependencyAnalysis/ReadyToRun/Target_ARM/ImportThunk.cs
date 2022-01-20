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
                case Kind.Eager:
                    // mov r12, [helper]
                    instructionEncoder.EmitMOV(Register.R12, _helperCell);
                    // ldr.w r12, [r12]
                    instructionEncoder.EmitLDR(Register.R12, Register.R12, 0);
                    // bx r12
                    instructionEncoder.EmitJMP(Register.R12);
                    break;

                case Kind.DelayLoadHelper:
                case Kind.VirtualStubDispatch:
                case Kind.DelayLoadHelperWithExistingIndirectionCell:
                    // r4 contains indirection cell
                    // push r4
                    instructionEncoder.EmitPUSH(Register.R4);

                    if (!relocsOnly)
                    {
                        int index = _containingImportSection.IndexFromBeginningOfArray;
                        // mov r4, #index
                        instructionEncoder.EmitMOV(Register.R4, index);
                        // push r4
                        instructionEncoder.EmitPUSH(Register.R4);
                    }

                    // mov r4, [module]
                    instructionEncoder.EmitMOV(Register.R4, factory.ModuleImport);
                    // ldr r4, [r4]
                    instructionEncoder.EmitLDR(Register.R4, Register.R4);
                    // push r4
                    instructionEncoder.EmitPUSH(Register.R4);

                    // mov r4, [helper]
                    instructionEncoder.EmitMOV(Register.R4, _helperCell);
                    // ldr r4, [r4]
                    instructionEncoder.EmitLDR(Register.R4, Register.R4);
                    // bx r4
                    instructionEncoder.EmitJMP(Register.R4);
                    break;

                case Kind.Lazy:
                    // mov r1, [module]
                    instructionEncoder.EmitMOV(Register.R1, factory.ModuleImport);
                    // ldr r1, [r1]
                    instructionEncoder.EmitLDR(Register.R1, Register.R1);
                    // mov r12, [helper]
                    instructionEncoder.EmitMOV(Register.R12, _helperCell);
                    // ldr.w r12, [r12]
                    instructionEncoder.EmitLDR(Register.R12, Register.R12, 0);
                    // bx r12
                    instructionEncoder.EmitJMP(Register.R12);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
