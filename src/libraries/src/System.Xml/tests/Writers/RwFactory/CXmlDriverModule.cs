// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    /// <summary>
    /// CXmlDriverModule
    /// </summary>
    public abstract class CXmlDriverModule : CTestModule
    {
        private CXmlDriverEngine _xmlDriverEngine;
        private CXmlDriverParam[] _xmlDriverParams;

        public override void DetermineTestCases()
        {
            // determine base test cases
            base.DetermineTestCases();
            XmlDriverEngine.BuildTest();
        }

        public CXmlDriverEngine XmlDriverEngine
        {
            get
            {
                if (_xmlDriverEngine == null)
                    _xmlDriverEngine = new CXmlDriverEngine(this);
                return _xmlDriverEngine;
            }
        }

        public CXmlDriverParam XmlDriverParam
        {
            get
            {
                if (_xmlDriverParams == null && _xmlDriverParams.Length == 0)
                    throw new CXmlDriverException("No control file is specified.");
                return _xmlDriverParams[0];
            }
            set
            {
                _xmlDriverParams = new CXmlDriverParam[] { value };
            }
        }

        public CXmlDriverParam[] XmlDriverParams
        {
            get
            {
                return _xmlDriverParams;
            }
            set
            {
                _xmlDriverParams = value;
            }
        }
    }
}
