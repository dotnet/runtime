// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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