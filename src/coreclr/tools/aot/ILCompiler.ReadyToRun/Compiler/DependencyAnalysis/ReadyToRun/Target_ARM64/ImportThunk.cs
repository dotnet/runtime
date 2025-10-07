// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis.ARM64;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This node emits a thunk calling DelayLoad_Helper with a given instance signature
    /// to populate its indirection cell.
    /// </summary>
    public partial class ImportThunk : ISymbolNode
    {
        protected override void EmitCode(NodeFactory factory, ref ARM64Emitter instructionEncoder, bool relocsOnly)
        {
            if (_thunkKind == Kind.Eager)
            {
                instructionEncoder.EmitJMP(_helperCell);
                return;
            }

            instructionEncoder.Builder.RequireInitialPointerAlignment();
            Debug.Assert(instructionEncoder.Builder.CountBytes == 0);

            switch (_thunkKind)
            {
                case Kind.DelayLoadHelper:
                case Kind.DelayLoadHelperWithExistingIndirectionCell:
                case Kind.VirtualStubDispatch:

                    instructionEncoder.Builder.EmitReloc(factory.ModuleImport, RelocType.IMAGE_REL_BASED_DIR64);

                    Debug.Assert(instructionEncoder.Builder.CountBytes == ((ISymbolNode)this).Offset);

                    // x11 contains indirection cell
                    // Do nothing x11 contains our first param

                    if (!relocsOnly)
                    {
                        // movz x9, #index
                        int index = _containingImportSection.IndexFromBeginningOfArray;
                        instructionEncoder.EmitMOV(Register.X9, checked((ushort)index));
                    }

                    // Move Module* -> x10
                    // ldr x10, [PC-0xc]
                    instructionEncoder.EmitLDR(Register.X10, -0xc);

                    // ldr x10, [x10]
                    instructionEncoder.EmitLDR(Register.X10, Register.X10);
                    break;

                case Kind.Lazy:
                    instructionEncoder.Builder.EmitReloc(factory.ModuleImport, RelocType.IMAGE_REL_BASED_DIR64);

                    Debug.Assert(instructionEncoder.Builder.CountBytes == ((ISymbolNode)this).Offset);

                    // Move Module* -> x1
                    // ldr x1, [PC-0x8]
                    instructionEncoder.EmitLDR(Register.X1, -0x8);

                    // ldr x1, [x1]
                    instructionEncoder.EmitLDR(Register.X1, Register.X1);
                    break;

                default:
                    throw new NotImplementedException();
            }

            // branch to helper
            instructionEncoder.EmitJMP(_helperCell);
        }
    }
}
