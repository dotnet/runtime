// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class XNodeReplaceOnElement : XNodeReplace
    {
        public override void AddChildren()
        {
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement: Replace with multiple nodes") { Params = new object[] { 4, "<A xmlns='ns1' xmlns:p='nsp'><e1/>text1<p:e2>innertext<innerelem/></p:e2>text2<!--comment-->text3<?PI clicl?></A>" }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("(BVT)XElement: Replace with multiple nodes") { Params = new object[] { 2, "<A xmlns='ns1' xmlns:p='nsp'><e1/>text1<p:e2>innertext<innerelem/></p:e2>text2<!--comment-->text3<?PI clicl?></A>" }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement: Replace with single nodes - text node only") { Params = new object[] { 1, "<A>I_am_the_text</A>" }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("(BVT)XElement (in document II.): Replace with multiple nodes") { Params = new object[] { 2, "<A xmlns='ns1' xmlns:p='nsp'><e1/>text1<p:e2>innertext<innerelem/></p:e2>text2<!--comment-->text3<?PI clicl?></A>", true }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement: Replace with multiple nodes - text node only") { Params = new object[] { 4, "<A>I_am_the_text</A>" }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement: Replace with single nodes - one child") { Params = new object[] { 1, "<A><x/></A>" }, Priority = 2 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement (in document II.): Replace with multiple nodes") { Params = new object[] { 4, "<A xmlns='ns1' xmlns:p='nsp'><e1/>text1<p:e2>innertext<innerelem/></p:e2>text2<!--comment-->text3<?PI clicl?></A>", true }, Priority = 1 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("(BVT)XElement (in document): Replace with multiple nodes") { Params = new object[] { 2, "<A xmlns='ns1' xmlns:p='nsp'><e1/>text1<p:e2>innertext<innerelem/></p:e2>text2<!--comment-->text3<?PI clicl?></A>", false }, Priority = 0 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement: Replace with multiple nodes - one child") { Params = new object[] { 4, "<A><?Pi data?></A>" }, Priority = 2 } });
            AddChild(new TestVariation(OnXElement) { Attribute = new VariationAttribute("XElement (in document): Replace with multiple nodes") { Params = new object[] { 4, "<A xmlns='ns1' xmlns:p='nsp'><e1/>text1<p:e2>innertext<innerelem/></p:e2>text2<!--comment-->text3<?PI clicl?></A>", false }, Priority = 1 } });
        }
    }
}
