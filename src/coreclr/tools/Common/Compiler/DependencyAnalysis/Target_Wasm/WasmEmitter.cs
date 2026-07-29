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
    public struct WasmEmitter
    {
#if READYTORUN
        public WasmFunctionBody FunctionBody = null;
#endif

        private readonly NodeFactory _factory;
        private readonly bool _relocsOnly;

        public WasmEmitter(NodeFactory factory, bool relocsOnly)
        {
            _factory = factory;
            _relocsOnly = relocsOnly;
#if READYTORUN
            FunctionBody = null;
#endif
        }

        public bool Is64Bit => _factory.Target.PointerSize == 8;
        public bool RelocsOnly => _relocsOnly;

        public ObjectNode.ObjectData Encode(ISymbolDefinitionNode symbolDefinitionNode)
        {
#if READYTORUN
            byte[] encodedThunk = new byte[FunctionBody.EncodeSize()];
            FunctionBody.Encode(encodedThunk);

            Relocation[] relocs = new Relocation[FunctionBody.EncodeRelocationCount()];
            FunctionBody.EncodeRelocations(relocs.AsSpan());
    
            return new ObjectNode.ObjectData(encodedThunk, relocs, 1, new ISymbolDefinitionNode[] { symbolDefinitionNode });
#else
            throw new PlatformNotSupportedException("NativeAOT WebAssembly assembly stubs are not supported.");
#endif
        }
    }
}
