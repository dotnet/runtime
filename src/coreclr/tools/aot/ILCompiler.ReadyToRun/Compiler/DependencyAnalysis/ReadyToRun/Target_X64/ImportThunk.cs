// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Text;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis.X64;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This node emits a thunk calling DelayLoad_Helper with a given instance signature
    /// to populate its indirection cell.
    /// </summary>
    public partial class ImportThunk
    {
        protected override void EmitCode(NodeFactory factory, ref X64Emitter instructionEncoder, bool relocsOnly)
        {
            switch (_thunkKind)
            {
                case Kind.Eager:
                    break;

                case Kind.DelayLoadHelper:
                    // xor eax, eax
                    instructionEncoder.EmitZeroReg(Register.RAX);

                    if (!relocsOnly)
                    {
                        // push table index
                        instructionEncoder.EmitPUSH((sbyte)_containingImportSection.IndexFromBeginningOfArray);
                    }

                    // push [module]
                    instructionEncoder.EmitPUSH(factory.ModuleImport);

                    break;

                case Kind.DelayLoadHelperWithExistingIndirectionCell:
                    // Indirection cell is already in rax which will be first arg. Used for fast tailcalls.

                    if (!relocsOnly)
                    {
                        // push table index
                        instructionEncoder.EmitPUSH((sbyte)_containingImportSection.IndexFromBeginningOfArray);
                    }

                    // push [module]
                    instructionEncoder.EmitPUSH(factory.ModuleImport);

                    break;

                case Kind.VirtualStubDispatch:
                    // mov rax, r11 - this is the most general case as the value of R11 also propagates
                    // to the new method after the indirection cell has been updated so the cell content
                    // can be repeatedly modified as needed during virtual / interface dispatch.
                    instructionEncoder.EmitMOV(Register.RAX, Register.R11);

                    if (!relocsOnly)
                    {
                        // push table index
                        instructionEncoder.EmitPUSH((sbyte)_containingImportSection.IndexFromBeginningOfArray);
                    }

                    // push [module]
                    instructionEncoder.EmitPUSH(factory.ModuleImport);

                    break;

                case Kind.Lazy:
                    instructionEncoder.EmitMOV(factory.Target.OperatingSystem == TargetOS.Windows ? Register.RDX : Register.RSI, factory.ModuleImport);

                    break;

                default:
                    throw new NotImplementedException();
            }

            instructionEncoder.EmitJMP(_helperCell);
        }
    }
}
