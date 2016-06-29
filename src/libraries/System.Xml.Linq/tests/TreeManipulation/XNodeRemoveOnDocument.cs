// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.ModuleCore;

namespace XLinqTests
{
    public class XNodeRemoveOnDocument : XNodeRemove
    {
        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("Remove from XDocument (with decl)") { Params = new object[] { 5, true }, Priority = 1 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("Remove from XDocument (no decl)") { Params = new object[] { 5, false }, Priority = 1 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("(BVT)Remove from XDocument (with decl)") { Params = new object[] { 3, true }, Priority = 0 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("Remove from XDocument - the only node (no decl)") { Params = new object[] { 1, false }, Priority = 2 } });
            AddChild(new TestVariation(OnDocument) { Attribute = new VariationAttribute("Remove from XDocument - the only node (with decl)") { Params = new object[] { 1, true }, Priority = 1 } });
        }
        #endregion
    }
}
