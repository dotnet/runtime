// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis
{
    public partial class JumpStubNode
    {
        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            WasmFuncType signature = WasmLowering.GetSignature(this).FuncType;
            int parameterCount = signature.Params.Types.Length;

            WasmExpr[] expressions = new WasmExpr[parameterCount + 1];
            for (int i = 0; i < parameterCount; i++)
            {
                expressions[i] = Local.Get(i);
            }

            expressions[parameterCount] = ControlFlow.ReturnCall(_target);
            encoder.FunctionBody = new WasmFunctionBody(signature, expressions);
        }
    }
}
