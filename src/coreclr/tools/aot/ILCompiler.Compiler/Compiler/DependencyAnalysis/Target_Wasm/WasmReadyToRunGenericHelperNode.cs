// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.Wasm;

namespace ILCompiler.DependencyAnalysis
{
    public partial class ReadyToRunGenericHelperNode
    {
        protected override void EmitCode(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            throw new PlatformNotSupportedException(
                "NativeAOT WebAssembly does not support runtime generic dictionary lookup helpers.");
        }

        protected virtual void EmitLoadGenericContext(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            throw new PlatformNotSupportedException(
                "NativeAOT WebAssembly runtime generic dictionary context loading is not supported.");
        }
    }

    public partial class ReadyToRunGenericLookupFromTypeNode
    {
        protected override void EmitLoadGenericContext(NodeFactory factory, ref WasmEmitter encoder, bool relocsOnly)
        {
            throw new PlatformNotSupportedException(
                "NativeAOT WebAssembly runtime generic dictionary context loading from a type is not supported.");
        }
    }
}
