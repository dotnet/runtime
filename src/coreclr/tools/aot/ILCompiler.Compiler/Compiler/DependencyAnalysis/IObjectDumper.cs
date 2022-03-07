// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ObjectData = ILCompiler.DependencyAnalysis.ObjectNode.ObjectData;

namespace ILCompiler.DependencyAnalysis
{
    public interface IObjectDumper
    {
        void DumpObjectNode(NameMangler mangler, ObjectNode node, ObjectData objectData);
    }
}
