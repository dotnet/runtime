// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class XContainerReplaceNodesOnDocument : XContainerReplaceNodes
    {
        public override void AddChildren()
        {
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("XDocument(whitespace only): Replace with multiple nodes") { Params = new object[] { 4, "\t", true }, Priority = 1 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("XDocument(empty): Replace with single node") { Params = new object[] { 1, "", true }, Priority = 2 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("XDocument(regular): Replace with multiple nodes") { Params = new object[] { 4, "\n<?PI?><root Id='a0'/>\t<!--comment-->", true }, Priority = 1 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("XDocument(empty): Replace with multiple nodes") { Params = new object[] { 4, "", true }, Priority = 2 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("(BVT)XDocument(regular): Replace with multiple nodes") { Params = new object[] { 2, "\n<?PI?><root Id='a0'/>\t<!--comment-->", true }, Priority = 0 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("XDocument(whitespace only): Replace with single node") { Params = new object[] { 1, "\t", true }, Priority = 1 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("XDocument(regular): Replace with single node") { Params = new object[] { 1, "\n<?PI?><root Id='a0'/>\t<!--comment-->", true }, Priority = 0 } });
        }
    }
}
