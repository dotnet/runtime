// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Globalization;

namespace System.Xml.Tests
{
    /// <summary>
    /// CXmlDriverScenario 
    /// </summary>
    public abstract class CXmlDriverScenario : CTestCase
    {
        private CXmlDriverParam _xmlDriverParams = null;
        private CultureInfo _requiredCultureInfo = null;

        public CXmlDriverParam XmlDriverParam { get { return _xmlDriverParams; } set { _xmlDriverParams = value; } }
        public CultureInfo RequiredCultureInfo { get { return _requiredCultureInfo; } set { _requiredCultureInfo = value; } }
        public new CXmlDriverVariation CurVariation { get { return (CXmlDriverVariation)base.CurVariation; } }

        public abstract int ExecuteVariation(CXmlDriverParam param);
    }


    internal class CXmlDriverErrorTestCase : CTestCase
    {
        private Exception _exception;
        private tagVARIATION_STATUS _returnCode;

        internal CXmlDriverErrorTestCase(string name, string desc, Exception e, CTestModule testModule) : this(name, desc, e, tagVARIATION_STATUS.eVariationStatusAborted, testModule) { }
        internal CXmlDriverErrorTestCase(string name, string desc, Exception e, tagVARIATION_STATUS returnCode, CTestModule testModule) :
            base(testModule, (desc.Length > 1000 ? desc.Substring(0, 1000) + "..." : desc))
        {
            this.Name = name;
            _exception = e;
            _returnCode = returnCode;
        }

        public override int Init(object param)
        {
            if (_exception != null)
            {
                CError.WriteLine("XmlDriver Error:");
                CTestBase.HandleException(_exception);
            }
            return (int)_returnCode;
        }
    }


    internal class CXmlDriverEmptyTestCase : CTestCase
    {
        private string _message;
        internal CXmlDriverEmptyTestCase(string name, string desc, string message, CTestModule testModule) : base(testModule, desc)
        {
            this.Name = name;
            _message = message;
        }

        public override int Init(object param)
        {
            CError.WriteLine("XmlDriver: " + _message);
            return TEST_SKIPPED;
        }
    }

    public class XmlDriverScenario : CAttrBase
    {
        private string _spec;
        private string[] _filters;
        private string _defaultSection;

        public XmlDriverScenario(string desc, string controlFile) : base(desc)
        {
            _spec = controlFile;
            _filters = null;
            _defaultSection = CXmlDriverEngine.DEFAULT_SECTION;
        }

        //Define Name property.     
        public virtual string ControlFile
        {
            get { return _spec; }
        }

        public virtual new string Filter
        {
            get { return _filters[0]; }
            set { _filters = new string[] { value }; }
        }

        public virtual string[] Filters
        {
            get { return _filters; }
            set { _filters = value; }
        }

        public virtual string DefaultSection
        {
            get { return _defaultSection; }
            set { _defaultSection = value; }
        }
    }
}
