// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.ARM64;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This node emits a thunk calling DelayLoad_Helper with a given instance signature
    /// to populate its indirection cell.
    /// </summary>
    public partial class ImportThunk
    {
        protected override void EmitCode(NodeFactory factory, ref ARM64Emitter instructionEncoder, bool relocsOnly)
        {

            switch (_thunkKind)
            {
                case Kind.Eager:
                    break;

                case Kind.DelayLoadHelper:
                case Kind.DelayLoadHelperWithExistingIndirectionCell:
                case Kind.VirtualStubDispatch:
                    // x11 contains indirection cell
                    // Do nothing x11 contains our first param

                    if (!relocsOnly)
                    {
                        // movz x9, #index
                        int index = _containingImportSection.IndexFromBeginningOfArray;
                        instructionEncoder.EmitMOV(Register.X9, checked((ushort)index));
                    }

                    // Move Module* -> x10
                    // ldr x10, [PC+0x1c]
                    instructionEncoder.EmitLDR(Register.X10, 0x1c);

                    // ldr x10, [x10]
                    instructionEncoder.EmitLDR(Register.X10, Register.X10);
                    break;

                case Kind.Lazy:
                    // Move Module* -> x1
                    // ldr x1, [PC+0x1c]
                    instructionEncoder.EmitLDR(Register.X1, 0x1c);

                    // ldr x1, [x1]
                    instructionEncoder.EmitLDR(Register.X1, Register.X1);
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
