// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis
{
    public partial class UnboxingStubNode
    {
        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            WasmFuncType signature = WasmLowering.GetSignature(Method).FuncType;
            int parameterCount = signature.Params.Types.Length;

            WasmExpr[] expressions = new WasmExpr[parameterCount + 3];
            expressions[0] = Local.Get(0);
            expressions[1] = Local.Get(1);
            expressions[2] = I32.Const(factory.Target.PointerSize);
            expressions[3] = I32.Add;

            for (int i = 2; i < parameterCount; i++)
            {
                expressions[i + 2] = Local.Get(i);
            }

            expressions[parameterCount + 2] = ControlFlow.ReturnCall(GetUnderlyingMethodEntrypoint(factory));
            encoder.FunctionBody = new WasmFunctionBody(signature, expressions);
        }
    }
}
