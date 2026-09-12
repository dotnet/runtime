// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Internal.TypeSystem;
using ILCompiler.DependencyAnalysis.Wasm;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis
{
    public interface INodeWithTypeSignature : ISymbolDefinitionNode
    {
        MethodSignature Signature { get; }
        bool IsUnmanagedCallersOnly { get; }
        bool IsAsyncCall { get; }
        bool HasGenericContextArg { get; }
    }

    internal static class NodeWithTypeSignatureExtensions
    {
        public static WasmFuncType GetWasmFunctionType(this INodeWithTypeSignature node)
        {
            WasmLowering.LoweringFlags flags = WasmLowering.LoweringFlags.None;
            if (node.HasGenericContextArg)
                flags |= WasmLowering.LoweringFlags.HasGenericContextArg;
            if (node.IsAsyncCall)
                flags |= WasmLowering.LoweringFlags.IsAsyncCall;
            if (node.IsUnmanagedCallersOnly)
                flags |= WasmLowering.LoweringFlags.IsUnmanagedCallersOnly;

            return WasmLowering.GetSignature(node.Signature, flags).FuncType;
        }
    }

    public interface IMethodCodeNodeWithTypeSignature : IMethodNode, INodeWithTypeSignature
    {
        MethodSignature INodeWithTypeSignature.Signature => Method.Signature;
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => Method.IsUnmanagedCallersOnly;
        bool INodeWithTypeSignature.IsAsyncCall => Method.IsAsyncCall();
        bool INodeWithTypeSignature.HasGenericContextArg => Method.RequiresInstMethodDescArg() || Method.RequiresInstMethodTableArg();
    }
}
