// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using OLEDB.Test.ModuleCore;

namespace XmlConvertTests
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