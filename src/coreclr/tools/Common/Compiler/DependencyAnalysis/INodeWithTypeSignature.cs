// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using ILCompiler.DependencyAnalysis.Wasm;

using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public interface INodeWithWasmSignature : ISymbolDefinitionNode
    {
        WasmSignature WasmSignature { get; }
    }

    public interface INodeWithTypeSignature : INodeWithWasmSignature
    {
        MethodSignature Signature { get; }
        bool IsUnmanagedCallersOnly { get; }
        bool IsAsyncCall { get; }
        bool HasGenericContextArg { get; }

        WasmSignature INodeWithWasmSignature.WasmSignature => WasmLowering.GetSignature(this);
    }

    public interface IMethodCodeNodeWithTypeSignature : IMethodNode, INodeWithTypeSignature
    {
        // Keep methods aligned with WasmLowering.GetSignature(MethodDesc)
        MethodSignature INodeWithTypeSignature.Signature => Method.Signature;
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => Method.IsUnmanagedCallersOnly;
        bool INodeWithTypeSignature.IsAsyncCall => Method.IsAsyncCall();
        bool INodeWithTypeSignature.HasGenericContextArg => Method.RequiresInstMethodDescArg() || Method.RequiresInstMethodTableArg() || Method.IsArrayAddressMethod();
    }
}
