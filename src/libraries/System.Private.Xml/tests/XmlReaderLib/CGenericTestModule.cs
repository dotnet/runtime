// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // All test module cases should inherit from CUniTestModule
    // Encapsulates the notion of a "Backend" which each test
    // needs to respect
    //
    ////////////////////////////////////////////////////////////////
    public class CGenericTestModule : CTestModule
    {
        public CGenericTestModule()
            : base()
        {
        }

        private string _TestData = null;
        public string TestData
        {
            get
            {
                return _TestData;
            }
        }

        private string _standardpath = null;
        public string StandardPath
        {
            get
            {
                return _standardpath;
            }
        }

        private ReaderFactory _ReaderFactory = null;

        public ReaderFactory ReaderFactory
        {
            get
            {
                return _ReaderFactory;
            }
            set
            {
                _ReaderFactory = value;
            }
        }

        private DateTime _StartTime;

        public override int Init(object objParam)
        {
            _TestData = Path.Combine(FilePathUtil.GetTestDataPath(), @"XmlReader");

            _TestData = _TestData.ToLowerInvariant();

            _standardpath = FilePathUtil.GetStandardPath();
            _standardpath = _standardpath.ToLowerInvariant();

            int ret = base.Init(objParam);

            _StartTime = DateTime.Now;

            return ret;
        }

        public override int Terminate(object objParam)
        {
            CError.WriteLine("Total running time = {0}", DateTime.Now - _StartTime);

            base.Terminate(objParam);
            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase
    //
    ////////////////////////////////////////////////////////////////
    public class CGenericTestCase : CTestCase
    {
        public CGenericTestCase()
            : base()
        {
        }

        public string TestData
        {
            get
            {
                return TestModule.TestData;
            }
        }

        public string StandardPath
        {
            get
            {
                return TestModule.StandardPath;
            }
        }

        public virtual new CGenericTestModule TestModule
        {
            get { return (CGenericTestModule)base.TestModule; }
            set { base.TestModule = value; }
        }
    }

    ////////////////////////////////////////////////////////////////
    // InheritRequired attribute
    //
    // This attribute is used to mark test case classes (TCxxx). A check
    // is performed at startup to ensure that each InheritRequired class
    // actually has at least one derived class
    // The attribute is applied to all classes that require implementation
    // for both XmlxxxReader and XmlValidatingReader and its only purpose is
    // to avoid to forgot implementation of some test in one or both
    // of the suites (Reader and ValidatingReader)
    ////////////////////////////////////////////////////////////////
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class InheritRequired : Attribute
    {
    }

    ////////////////////////////////////////////////////////////////
    // Common interface for creating different readers in derived 
    // modules
    ////////////////////////////////////////////////////////////////
    public abstract class ReaderFactory
    {
        public static string HT_FILENAME = "FILENAME";
        public static string HT_VALIDATIONTYPE = "VT";
        public static string HT_READERTYPE = "READERTYPE";
        public static string HT_STREAM = "STREAM";
        public static string HT_CURVAR = "CURVAR";
        public static string HT_CURDESC = "CURDESC";
        public static string HT_FRAGMENT = "FRAGMENT";
        public static string HT_SCHEMASET = "SCHEMASET";
        public static string HT_SCHEMACOLLECTION = "SCHEMACOLLECTION";
        public static string HT_VALIDATIONHANDLER = "VH";
        public static string HT_READERSETTINGS = "READERSETTINGS";
        public static string HT_STRINGREADER = "STRINGREADER";

        public abstract XmlReader Create(MyDict<string, object> options);

        private int _validationErrorCount = 0;
        private int _validationWarningCount = 0;
        private int _validationCallbackCount = 0;

        public int ValidationErrorCount
        {
            get { return _validationErrorCount; }
            set { _validationErrorCount = value; }
        }

        public int ValidationWarningCount
        {
            get { return _validationWarningCount; }
            set { _validationWarningCount = value; }
        }

        public int ValidationCallbackCount
        {
            get { return _validationCallbackCount; }
            set { _validationCallbackCount = value; }
        }

        public virtual void Initialize()
        {
            //To manage the Factory class.
        }

        public virtual void Terminate()
        {
            //To manage the Factory class.
        }
    }
}
