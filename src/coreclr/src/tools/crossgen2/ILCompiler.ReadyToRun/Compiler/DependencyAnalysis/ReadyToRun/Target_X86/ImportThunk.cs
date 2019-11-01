// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Internal.Text;
using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis.X86;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This node emits a thunk calling DelayLoad_Helper with a given instance signature
    /// to populate its indirection cell.
    /// </summary>
    public partial class ImportThunk
    {
        protected override void EmitCode(NodeFactory factory, ref X86Emitter instructionEncoder, bool relocsOnly)
        {
            switch (_thunkKind)
            {
                case Kind.Eager:
                    break;

                case Kind.DelayLoadHelper:
                case Kind.VirtualStubDispatch:
                    instructionEncoder.EmitXOR(Register.EAX, Register.EAX);

                    if (!relocsOnly)
                    {
                        // push table index
                        instructionEncoder.EmitPUSH((sbyte)_instanceCell.Table.IndexFromBeginningOfArray);
                    }

                    // push [module]
                    instructionEncoder.EmitPUSH(_moduleImport);

                    break;

                case Kind.Lazy:
                    // mov edx, [module]
                    instructionEncoder.EmitMOV(Register.EDX, _moduleImport);
                    break;
            }
            instructionEncoder.EmitJMP(_helperCell);
        }
    }
}
