// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class XNodeRemoveOnElement : XNodeRemove
    {
        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("(BVT)Remove from XElement (in XDocument)") { Params = new object[] { false, true, 2 }, Priority = 0 } });
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("Remove from XElement, with siblings (in XDocument)") { Params = new object[] { true, true, 4 }, Priority = 1 } });
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("Remove from XElement (standalone)") { Params = new object[] { false, false, 4 }, Priority = 1 } });
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("Remove from XElement (in XDocument) - Remove the only node") { Params = new object[] { false, true, 1 }, Priority = 1 } });
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("Remove from XElement (standalone) - Remove the only node") { Params = new object[] { false, false, 1 }, Priority = 1 } });
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("(BVT)Remove from XElement, with siblings (in XDocument)") { Params = new object[] { true, true, 2 }, Priority = 0 } });
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("Remove from XElement, with siblings (standalone)") { Params = new object[] { true, false, 4 }, Priority = 1 } });
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("Remove from XElement, with siblings (in XDocument) - Remove the only node") { Params = new object[] { true, true, 1 }, Priority = 1 } });
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("Remove from XElement, with siblings (standalone) - Remove the only node") { Params = new object[] { true, false, 1 }, Priority = 1 } });
            AddChild(new TestVariation(OnElement) { Attribute = new VariationAttribute("Remove from XElement (in XDocument)") { Params = new object[] { false, true, 4 }, Priority = 1 } });
        }
        #endregion
    }
}
