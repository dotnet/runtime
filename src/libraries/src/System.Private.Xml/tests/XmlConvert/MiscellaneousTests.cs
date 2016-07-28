// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    internal class MiscellaneousTests : CTestCase
    {
        #region Public Methods and Operators

        public override void AddChildren()
        {
            AddChild(new CVariation(v2) { Attribute = new Variation("v2 - XmlConvert.ToDouble does not handle -0 correctly") });
        }

        /// <summary>
        ///     XmlConvert.ToDouble does not handle -0 correctly
        /// </summary>
        /// <returns></returns>
        public int v2()
        {
            Double d1 = -0e0;
            Double d2 = -0;
            CError.Compare(XmlConvert.ToString(d1), "-0", "ToString1 failed");
            CError.Compare(XmlConvert.ToString(d2), "0", "ToString2 failed");
            CError.Compare(XmlConvert.ToString(XmlConvert.ToDouble("-0e0")), "-0", "ToDouble1 failed");
            CError.Compare(XmlConvert.ToString(XmlConvert.ToDouble("-0")), "-0", "ToDouble2 failed");
            return TEST_PASS;
        }
        #endregion
    }
}
