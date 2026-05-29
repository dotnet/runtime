// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Interface for nodes that have attached funclets. On platforms such as Wasm,
    /// we need to know what kind of funclets are attached to a method in order to emit
    /// the funclets as separate function definitions.
    /// </summary>
    public interface INodeWithFunclets : ISymbolNode
    {
        public FuncletKind[] GetFuncletKinds();
    }
}
