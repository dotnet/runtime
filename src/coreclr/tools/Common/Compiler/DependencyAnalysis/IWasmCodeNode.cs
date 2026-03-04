// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    // TODO-Wasm: We may need to refactor here to split the functionality of
    // this interface into a separate `IWasmNodeWithTypeReference` or similar
    // once we add the concept of Wasm imports (since imports will also need signatures).
    public interface IWasmCodeNode : ISymbolDefinitionNode
    {
        WasmTypeNode GetWasmTypeSignature(NodeFactory factory);
    }
}
