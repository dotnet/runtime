// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class XNodeReplaceOnDocument2 : XNodeReplace
    {
        public override void AddChildren()
        {
            AddChild(new TestVariation(OnXDocument) { Attribute = new VariationAttribute("XDocument: Replace with single node") { Params = new object[] { 1, "<?xml version='1.0'?>\t<?PI?> <E><sub1/></E>\n <!--comx--> " }, Priority = 0 } });
        }
    }
}
