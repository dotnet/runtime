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
                case Kind.DelayLoadHelperWithExistingIndirectionCell:
                case Kind.VirtualStubDispatch:

                    // x11 contains indirection cell
                    // Do nothing x11 contains our first param

                    // movz x9, #index
                    int index = _containingImportSection.IndexFromBeginningOfArray;
                    instructionEncoder.EmitMOV(Register.X9, checked((ushort)index));

                    // Move Module* -> x10
                    // adrp x10, ModuleImport
                    instructionEncoder.EmitADRP(Register.X10, factory.ModuleImport);

                    // ldr x10, [x10, ModuleImport page offset]
                    instructionEncoder.EmitLDR(Register.X10, Register.X10, factory.ModuleImport);
                    break;

                case Kind.Lazy:

                    // Move Module* -> x1
                    // adrp x1, ModuleImport
                    instructionEncoder.EmitADRP(Register.X1, factory.ModuleImport);

                    // ldr x1, [x1, ModuleImport page offset]
                    instructionEncoder.EmitLDR(Register.X1, Register.X1, factory.ModuleImport);
                    break;

                default:
                    throw new NotImplementedException();
            }

            // branch to helper
            instructionEncoder.EmitJMP(_helperCell);
        }
    }
}
