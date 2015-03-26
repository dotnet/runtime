// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Globalization;
using OLEDB.Test.ModuleCore;

namespace Webdata.Test.XmlDriver
{
    /// <summary>
    /// CXmlDriverVariation
    /// </summary>
    public class CXmlDriverVariation : CVariation
    {
        private CXmlDriverParam _xmlDriverParams;

        //Constructor
        internal CXmlDriverVariation(CXmlDriverScenario testCase,
            string name, string description, int id, int pri,
            CXmlDriverParam xmlDriverParams) : base(testCase)
        {
            _xmlDriverParams = xmlDriverParams;

            // use name as a description if provided
            if (name != null)
                this.Desc = name;
            else
                this.Desc = description;
            this.Name = name;


            this.Pri = pri;
            this.id = id;
        }


        private bool CheckSkipped()
        {
            string skipped = XmlDriverParam.GetTopLevelAttributeValue("Skipped");
            if (skipped == null || !bool.Parse(skipped))
                return true;
            return false;
        }

        public override tagVARIATION_STATUS Execute()
        {
            tagVARIATION_STATUS res = (tagVARIATION_STATUS)TEST_FAIL;

            try
            {
                if (!CheckSkipped()) return (tagVARIATION_STATUS)TEST_SKIPPED;

                CXmlDriverScenario scenario = (CXmlDriverScenario)Parent;

                res = (tagVARIATION_STATUS)scenario.ExecuteVariation(XmlDriverParam);
            }
            catch (CTestSkippedException e)
            {
                res = (tagVARIATION_STATUS)HandleException(e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                res = (tagVARIATION_STATUS)HandleException(e);
            }

            return res;
        }

        public CXmlDriverParam XmlDriverParam { get { return _xmlDriverParams; } set { _xmlDriverParams = value; } }
    }
}

