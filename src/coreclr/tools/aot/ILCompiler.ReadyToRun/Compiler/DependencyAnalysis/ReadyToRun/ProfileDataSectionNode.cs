// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ProfileDataSectionNode : ArrayOfEmbeddedDataNode<ProfileDataNode>
    {
        public ProfileDataSectionNode()
            : base("ProfileDataSectionNode_Begin", "ProfileDataSectionNode_End", new EmbeddedObjectNodeComparer(CompilerComparer.Instance))
        {
        }

        public override int ClassCode => 576050264;

        public override ObjectNodeSection Section => ObjectNodeSection.DataSection;

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;
    }
}
