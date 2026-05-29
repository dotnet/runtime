// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Interface for nodes that have funclet-level unwind and exception handling metadata.
    /// </summary>
    public interface INodeWithFunclets : ISymbolNode
    {
        FrameInfo[] FrameInfos { get; }

        ObjectNode.ObjectData EHInfo { get; }
    }
}
