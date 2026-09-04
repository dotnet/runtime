// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysis.Wasm;
using ILCompiler.ObjectWriter.WasmInstructions;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis
{
    public partial class JumpStubNode
    {
        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            INodeWithTypeSignature signatureNode = (INodeWithTypeSignature)this;
            WasmLowering.LoweringFlags flags = WasmLowering.LoweringFlags.None;

            if (signatureNode.HasGenericContextArg)
            {
                flags |= WasmLowering.LoweringFlags.HasGenericContextArg;
            }

            if (signatureNode.IsAsyncCall)
            {
                flags |= WasmLowering.LoweringFlags.IsAsyncCall;
            }

            if (signatureNode.IsUnmanagedCallersOnly)
            {
                flags |= WasmLowering.LoweringFlags.IsUnmanagedCallersOnly;
            }

            WasmFuncType signature = WasmLowering.GetSignature(signatureNode.Signature, flags).FuncType;

            List<WasmExpr> expressions = new List<WasmExpr>(signature.Params.Types.Length + 1);
            for (int i = 0; i < signature.Params.Types.Length; i++)
            {
                expressions.Add(Local.Get(i));
            }

            expressions.Add(ControlFlow.Call(_target));
            encoder.FunctionBody = new WasmFunctionBody(signature, expressions.ToArray());
        }
    }
}
