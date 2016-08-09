// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

//This file loads the specified new control file and executes the variations.

namespace System.Xml.Tests
{
    /// <summary>
    /// This is the CXmlDriver related code.
    /// </summary>
    //[TestModule("Reader and Writer Factory Tests")]
    public partial class CFactoryModule : CXmlDriverModule
    {
        private string _testData;

        public string TestDataPath
        {
            get { return _testData; }
        }

        public override int Init(object o)
        {
            int ret = base.Init(o);

            _testData = Path.Combine(FilePathUtil.GetTestDataPath(), @"XmlReader");

            return ret;
        }
    }

    /// <summary>
    /// We will basically create two separate spec files and call the below method 
    /// for each of the spec file separately.
    /// </summary>
    //[XmlDriverScenario("XmlReader", "ReaderCreateSpec.xml")]
    //[XmlDriverScenario("XmlWriter", "WriterCreateSpec.xml")]
    public partial class CRWFactoryDriverScenario : CXmlDriverScenario
    {
        public override int ExecuteVariation(CXmlDriverParam param)
        {
            CError.WriteLine("Test Parameters : ");
            CError.WriteLine(this.CurVariation.Desc);
            CFactory f = null;
            string factoryToInvoke = param.SelectExistingValue("DriverFunction");
            switch (factoryToInvoke) //separates whether to call Reader or Writer
            {
                case "XmlReader":
                    f = new CReaderFactory();
                    break;
                case "XmlWriter":
                    f = new CWriterFactory();
                    break;
                default:
                    throw new
                            CTestFailedException("Invalid XmlDriverScenario passed in : " + factoryToInvoke);
            }
            CFactory.TestState testResult = f.TestVariation(param);
            if (testResult == CFactory.TestState.Pass)
                return TEST_PASS;
            else if (testResult == CFactory.TestState.Skip)
                return TEST_SKIPPED;

            return TEST_FAIL;
        }
    }


    /// <summary>
    /// Defines the basic functionality for any Factory
    /// </summary>
    public abstract class CFactory
    {
        protected CXmlDriverParam varInfo;

        protected bool isValid;
        public bool IsVariationValid
        {
            get { return isValid; }
        }

        protected string exceptionType;
        public string PExceptionType
        {
            get { return exceptionType; }
        }

        protected string exceptionMsg;
        public string PExceptionMsg
        {
            get { return exceptionMsg; }
        }

        //This controls the state machine.
        public enum TestState
        {
            Initial, //At Start.
            PreTest, //After PreTest is called and finished successfully.
            CreateSuccess, //After Create method is called.
            Consume,	//Before starting to use the object.
            Skip, //The case is skipped.
            Error,		// In case of an error. Should throw CTestFailedException anyways.
            Pass,	// Test() is successful.
            Complete //After Successful PostTest.
        };

        protected TestState pstate = TestState.Initial;

        protected string xmlFile;
        public string TestFileName
        {
            get { return xmlFile; }
        }

        protected string filePath;
        public string TestFilePath
        {
            get { return filePath; }
        }

        protected string httpPath;
        public string TestHttpPath
        {
            get { return httpPath; }
        }

        //Helper Method to get the full path of the File we want.
        protected string GetPath(string fileName, bool isHttp)
        {
            string root = TestFilePath;
            if (isHttp)
                root = TestHttpPath;


            if (root.EndsWith(@"\"))
            {
                if (fileName.StartsWith(@"\"))
                {
                    return (root + fileName.Substring(1));
                }
                return (root + fileName);
            }
            else
            {
                if (fileName.StartsWith(@"\"))
                {
                    return (root + fileName);
                }
                return (root + @"\" + fileName);
            }
        }

        protected string GetFile(string fileName)
        {
            return GetPath(fileName, false);
        }

        protected string GetUrl(string url)
        {
            return GetPath(url, true);
        }

        /// <summary>
        /// Init does the following :
        /// Store Parameter Info.
        /// Parse out the commonly required and universal tags
        /// </summary>
        public virtual void Init(CXmlDriverParam param)
        {
            varInfo = param;
            string resultType = varInfo.SelectExistingValue("Result/@Type", "Data");
            if (resultType == "Valid")
                isValid = true;
            else
                isValid = false;

            exceptionType = varInfo.SelectValue("Result/ExceptionType", "Data");
            exceptionMsg = varInfo.SelectValue("Result/ExceptionMessage", "Data");
            xmlFile = varInfo.SelectValue("Result/TestXmlFile", "Data");
            filePath = varInfo.SelectValue("filepath", "Data");
            filePath = FilePathUtil.ExpandVariables(filePath);
            if (filePath == null)
            {
                Log("Setting filePath = " + filePath);
            }
            httpPath = varInfo.SelectValue("httppath", "Data");
        }

        public virtual void DumpVariationInfo()
        {
            CError.WriteIgnore(this.IsVariationValid + "\n");
            CError.WriteIgnore(this.TestFileName + "\n");
            CError.WriteIgnore(this.PExceptionType + "\n");
            CError.WriteIgnore(this.PExceptionMsg + "\n");
        }

        public void Log(string str)
        {
            CError.WriteLineIgnore(str);
        }
        /// <summary>
        /// This method will be called by ExecuteVariation and it will 
        /// orchestrate the state.
        /// </summary>
        /// <param name="param"></param>
        /// <returns>TEST_SUCCESS, TEST_FAIL or TEST_SKIPPED
        /// </returns>
        public TestState TestVariation(CXmlDriverParam param)
        {
            // The following commands actually test the variation.
            Init(param);

            PreTest();
            CError.Compare(pstate, TestState.PreTest, "Invalid State: " + pstate);
            try
            {
                Test();
                if (pstate == TestState.Skip)
                {
                    return TestState.Skip;
                }
                CError.Compare(pstate, TestState.Pass, "Invalid State: " + pstate);
            }
            catch (Exception readerException)
            {
                Log(readerException.ToString());
                if (!IsVariationValid)
                {
                    if (!CheckException(readerException))
                    {
                        pstate = TestState.Error;
                        DumpVariationInfo();
                        throw readerException;
                    }
                    else
                    {
                        pstate = TestState.Pass;
                    }
                }
                else //Variation was valid
                {
                    pstate = TestState.Error;
                    DumpVariationInfo();
                    throw readerException;
                }
            }
            finally //do we need this style? what if somehow state is not pass, investigate
            {
                PostTest();
            }

            Log("State at the End: " + pstate);

            if (pstate == TestState.Complete)
                return TestState.Pass;

            return TestState.Error;
        }

        /// <summary>
        /// These are the abstract methods which each factory 
        /// will implement and know how to test the variation
        /// </summary>
        protected abstract void PreTest();
        protected abstract void Test();
        protected abstract void PostTest();

        /// <summary>
        /// This method compares the given exception with the expected exception for this variation
        /// </summary>
        /// <param name="actualException">The actual exception that got thrown</param>
        /// <returns>true if the actual exception matches the expected exception</returns>
        protected bool CheckException(Exception actualException)
        {
            //Very primitive checking to startoff with.
            return (actualException.GetType().ToString() == exceptionType);
        }

        /// <summary>
        /// Reads the value of the tag in the Spec file under the FilterCriteria Section
        /// </summary>
        /// <param name="tag">Name of the tag to read.</param>
        /// <param name="throwOnNull">IF true, will check for null and throw, 
        /// set to true if you want to throw an exception if value is not found. 
        /// If set to false, the return value can be null</param>
        /// <returns>Value of the tag under FilterCriteria Section</returns>
        protected string ReadFilterCriteria(string tag, bool throwOnNull)
        {
            CError.WriteIgnore("Filtering " + tag + " : ");
            string s = null;
            if (throwOnNull)
            {
                s = varInfo.SelectExistingValue(tag, "FilterCriteria");
                Log(s);
            }
            else
            {
                s = varInfo.SelectValue(tag, "FilterCriteria");
                Log(s);
            }
            return s;
        }

        /// <summary>
        /// Reads the value of the tag in the Spec file under the Data Section
        /// </summary>
        /// <param name="tag">Name of the tag to read.</param>
        /// <param name="throwOnNull">IF true, will check for null and throw, 
        /// set to true if you want to throw an exception if value is not found. 
        /// If set to false, the return value can be null</param>
        /// <returns>Value of the tag under Data Section</returns>
        protected string ReadData(string tag, bool throwOnNull)
        {
            CError.WriteIgnore("Loading .. " + tag + " : ");
            string s = null;
            if (throwOnNull)
            {
                s = varInfo.SelectExistingValue(tag, "Data");
                Log(s);
            }
            else
            {
                s = varInfo.SelectValue(tag, "Data");
                Log(s);
            }
            return s;
        }
    }

    #region CustomReader
    /// <summary>
    /// CustomReader which wraps Factory created reader.
    /// </summary>

    public class CustomReader : XmlReader
    {
        private XmlReader _tr = null;

        public CustomReader(string filename)
        {
            _tr = ReaderHelper.Create(filename);
        }

        public CustomReader(Stream stream, bool isFragment)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            if (!isFragment)
                _tr = ReaderHelper.Create(stream, settings, (string)null);
            else
            {
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                _tr = ReaderHelper.Create(stream, settings, (string)null);
            }
        }

        public CustomReader(TextReader txtReader, bool isFragment)
        {
            XmlReaderSettings settings = new XmlReaderSettings();

            if (!isFragment)
                _tr = ReaderHelper.Create(txtReader, settings, (string)null);
            else
            {
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                _tr = ReaderHelper.Create(txtReader, settings, (string)null);
            }
        }

        public CustomReader(string url, bool isFragment)
        {
            XmlReaderSettings settings = new XmlReaderSettings();

            if (!isFragment)
                _tr = ReaderHelper.Create(url, settings);
            else
            {
                settings.ConformanceLevel = ConformanceLevel.Fragment;
                _tr = ReaderHelper.Create(url, settings, null);
            }
        }

        public CustomReader(Stream stream)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            _tr = ReaderHelper.Create(stream, settings, (string)null);
        }

        public override int Depth { get { return _tr.Depth; } }
        public override string Value { get { return _tr.Value; } }
        public override bool MoveToElement() { return _tr.MoveToElement(); }
        public override string LocalName { get { return _tr.LocalName; } }
        public override XmlNodeType NodeType { get { return _tr.NodeType; } }
        public override bool MoveToNextAttribute() { return _tr.MoveToNextAttribute(); }
        public override bool MoveToFirstAttribute() { return _tr.MoveToFirstAttribute(); }
        public override string LookupNamespace(string prefix) { return _tr.LookupNamespace(prefix); }

        public new void Dispose()
        {
            _tr.Dispose();
        }

        public override bool EOF { get { return _tr.EOF; } }

        public override bool HasValue { get { return _tr.HasValue; } }

        public override string NamespaceURI { get { return _tr.NamespaceURI; } }

        public override bool Read() { return _tr.Read(); }

        public override XmlNameTable NameTable { get { return _tr.NameTable; } }

        public override bool CanResolveEntity { get { return _tr.CanResolveEntity; } }

        public override void ResolveEntity()
        {
            _tr.ResolveEntity();
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            return _tr.GetAttribute(name, namespaceURI);
        }

        public override string GetAttribute(string name)
        {
            return _tr.GetAttribute(name);
        }

        public override string GetAttribute(int i)
        {
            return _tr.GetAttribute(i);
        }

        public override string BaseURI { get { return _tr.BaseURI; } }

        public override bool ReadAttributeValue() { return _tr.ReadAttributeValue(); }

        public override string Prefix { get { return _tr.Prefix; } }

        public override bool MoveToAttribute(string name, string ns)
        {
            return _tr.MoveToAttribute(name, ns);
        }

        public override bool MoveToAttribute(string name)
        {
            return _tr.MoveToAttribute(name);
        }

        public override int AttributeCount { get { return _tr.AttributeCount; } }
        public override bool IsEmptyElement { get { return _tr.IsEmptyElement; } }
        public override ReadState ReadState { get { return _tr.ReadState; } }
        public override XmlReaderSettings Settings { get { return _tr.Settings; } }
    }
    #endregion
}
