// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Implemented by object nodes that can report the alignment of their data without having to
    /// produce the data itself. The value returned from <see cref="GetAlignment"/> must match the
    /// alignment the node bakes into its <c>GetData</c>/<c>GetDehydratableData</c> output, so that
    /// consumers relying on it (such as the JIT deciding whether an alignment-sensitive relocation
    /// against the node is legal) can never disagree with the actual layout.
    /// </summary>
    public interface IObjectNodeWithAlignment
    {
        int GetAlignment(NodeFactory factory);
    }
}
