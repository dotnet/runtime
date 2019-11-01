// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class RuntimeFunctionsGCInfoNode : ArrayOfEmbeddedDataNode<MethodGCInfoNode>
    {
        public RuntimeFunctionsGCInfoNode()
            : base("RuntimeFunctionsGCInfo_Begin", "RuntimeFunctionsGCInfo_End", null)
        {
        }

        public override int ClassCode => 316678892;

        public override ObjectNodeSection Section => ObjectNodeSection.ReadOnlyDataSection;

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;
    }
}
