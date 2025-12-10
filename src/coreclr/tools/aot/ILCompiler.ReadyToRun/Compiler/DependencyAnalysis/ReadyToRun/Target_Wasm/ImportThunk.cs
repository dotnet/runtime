// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using ILCompiler.DependencyAnalysis.Wasm;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This node emits a thunk calling DelayLoad_Helper with a given instance signature
    /// to populate its indirection cell.
    /// </summary>
    public partial class ImportThunk
    {
        protected override void EmitCode(NodeFactory factory, ref WasmEmitter instructionEncoder, bool relocsOnly)
        {
            // WASM-TODO: implement.
            // throw new NotImplementedException();
        }
    }
}
