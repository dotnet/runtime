// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestCase
    //
    ////////////////////////////////////////////////////////////////
    public class CDataReaderTestCase : CGenericTestCase
    {
        private string _WRONG_EXCEPTION = "Catching Wrong Exception";

        private CDataReader _rReader;

        public CDataReaderTestCase()
            : base()
        {
            _rReader = new CDataReader(null, 0, null);
        }

        public override int Terminate(object objParam)
        {
            return base.Terminate(objParam);
        }
        public string WRONG_EXCEPTION
        {
            get { return _WRONG_EXCEPTION; }
        }
        protected int BoolToLTMResult(bool bResult)
        {
            if (bResult)
            {
                return TEST_PASS;
            }
            else
            {
                return TEST_FAIL;
            }
        }

        public void CreateTestFile(EREADER_TYPE eReaderType)
        {
            string strFileName = string.Empty;

            TestFiles.CreateTestFile(ref strFileName, eReaderType);
        }

        public string GetTestFileName(EREADER_TYPE eReaderType)
        {
            return TestFiles.GetTestFileName(eReaderType);
        }

        public void DeleteTestFile(EREADER_TYPE eReaderType)
        {
            TestFiles.RemoveDataReader(eReaderType);
        }

        public CDataReader DataReader
        {
            get { return _rReader; }
        }

        private string ReplaceSurrogates(string str)
        {
            int len = str.Length;
            for (int i = 0; i < len; i++)
            {
                if (str[i] >= 0xD800 && str[i] <= 0xDFFF)
                {
                    str = str.Replace(str[i], '?');
                }
            }

            return str;
        }

        protected void CheckXmlException(string expectedCode, XmlException e, int expectedLine, int expectedPosition)
        {
            string actualCode = expectedCode;
            CError.WriteLine("***Exception");
            CError.WriteLineIgnore(e.ToString());
            CError.Compare(e.LineNumber, expectedLine, "CheckXmlException:LineNumber");
            CError.Compare(e.LinePosition, expectedPosition, "CheckXmlException:LinePosition");

            CError.Compare(actualCode, expectedCode, "ec" + e.Message);
        }

        protected void CheckException(string expectedCode, Exception e)
        {
            string actualCode = expectedCode;
            CError.Compare(actualCode, expectedCode, "Error Code" + e.Message);
        }

        public override int ExecuteVariation(int index, object param)
        {
            PreExecuteVariation(index, param);
            int result = TEST_FAIL;
            try
            {
                result = base.ExecuteVariation(index, param);
            }
            finally
            {
                PostExecuteVariation(index, param);
            }
            return result;
        }

        public virtual void PreExecuteVariation(int index, object param)
        {
        }

        public virtual void PostExecuteVariation(int index, object param)
        {
        }
    }
}
