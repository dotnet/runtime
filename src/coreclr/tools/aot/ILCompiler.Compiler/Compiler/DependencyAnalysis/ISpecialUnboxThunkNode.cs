// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// A dependency analysis node that represents a special instantiating unboxing stub.
    /// </summary>
    public interface ISpecialUnboxThunkNode : IMethodNode
    {
        bool IsSpecialUnboxingThunk { get; }
        ISymbolNode GetUnboxingThunkTarget(NodeFactory factory);
    }
}
