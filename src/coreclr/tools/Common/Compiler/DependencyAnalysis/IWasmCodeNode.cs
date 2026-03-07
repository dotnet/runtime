// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    // TODO-Wasm: We may need to refactor here to split the functionality of
    // this interface into a separate `IWasmNodeWithTypeReference` or similar
    // once we add the concept of Wasm imports (since imports will also need signatures).
    public interface IWasmCodeNode : ISymbolDefinitionNode
    {
        MethodSignature Signature { get; }
        bool IsUnmanagedCallersOnly { get; }
    }

    public interface IWasmMethodCodeNode : IMethodNode, IWasmCodeNode
    {
        MethodSignature IWasmCodeNode.Signature => Method.Signature;
        bool IWasmCodeNode.IsUnmanagedCallersOnly => Method.IsUnmanagedCallersOnly;
    }
}
