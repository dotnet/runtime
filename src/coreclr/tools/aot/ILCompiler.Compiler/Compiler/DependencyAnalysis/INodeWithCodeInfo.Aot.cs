// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    public interface INodeWithCodeInfo
    {
        FrameInfo[] FrameInfos
        {
            get;
        }

        byte[] GCInfo
        {
            get;
        }

        DebugEHClauseInfo[] DebugEHClauseInfos
        {
            get;
        }

        MethodExceptionHandlingInfoNode EHInfo
        {
            get;
        }

        ISymbolNode GetAssociatedDataNode(NodeFactory factory);
    }
}
