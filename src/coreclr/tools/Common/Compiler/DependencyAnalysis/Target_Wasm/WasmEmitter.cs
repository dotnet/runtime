// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.ObjectWriter;
using ILCompiler.ObjectWriter.WasmInstructions;

namespace ILCompiler.DependencyAnalysis.Wasm
{
    public struct WasmEmitter(NodeFactory factory, bool relocsOnly)
    {
        public WasmFunctionBody FunctionBody = null!;

        public bool Is64Bit => factory.Target.PointerSize == 8;
        public bool RelocsOnly => relocsOnly;

        public ObjectNode.ObjectData Encode(ISymbolDefinitionNode symbolDefinitionNode)
        {
            byte[] encodedThunk = new byte[FunctionBody.EncodeSize()];
            FunctionBody.Encode(encodedThunk);

            Relocation[] relocs = new Relocation[FunctionBody.EncodeRelocationCount()];
            FunctionBody.EncodeRelocations(relocs.AsSpan());

            return new ObjectNode.ObjectData(encodedThunk, relocs, 1, new ISymbolDefinitionNode[] { symbolDefinitionNode });
        }
    }
}
