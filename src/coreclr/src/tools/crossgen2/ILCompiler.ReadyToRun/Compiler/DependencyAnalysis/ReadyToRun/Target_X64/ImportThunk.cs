// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    // lea rax, [pCell] - this is the simple case which allows for only one lazy resolution
                    // of the indirection cell; the final method pointer stored in the indirection cell
                    // no longer receives the location of the cell so it cannot modify it repeatedly.
                    instructionEncoder.EmitLEAQ(Register.RAX, _instanceCell);

                    if (!relocsOnly)
                    {
                        // push table index
                        instructionEncoder.EmitPUSH((sbyte)_instanceCell.Table.IndexFromBeginningOfArray);
                    }

                    // push [module]
                    instructionEncoder.EmitPUSH(_moduleImport);

                    break;

                case Kind.VirtualStubDispatch:
                    // mov rax, r11 - this is the most general case as the value of R11 also propagates
                    // to the new method after the indirection cell has been updated so the cell content
                    // can be repeatedly modified as needed during virtual / interface dispatch.
                    instructionEncoder.EmitMOV(Register.RAX, Register.R11);

                    if (!relocsOnly)
                    {
                        // push table index
                        instructionEncoder.EmitPUSH((sbyte)_instanceCell.Table.IndexFromBeginningOfArray);
                    }

                    // push [module]
                    instructionEncoder.EmitPUSH(_moduleImport);

                    break;

                case Kind.Lazy:
                    instructionEncoder.EmitMOV(factory.Target.OperatingSystem == TargetOS.Windows ? Register.RDX : Register.RSI, _moduleImport);

                    break;

                default:
                    throw new NotImplementedException();
            }

            instructionEncoder.EmitJMP(_helperCell);
        }
    }
}
