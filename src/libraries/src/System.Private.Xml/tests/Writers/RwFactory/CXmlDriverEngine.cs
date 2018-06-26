// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    /// <summary>
    /// CXmlDriverEngine
    /// </summary>
    public class CXmlDriverEngine
    {
        public const string CMD_LANG_KEY = "LANG";
        public const string CMD_SET_LANG_KEY = "SETLANGUAGE";
        public const string CMD_PRIORITY_FILTER_LTM_KEY = "MAXPRIORITY";
        public const string CMD_FILTER_LTM_KEY = "FILTER";
        public const string CMD_SPEC_LTM_KEY = "XMLTESTMODULE";
        public const string CMD_ROOT_PATH_KEY = "ROOTPATH";
        public const string CMD_SPEC_DEFAULT_SECTION = "DEFAULT_SECTION";
        public const string DEFAULT_SECTION = "Data";
        public const int DEFAULT_VAR_PRIORITY = 2;
        public const bool DEFAULT_VAR_IMPLEMENTED = true;
        public const bool DEFAULT_VAR_SKIPPED = false;
        public const string DEFAULT_VAR_SECURITY = "none";
        public const bool DEFAULT_VAR_ERROR = false;
        public const string DEFAULT_VAR_LANGUAGE = "";
        public const int MAX_SECTION_COUNT = 10; // support max 10 sections per level

        private CTestModule _testModule = null;
        private IList _testModuleParams = null;
        private Exception _parseError = null;
        private string _confirmanceErrors = null;

        private CultureInfo _requiredCultureInfo = null;
        private string _requiredLanguage = null;
        private bool _isRequiredCultureInfoNeedToBeSet = false;

        public CXmlDriverEngine(CTestModule testModule)
        {
            _testModule = testModule;
            _testModuleParams = new List<CXmlDriverParam>();
        }


        public void BuildTest()
        {
            DetermineTestCases();
        }

        public void AddTestCases(string specFile)
        {
            AddTestCases("", specFile, null, DEFAULT_SECTION);
        }

        public void AddTestCases(string testCaseNamePrefix, string specFile, string[] filters, string defaultSection)
        {
            ParseControlFileSafe(testCaseNamePrefix, specFile, filters, defaultSection, null, null, _testModuleParams);
        }

        private string BuildFilterFromMaxPriority(string maxPriority)
        {
            string res = "@Pri<=" + maxPriority;
            return res;
        }

        private static Array CombineArrays(Array arr1, Array arr2)
        {
            if (arr1 == null)
                return arr2;
            if (arr2 == null)
                return arr1;

            Array newArr = Array.CreateInstance(arr1.GetType().GetElementType(), arr1.Length + arr2.Length);
            arr1.CopyTo(newArr, 0);
            arr2.CopyTo(newArr, arr1.Length);
            return newArr;
        }

        // determines test cases, gets spec file from test cases attribute or from command line and 
        // starts parsing of the spec file.
        // SpecFile passed as a parameter in the command line overrides local spec file defined in the attribute
        protected virtual void DetermineTestCases()
        {
            try
            {
                _parseError = null;

                // get control file
                string cmdSpecFile = null;
                if (CModCmdLine.CmdLine.ContainsKey(CMD_SPEC_LTM_KEY))
                    cmdSpecFile = (string)CModCmdLine.CmdLine[CMD_SPEC_LTM_KEY];

                // get max priority
                string[] tmp1 = null;
                string[] tmp2 = null;
                if (CModCmdLine.CmdLine.ContainsKey(CMD_PRIORITY_FILTER_LTM_KEY))
                    tmp1 = new string[] { BuildFilterFromMaxPriority((string)CModCmdLine.CmdLine[CMD_PRIORITY_FILTER_LTM_KEY]) };
                if (CModCmdLine.CmdLine.ContainsKey(CMD_FILTER_LTM_KEY))
                    tmp2 = new string[] { (string)CModCmdLine.CmdLine[CMD_FILTER_LTM_KEY] };
                string[] cmdFilters = (string[])CombineArrays(tmp1, tmp2);

                // get default section
                string cmdDefaultSection = null;
                if (CModCmdLine.CmdLine.ContainsKey(CMD_SPEC_DEFAULT_SECTION))
                    cmdDefaultSection = (string)CModCmdLine.CmdLine[CMD_SPEC_DEFAULT_SECTION];


                XmlDriverScenario XmlReaderScenario = new XmlDriverScenario("XmlReader", "ReaderCreateSpec.xml");
                XmlDriverScenario XmlWriterScenario = new XmlDriverScenario("XmlWriter", "WriterCreateSpec.xml");
                List<XmlDriverScenario> slist = new List<XmlDriverScenario>();
                slist.Add(XmlReaderScenario);
                slist.Add(XmlWriterScenario);

                foreach (XmlDriverScenario scenarioAttr in slist)
                {
                    string specFile = (cmdSpecFile == null) ? scenarioAttr.ControlFile : cmdSpecFile;
                    string name = scenarioAttr.Name;
                    string[] filters = (string[])CombineArrays(cmdFilters, scenarioAttr.Filters);
                    string defaultSection = (cmdDefaultSection == null) ? scenarioAttr.DefaultSection : cmdDefaultSection;

                    if (specFile == null || specFile == string.Empty)
                        throw new CXmlDriverException("Control File is not defined for TestCase \"" + scenarioAttr.Desc + "\"");

                    // parse spec file and update testModule's param list
                    ParseControlFile(name, specFile, filters, defaultSection, null, null, _testModuleParams);
                }
            }

            catch (Exception e)
            {
                HandleException(e);
            }
        }


        private void HandleException(Exception e)
        {
            // There is no way to display an error message in the LTM output at this time due to TestConsole/Error is not set,
            // so create an "Error" test case that will show this error during test execution
            _testModule.AddTestCase(new CXmlDriverErrorTestCase("Error! (" + e.Message + ")", e.Message, e, _testModule));
            _parseError = e;
        }

        private static string SelectExistingValue(XElement node, string xpath)
        {
            XElement resNode = node.Element(xpath);
            if (resNode == null)
                throw new CXmlDriverException(xpath + " was not found for " + NodeName(node));
            return resNode.Value;
        }

        private static string SelectExistingAttributeValue(XElement node, string attrName)
        {
            XAttribute resNode = node.Attribute(attrName);
            if (resNode == null)
                throw new CXmlDriverException("@" + attrName + " was not found for " + NodeName(node));
            return resNode.Value;
        }


        private static string SelectValue(XElement node, string xpath, string defaultValue)
        {
            XElement resNode = node.Element(xpath);
            if (resNode == null)
                return defaultValue;
            return resNode.Value;
        }

        private static string SelectAttributeValue(XElement node, string attrName, string defaultValue)
        {
            XAttribute resNode = node.Attribute(attrName);
            if (resNode == null)
                return defaultValue;
            return resNode.Value;
        }

        private static void CreateAttributeIfNotExists(XElement node, string attrName, string value)
        {
            XAttribute attr = node.Attribute(attrName);
            if (attr == null)
            {
                attr = new XAttribute(attrName, value);
                node.Add(attr);
            }
        }

        private static CXmlDriverParam[] ToXmlDriverParamArray(IList list)
        {
            CXmlDriverParam[] arr = new CXmlDriverParam[list.Count];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = (CXmlDriverParam)list[i];
            return arr;
        }


        private static string CombineXPath(string[] xpaths)
        {
            if (xpaths == null || xpaths.Length == 0)
                return null;

            string xpath = string.Empty;
            if (xpaths.Length == 1)
                xpath = xpaths[0];

            else
                foreach (string item in xpaths)
                {
                    if (xpath != string.Empty)
                        xpath += " and ";
                    xpath += "(" + item + ")";
                }
            xpath = "self::*[" + xpath + "]";

            return xpath;
        }

        private static bool CheckFilter(CXmlDriverParamRawNodes param, string filterXPath)
        {
            // check whether or not the item is implemented,
            // don't use VirtualNode here to reduce total time for parsing the control file
            string implemented = CXmlDriverParam.GetTopLevelAttributeValue_Inline(param, "Implemented");
            if (implemented != null && !bool.Parse(implemented))
                return false;

            if (filterXPath == null)
                return true;

            // execute xpath
            XElement tmp = param.Virtual.Element(filterXPath);
            bool res = tmp != null;
            return res;
        }


        private static string NodeName(XElement parent, XElement child)
        {
            string name = NodeName(parent) +
                (child == null ? "" : "/" + NodeName(child));
            return name;
        }

        private static string NodeName(XElement node)
        {
            string name = "";

            if (node != null)
            {
                name += node.Name;
                if (node.NodeType == XmlNodeType.Element)
                {
                    XAttribute tmp = node.Attribute("Name");
                    if (tmp != null)
                        name += "[" + tmp.Value + "]";
                    else
                    {
                        XAttribute tmp1 = node.Attribute("Id");
                        if (tmp1 != null)
                            name += "[" + tmp1.Value + "]";
                    }
                }
            }

            return name;
        }


        private static void AppendAttributes(XElement res, XElement child)
        {
            foreach (XAttribute attr in child.Attributes())
            {
                if (res.Attribute(attr.Name) != null)
                {
                    res.SetAttributeValue(attr.Name, attr.Value);
                }
                else
                    res.Add(new XAttribute(attr));
            }
        }


        private static XElement[] BuildElementList(XElement node, out int listCount)
        {
            XElement[] childList = Enumerable.ToArray(node.Elements());
            listCount = Enumerable.Count(node.Elements());
            return childList;
        }


        private static XElement[] BuildSectionList(XElement node, out int listCount)
        {
            int childNodesCount = Enumerable.Count(node.Elements());
            XElement[] childList = new XElement[(MAX_SECTION_COUNT < childNodesCount) ? MAX_SECTION_COUNT : childNodesCount];

            listCount = 0;
            foreach (XElement cur in node.Elements())
            {
                if (cur.NodeType != XmlNodeType.Element ||
                    cur.Name.LocalName == "Description" || cur.Name.LocalName == "Owner")
                    continue;

                // ignore any sections after <TestCase> or <Variation>
                if (cur.Name.LocalName == "Variation" || cur.Name.LocalName == "TestCase")
                    break;

                childList[listCount] = cur;
                listCount++;
            }
            return childList;
        }


        private static bool HasChildElements(XElement node)
        {
            return node.Elements().Any();
        }


        private static XElement FindElementAndRemoveIt(string name, int pos, XElement[] list, int listCount)
        {
            XElement node = null;
            for (int curPos = 0, i = 0; i < listCount; i++)
            {
                XElement curNode = list[i];
                if (curNode == null)
                    continue;

                if (name == curNode.Name.LocalName)
                    if (pos == curPos)
                    {
                        node = curNode;
                        list[i] = null;
                        break;
                    }
                    else
                        curPos++;
            }
            return node;
        }


        private static void AppendElements(XElement node, XElement[] list, int listCount)
        {
            for (int i = 0; i < listCount; i++)
            {
                XElement cur = list[i];
                if (cur == null)
                    continue;
                node.Add(new XElement(cur));
            }
        }


        private static void MergeElements(XElement res, XElement parent, XElement child)
        {
            // merge attributes
            AppendAttributes(res, child);

            // check whether the "parent" node has children elements
            if (!HasChildElements(parent))
            {
                // child append children and return
                foreach (XElement cur in child.Elements())
                    res.Add(new XElement(cur));
                return;
            }


            // build child list
            int childListCount = 0;
            XElement[] childList = BuildElementList(child, out childListCount);
            int childListLeft = childListCount;

            // go through all parent elements
            foreach (XElement cur in parent.Elements())
            {
                if (cur.NodeType != XmlNodeType.Element)
                    continue;
                string curName = cur.Name.LocalName;

                // check position,
                // assume that number of elements is not big, so we could use liner search
                int curPos = 0;
                for (XElement prev = (XElement)cur.PreviousNode; prev != null; prev = (XElement)prev.PreviousNode)
                    if (prev.NodeType == XmlNodeType.Element && prev.Name.LocalName == curName)
                        curPos++;

                // search for the element in the child
                XElement childElement = childListCount == 0 ? null : FindElementAndRemoveIt(curName, curPos, childList, childListCount);

                // if not found
                if (childElement == null)
                {
                    res.Add(new XElement(cur));
                    continue;
                }

                childListLeft--;
                XElement temp = new XElement(cur);
                temp.RemoveNodes();
                res.Add(temp);
                XElement resNextLevel = temp;
                MergeElements(resNextLevel, cur, childElement);
            }

            // add nodes missing in the parent
            if (childListLeft > 0)
                AppendElements(res, childList, childListCount);
        }


        private static XElement MergeSections(XElement parent, XElement child)
        {
            // check inheritance rules
            XAttribute attr = child.Attribute("Inheritance");
            string Inheritance = "";
            if (attr != null)
                Inheritance = attr.Value.ToUpperInvariant();

            XElement res = null;
            if (Inheritance == "" || Inheritance == "TRUE")
            {
                res = new XElement(parent);
                res.RemoveNodes();
                MergeElements(res, parent, child);
            }

            else if (Inheritance == "FALSE")
                res = new XElement(child);

            else if (Inheritance == "MERGE")
            {
                res = new XElement(parent);
                foreach (XElement node in child.Elements())
                    res.Add(new XElement(node));
            }

            else
                throw new CXmlDriverException("Section " + NodeName(child) + " has invalid value for attribute @Inheritance. Expected 'true', 'false' or 'merge'");

            return res;
        }


        internal static XElement BuildVirtualNode(XElement parent, XElement child)
        {
            XElement virtualNode = new XElement(child.Name);
            AppendAttributes(virtualNode, parent);
            AppendAttributes(virtualNode, child);

            // build child section list
            int childListCount = 0;
            XElement[] childList = BuildSectionList(child, out childListCount);
            int childListLeft = childListCount;

            // select all top sections but Description, Owner, Variation, TestCase
            foreach (XElement curParentSection in parent.Elements())
            {
                if (curParentSection.NodeType != XmlNodeType.Element ||
                    curParentSection.Name == "Description" || curParentSection.Name == "Owner")
                    continue;

                // ignore any section after <TestCase> or <Variation>
                if (curParentSection.Name == "Variation" || curParentSection.Name == "TestCase")
                    break;

                string curParentSectionName = curParentSection.Name.LocalName;

                // search for the same section in the child
                XElement curChildSection = childListCount == 0 ? null : FindElementAndRemoveIt(curParentSectionName, 0, childList, childListCount);

                // child doesn't have a section with the same, just copy it from the parent
                if (curChildSection == null)
                {
                    virtualNode.Add(new XElement(curParentSection));
                    continue;
                }

                childListLeft--;
                XElement mergedSection = MergeSections(curParentSection, curChildSection);
                virtualNode.Add(mergedSection);
            }

            // add sections missing in the parent
            if (childListLeft > 0)
                AppendElements(virtualNode, childList, childListCount);

            return virtualNode;
        }


        private void AddBuiltInAttributes(XElement node)
        {
            CreateAttributeIfNotExists(node, "Implemented", DEFAULT_VAR_IMPLEMENTED.ToString());
            CreateAttributeIfNotExists(node, "Pri", DEFAULT_VAR_PRIORITY.ToString());
            CreateAttributeIfNotExists(node, "Security", DEFAULT_VAR_SECURITY.ToString());
            CreateAttributeIfNotExists(node, "Skipped", DEFAULT_VAR_SKIPPED.ToString());
            CreateAttributeIfNotExists(node, "Error", DEFAULT_VAR_ERROR.ToString());
            CreateAttributeIfNotExists(node, "Language", DEFAULT_VAR_LANGUAGE.ToString());
        }

        private void ProcessIncludes(string[] parentFilters, string defaultSection, XElement testModuleNode, MyDict<string, object> masterList, IList xmlDriverParams)
        {
            // loop through all includes cases
            foreach (XElement includeNode in testModuleNode.Elements("Include"))
            {
                string includeName = SelectExistingAttributeValue(includeNode, "Name");
                string includeControlFile = SelectExistingValue(includeNode, "ControlFile");
                string includeDefaultSection = SelectValue(includeNode, "DefaultSection", defaultSection);
                if (masterList != null && masterList[includeControlFile] != null)
                    throw new CXmlDriverException("Reference cycle is detected in the control file " + includeControlFile);

                IEnumerable<XElement> includeFilterNodes = includeNode.Elements("Filter");

                // add filters from <INCLUDE> section
                string[] includeFilters = null;
                int count = Enumerable.Count(includeFilterNodes);
                if (count > 0)
                {
                    string[] tmp = new string[count];
                    XElement[] list = Enumerable.ToArray(includeFilterNodes);
                    for (int i = 0; i < count; i++)
                        tmp[i] = list[i].Value;
                    includeFilters = (string[])CombineArrays(parentFilters, tmp);
                }
                else
                    includeFilters = parentFilters;

                MyDict<string, object> newMasterList = masterList == null ? new MyDict<string, object>() : masterList;
                newMasterList[includeControlFile] = true;

                ParseControlFile(includeName, includeControlFile, includeFilters, includeDefaultSection, includeNode, masterList, xmlDriverParams);
            }
        }

        private XDocument LoadControlFile(string controlFile)
        {
            using (StreamReader reader = new StreamReader(FilePathUtil.getStream(controlFile)))
            {
                string buf = reader.ReadToEnd();
                return XDocument.Parse(buf);
            }
        }

        private void ParseControlFileSafe(string testCaseNamePrefix, string controlFile, string[] filters, string defaultSection, XElement topNode, MyDict<string, object> masterList, IList xmlDriverParams)
        {
            try
            {
                _parseError = null;
                ParseControlFile(testCaseNamePrefix, controlFile, filters, defaultSection, topNode, masterList, xmlDriverParams);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        private string SelectDescription(XElement node)
        {
            string description = SelectAttributeValue(node, "Desc", null);
            if (description != null)
                return description;

            // rollback to old schema
            _confirmanceErrors = _confirmanceErrors + NodeName(node.Parent, node) + ": Use @Desc attribute instead of <Desctipion> element.\n";
            return SelectExistingValue(node, "Description");
        }


        private string SelectPriority(XElement node)
        {
            string priority = SelectAttributeValue(node, "Pri", null);
            if (priority != null)
                return priority;

            // check whether it's old schema or not
            priority = SelectAttributeValue(node, "Priority", null);
            if (priority == null)
                return DEFAULT_VAR_PRIORITY.ToString(); // use default priority

            // rollback to old schema
            _confirmanceErrors = _confirmanceErrors + NodeName(node.Parent, node) + ": Use @Pri attribute instead of @Priority.\n";
            return priority;
        }

        private string SelectName(XElement node)
        {
            string name = SelectAttributeValue(node, "Name", null);
            if (name == null)
                return null;

            // rollback to old schema
            _confirmanceErrors = _confirmanceErrors + NodeName(node.Parent, node) + ": Use @Desc attribute instead of @Name.\n";
            return name;
        }

        private void ParseControlFile(string testCaseNamePrefix, string controlFile, string[] filters, string defaultSection, XElement topNode, MyDict<string, object> masterList, IList xmlDriverParams)
        {
            // load the control file
            XDocument doc = LoadControlFile(controlFile);

            // parse test module top node
            XElement testModuleNode = doc.Element("TestModule");
            AddBuiltInAttributes(testModuleNode);
            XElement testModuleVirtualNode = (topNode == null) ? testModuleNode :
                BuildVirtualNode(new XElement(topNode), testModuleNode);
            string testModuleName = SelectExistingAttributeValue(testModuleNode, "Name");
            string testModuleCreated = SelectExistingAttributeValue(testModuleNode, "Created");
            string testModuleModified = SelectExistingAttributeValue(testModuleNode, "Modified");
            string testModuleDescription = SelectDescription(testModuleNode);

            CXmlDriverParamRawNodes testModuleParam = new CXmlDriverParamRawNodes_TestModule(testModuleNode, testModuleVirtualNode, null);
            xmlDriverParams.Add(new CXmlDriverParam(testModuleParam, defaultSection));

            // process includes
            ProcessIncludes(filters, defaultSection, testModuleNode, masterList, xmlDriverParams);

            // filter xPath 
            string filterXPath = CombineXPath(filters);

            // loop through all test cases
            foreach (XElement testCaseNode in testModuleNode.Elements("TestCase"))
            {
                string testCaseName = SelectExistingAttributeValue(testCaseNode, "Name");
                if (testCaseNamePrefix != null && testCaseNamePrefix != "")
                    testCaseName = testCaseNamePrefix + "_" + testCaseName;
                string testCaseDescription = SelectDescription(testCaseNode);

                XElement testCaseVirtualNode = null;
                CXmlDriverParamRawNodes testCaseParam = new CXmlDriverParamRawNodes_TestCase(testCaseNode, testCaseVirtualNode, testModuleParam);

                // create a test case class
                CTestCase testCase = CreateTestCase(testCaseName, testCaseDescription, new CXmlDriverParam(testCaseParam, defaultSection));

                // loop through all variations
                int varCount = 0;
                int actVarCount = 0;
                foreach (XElement varNode in testCaseNode.Elements("Variation"))
                {
                    varCount++;
                    string varId = SelectExistingAttributeValue(varNode, "Id");
                    string varName = SelectName(varNode);
                    string varPri = SelectPriority(varNode);
                    XElement varVirtualNode = null;

                    // check filter
                    CXmlDriverParamRawNodes varParam = new CXmlDriverParamRawNodes_Variation(varNode, varVirtualNode, testCaseParam);
                    if (!CheckFilter(varParam, filterXPath))
                        continue;

                    // create a new variation and add it to the current testCase 
                    actVarCount++;
                    string varDescription = SelectDescription(varNode);
                    CXmlDriverVariation var = new CXmlDriverVariation((CXmlDriverScenario)testCase,
                        varName, varDescription, int.Parse(varId), int.Parse(varPri),
                        new CXmlDriverParam(varParam, defaultSection));
                    testCase.AddVariation(var);
                }

                if (actVarCount == 0 && varCount > 0)
                    // no 'implemented' variations satisfying the filter
                    testCase = new CXmlDriverEmptyTestCase(testCaseName, testCaseDescription,
                        " no variations with @Implemented='True' " +
                        (_requiredLanguage != null && _requiredLanguage.Length != 0 ? " and @Language='" + _requiredLanguage + "'" : "") +
                        (filterXPath == null ? "" : " and satisfying '" + filterXPath + "'"), _testModule);

                // add test case
                _testModule.AddTestCase(testCase);
            }
        }


        // Creates test case class
        private CXmlDriverScenario CreateTestCase(string name, string desc, CXmlDriverParam param)
        {
            CXmlDriverScenario tmp = null;
            try
            {
                //Create this class (call the constructor with no arguments)
                tmp = new CRWFactoryDriverScenario();
                tmp.Name = name;
                tmp.Desc = desc;
                tmp.TestModule = TestModule;
                tmp.XmlDriverParam = param;
                if (_isRequiredCultureInfoNeedToBeSet)
                    tmp.RequiredCultureInfo = _requiredCultureInfo;
            }
            catch (Exception e)
            {
                throw new CXmlDriverException("XmlDriver: CreateIntance failed for TestCase '" + "CRWFactoryDriverScenario"/*type.Name*/ + "' (" + e.ToString() + ")");
            }
            return tmp;
        }


        public CTestModule TestModule
        {
            get { return _testModule; }
        }

        public string ModuleName
        {
            get
            {
                // get name of the first parsed the module
                if (_testModuleParams.Count == 0)
                    return null;
                string moduleName = "";
                if (((CXmlDriverParam)_testModuleParams[0]).RawNodes.TestModule.Attribute("Name") != null)
                {
                    moduleName = ((CXmlDriverParam)_testModuleParams[0]).RawNodes.TestModule.Attribute("Name").Value;
                }
                return moduleName;
            }
        }

        public string ModuleDesc
        {
            get
            {
                // get description of the first parsed the module
                if (_testModuleParams.Count == 0)
                    return null;
                string moduleDesc = "";
                if (((CXmlDriverParam)_testModuleParams[0]).RawNodes.TestModule.Attribute("Description") != null)
                {
                    moduleDesc = ((CXmlDriverParam)_testModuleParams[0]).RawNodes.TestModule.Attribute("Description").Value;
                }
                return moduleDesc;
            }
        }

        public CXmlDriverParam[] TestModuleParams
        {
            get { return ToXmlDriverParamArray(_testModuleParams); }
        }

        public Exception ParseError
        {
            get { return _parseError; }
        }

        public CultureInfo RequiredCultureInfo
        {
            get { return _requiredCultureInfo; }
        }

        public string ConfirmanceErrors
        {
            get { return _confirmanceErrors; }
        }

        internal bool IsRequiredCultureInfoNeedToBeSet
        {
            get { return _isRequiredCultureInfoNeedToBeSet; }
        }
    }
}
