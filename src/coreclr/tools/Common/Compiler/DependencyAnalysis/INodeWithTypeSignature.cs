// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public interface INodeWithTypeSignature : ISymbolDefinitionNode
    {
        MethodSignature Signature { get; }
        bool IsUnmanagedCallersOnly { get; }
    }

    public interface IMethodCodeNodeWithTypeSignature : IMethodNode, INodeWithTypeSignature
    {
        MethodSignature INodeWithTypeSignature.Signature => Method.Signature;
        bool INodeWithTypeSignature.IsUnmanagedCallersOnly => Method.IsUnmanagedCallersOnly;
    }
}
