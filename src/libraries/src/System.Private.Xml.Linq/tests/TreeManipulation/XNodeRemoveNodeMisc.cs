// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class XNodeRemoveNodeMisc : XNodeRemove
    {
        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(NodeWithNoParent) { Attribute = new VariationAttribute("Nodes with no parent") { Priority = 2 } });
            AddChild(new TestVariation(RemoveNodesFromMixedContent) { Attribute = new VariationAttribute("Removing nodes from mixed content throws InvalIdOperationException") { Priority = 2 } });
            AddChild(new TestVariation(UsagePattern1) { Attribute = new VariationAttribute("Usage patterns: Remove all children") { Priority = 3 } });
        }
        #endregion
    }
}
