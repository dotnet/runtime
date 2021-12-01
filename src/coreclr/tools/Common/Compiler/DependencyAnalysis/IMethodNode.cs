// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// A dependency analysis node that represents a method.
    /// </summary>
    public interface IMethodNode : ISortableSymbolNode
    {
        MethodDesc Method { get; }
    }
}
