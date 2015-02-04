// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class XContainerReplaceNodesOnXElement : XContainerReplaceNodes
    {
        public override void AddChildren()
        {
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("(BVT)XElement (text content): Replace with multiple nodes") { Params = new object[] { 2, "<A xmlns='ns1' xmlns:p='nsp'>_text_content_</A>", false }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("(BVT)XElement: Replace with multiple nodes") { Params = new object[] { 2, "<A xmlns='ns1' xmlns:p='nsp'><e1/>text1<p:e2>innertext<innerelem/></p:e2>text2<!--coment-->text3<?PI clicl?></A>", true }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement (text content): Replace with single node") { Params = new object[] { 1, "<A xmlns='ns1' xmlns:p='nsp'>_text_content_</A>", false }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement (text content): Replace with multiple nodes") { Params = new object[] { 4, "<A xmlns='ns1' xmlns:p='nsp'>_text_content_</A>", false }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement: Replace with single node") { Params = new object[] { 1, "<A xmlns='ns1' xmlns:p='nsp'><e1/>text1<p:e2>innertext<innerelem/></p:e2>text2<!--coment-->text3<?PI clicl?></A>", true }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement: Replace with multiple nodes") { Params = new object[] { 4, "<A xmlns='ns1' xmlns:p='nsp'><e1/>text1<p:e2>innertext<innerelem/></p:e2>text2<!--coment-->text3<?PI clicl?></A>", true }, Priority = 1 } });
        }
    }
}
