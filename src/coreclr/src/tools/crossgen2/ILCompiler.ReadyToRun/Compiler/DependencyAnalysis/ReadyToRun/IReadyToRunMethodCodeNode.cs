// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public interface IReadyToRunMethodCodeNode : IMethodNode, ISymbolDefinitionNode
    {
        void SetCode(ObjectNode.ObjectData data);
        void InitializeFrameInfos(FrameInfo[] frameInfos);
        void InitializeGCInfo(byte[] gcInfo);
        void InitializeEHInfo(ObjectNode.ObjectData ehInfo);
        void InitializeDebugLocInfos(OffsetMapping[] debugLocInfos);
        void InitializeDebugVarInfos(NativeVarInfo[] debugVarInfos);
        void InitializeDebugEHClauseInfos(DebugEHClauseInfo[] debugEHClauseInfos);
        void InitializeInliningInfo(MethodDesc[] inlinedMethods);
    }
}
