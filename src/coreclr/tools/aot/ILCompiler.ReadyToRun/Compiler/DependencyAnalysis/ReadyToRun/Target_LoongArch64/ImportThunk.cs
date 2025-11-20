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
            if (relocsOnly)
            {
                // When doing relocs only, we don't need to generate the actual instructions
                // as they will be ignored. Just emit the module import load and jump so we record the dependencies.
                instructionEncoder.EmitLD(Register.R5, factory.ModuleImport);
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

                    instructionEncoder.EmitLD(Register.R13, factory.ModuleImport);
                    break;
                }

                case Kind.Lazy:
                {
                    instructionEncoder.EmitLD(Register.R5, factory.ModuleImport);
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
