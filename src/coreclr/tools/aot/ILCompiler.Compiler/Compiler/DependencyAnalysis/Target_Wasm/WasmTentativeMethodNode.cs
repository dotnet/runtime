// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter.WasmInstructions;

using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis
{
    public partial class TentativeMethodNode
    {
        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            WasmFuncType signature = WasmLowering.GetSignature(Method).FuncType;
            ISymbolNode target = GetTarget(factory);
            WasmExpr[] expressions;

            if (ReferenceEquals(target, RealBody))
            {
                int parameterCount = signature.Params.Types.Length;
                expressions = new WasmExpr[parameterCount + 1];
                for (int i = 0; i < parameterCount; i++)
                {
                    expressions[i] = Local.Get(i);
                }
                expressions[parameterCount] = ControlFlow.ReturnCall(target);
            }
            else
            {
                Debug.Assert(!Method.IsUnmanagedCallersOnly);
                Debug.Assert(signature.Params.Types.Length >= 2);

                expressions =
                [
                    Local.Get(0),
                    Local.Get(signature.Params.Types.Length - 1),
                    ControlFlow.Call(target),
                    ControlFlow.Unreachable,
                ];
            }

            encoder.FunctionBody = new WasmFunctionBody(signature, expressions);
        }
    }
}
