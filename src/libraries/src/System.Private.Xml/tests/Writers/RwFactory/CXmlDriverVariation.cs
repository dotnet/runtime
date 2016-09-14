// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
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

