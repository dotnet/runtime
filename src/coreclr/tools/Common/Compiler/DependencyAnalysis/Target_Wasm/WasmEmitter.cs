// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using ILCompiler.ObjectWriter;

// These #ifs will disappear when we enable NativeAOT support for Wasm
#if READYTORUN
using ILCompiler.ObjectWriter.WasmInstructions;
#endif

namespace ILCompiler.DependencyAnalysis.Wasm
{
    public struct WasmEmitter(NodeFactory factory, bool relocsOnly)
    {
#if READYTORUN
        public WasmFunctionBody FunctionBody = null;
#endif

        public bool Is64Bit => factory.Target.PointerSize == 8;
        public bool RelocsOnly => relocsOnly;

        public ObjectNode.ObjectData Encode(ISymbolDefinitionNode symbolDefinitionNode)
        {
#if READYTORUN
            byte[] encodedThunk = new byte[FunctionBody.EncodeSize()];
            Relocation[] relocs = new Relocation[FunctionBody.EncodeRelocationCount()];
            FunctionBody.Encode(encodedThunk.AsSpan());
            FunctionBody.EncodeRelocations(relocs.AsSpan());
    
            return new ObjectNode.ObjectData(encodedThunk, relocs, 1, new ISymbolDefinitionNode[] { symbolDefinitionNode });
#else
            return default(ObjectNode.ObjectData);
#endif
        }
    }
}
