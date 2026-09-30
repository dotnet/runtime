// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public interface IHasTypeSignature
    {
        MethodSignature Signature { get; }
        bool IsUnmanagedCallersOnly { get; }
        bool IsAsyncCall { get; }
        bool HasGenericContextArg { get; }
    }

    public interface INodeWithTypeSignature : ISymbolDefinitionNode, IHasTypeSignature
    {
    }

    public interface IMethodCodeNodeWithTypeSignature : IMethodNode, INodeWithTypeSignature
    {
        // Keep methods aligned with WasmLowering.GetSignature(MethodDesc)
        MethodSignature IHasTypeSignature.Signature => Method.Signature;
        bool IHasTypeSignature.IsUnmanagedCallersOnly => Method.IsUnmanagedCallersOnly;
        bool IHasTypeSignature.IsAsyncCall => Method.IsAsyncCall();
        bool IHasTypeSignature.HasGenericContextArg => Method.RequiresInstMethodDescArg() || Method.RequiresInstMethodTableArg() || Method.IsArrayAddressMethod();
    }
}
