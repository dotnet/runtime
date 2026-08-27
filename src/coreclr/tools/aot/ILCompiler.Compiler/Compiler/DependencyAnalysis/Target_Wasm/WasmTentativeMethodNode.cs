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
                // The real body was marked, so forward all parameters and tail-call it.
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
                // The real body was removed, so call the throw helper with the shadow stack pointer.
                Debug.Assert(!Method.IsUnmanagedCallersOnly);

                expressions =
                [
                    Local.Get(0),
                    ControlFlow.Call(target),
                    ControlFlow.Unreachable,
                ];
            }

            encoder.FunctionBody = new WasmFunctionBody(signature, expressions);
        }
    }
}
