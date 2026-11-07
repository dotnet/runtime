// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.IL;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis
{
    public partial class UnboxingStubNode
    {
        protected override void ComputeNonRelocationBasedDependencies(DependencySink sink, NodeFactory factory)
        {
            base.ComputeNonRelocationBasedDependencies(sink, factory);

            if (factory.Target.IsWasm && IsInstantiating && Method.OwningType.IsByRefLike)
            {
                // The throwing dummy stub doesn't reference its target, but the unboxing stub map needs it.
                // Non-WASM special unboxing thunks maintain this dependency through MethodAssociatedDataNode.
                sink.Add(factory.MethodEntrypoint(Method), "Non-unboxing method");
            }
        }

        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            WasmFuncType signature = WasmLowering.GetSignature(this).FuncType;
            if (IsInstantiating && Method.OwningType.IsByRefLike)
            {
                // Byref-like types cannot be boxed; preserve the throwing dummy entrypoint.
                encoder.FunctionBody = new WasmFunctionBody(signature,
                [
                    Local.Get(0),
                    ControlFlow.Call(factory.MethodEntrypoint(factory.TypeSystemContext.GetHelperEntryPoint(
                        "ThrowHelpers"u8, "ThrowNullReferenceException"u8))),
                    ControlFlow.Unreachable,
                ]);
                return;
            }

            int parameterCount = signature.Params.Types.Length;
            List<WasmExpr> expressions = new List<WasmExpr>(parameterCount + 5);
            expressions.Add(Local.Get(0));
            expressions.Add(Local.Get(1));
            expressions.Add(I32.Const(factory.Target.PointerSize));
            expressions.Add(I32.Add);

            int firstArgument = 2;
            if (IsInstantiating)
            {
                if (WasmLowering.LowerToAbiType(Method.Signature.ReturnType) is null
                    && !WasmLowering.IsEmptyStruct(Method.Signature.ReturnType))
                {
                    expressions.Add(Local.Get(firstArgument++));
                }

                expressions.Add(Local.Get(1));
                expressions.Add(I32.Load(0));
            }

            for (int i = firstArgument; i < parameterCount; i++)
            {
                expressions.Add(Local.Get(i));
            }

            expressions.Add(ControlFlow.ReturnCall(GetUnderlyingMethodEntrypoint(factory)));
            encoder.FunctionBody = new WasmFunctionBody(signature, expressions.ToArray());
        }
    }
}
