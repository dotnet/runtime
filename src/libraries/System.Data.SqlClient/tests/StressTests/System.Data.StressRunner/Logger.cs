// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace DPStressHarness
{
    public class Logger
    {
        private const string _resultDocumentName = "perfout.xml";

        private XmlDocument _doc;
        private XmlElement _runElem;
        private XmlElement _testElem;

        public Logger(string runLabel, bool isOfficial, string milestone, string branch)
        {
            _doc = GetTestResultDocument();

            _runElem = GetRunElement(_doc, runLabel, DateTime.Now.ToString(), isOfficial, milestone, branch);

            Process currentProcess = Process.GetCurrentProcess();
            AddRunMetric(Constants.RUN_PROCESS_MACHINE_NAME, currentProcess.MachineName);
            AddRunMetric(Constants.RUN_DNS_HOST_NAME, System.Net.Dns.GetHostName());
            AddRunMetric(Constants.RUN_IDENTITY_NAME, System.Security.Principal.WindowsIdentity.GetCurrent().Name);
            AddRunMetric(Constants.RUN_METRIC_PROCESSOR_COUNT, Environment.ProcessorCount.ToString());

        }

        public void AddRunMetric(string metricName, string metricValue)
        {
            Debug.Assert(_runElem != null);

            if (metricValue.Equals(String.Empty))
                return;

            AddRunMetricElement(_runElem, metricName, metricValue);
        }

        public void AddTest(string testName)
        {
            Debug.Assert(_runElem != null);

            _testElem = AddTestElement(_runElem, testName);
        }

        public void AddTestMetric(string metricName, string metricValue, string metricUnits)
        {
            AddTestMetric(metricName, metricValue, metricUnits, null);
        }

        public void AddTestMetric(string metricName, string metricValue, string metricUnits, bool? isHigherBetter)
        {
            Debug.Assert(_runElem != null);
            Debug.Assert(_testElem != null);

            if (metricValue.Equals(String.Empty))
                return;

            AddTestMetricElement(_testElem, metricName, metricValue, metricUnits, isHigherBetter);
        }

        public void AddTestException(string exceptionData)
        {
            Debug.Assert(_runElem != null);
            Debug.Assert(_testElem != null);

            AddTestExceptionElement(_testElem, exceptionData);
        }

        public void Save()
        {
            FileStream resultDocumentStream = new FileStream(_resultDocumentName, FileMode.Create);
            _doc.Save(resultDocumentStream);
            resultDocumentStream.Dispose();
        }

        private static XmlDocument GetTestResultDocument()
        {
            if (File.Exists(_resultDocumentName))
            {
                XmlDocument doc = new XmlDocument();
                FileStream resultDocumentStream = new FileStream(_resultDocumentName, FileMode.Open, FileAccess.Read);
                doc.Load(resultDocumentStream);
                resultDocumentStream.Dispose();
                return doc;
            }
            else
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\" ?><PerfResults></PerfResults>");
                FileStream resultDocumentStream = new FileStream(_resultDocumentName, FileMode.CreateNew);
                doc.Save(resultDocumentStream);
                resultDocumentStream.Dispose();
                return doc;
            }
        }


        private static XmlElement GetRunElement(XmlDocument doc, string label, string startTime, bool isOfficial, string milestone, string branch)
        {
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element &&
                     node.Name.Equals(Constants.XML_ELEM_RUN) &&
                     ((XmlElement)node).GetAttribute(Constants.XML_ATTR_RUN_LABEL).Equals(label))
                {
                    return (XmlElement)node;
                }
            }

            XmlElement runElement = doc.CreateElement(Constants.XML_ELEM_RUN);

            XmlAttribute attrLabel = doc.CreateAttribute(Constants.XML_ATTR_RUN_LABEL);
            attrLabel.Value = label;
            runElement.Attributes.Append(attrLabel);

            XmlAttribute attrStartTime = doc.CreateAttribute(Constants.XML_ATTR_RUN_START_TIME);
            attrStartTime.Value = startTime;
            runElement.Attributes.Append(attrStartTime);

            XmlAttribute attrOfficial = doc.CreateAttribute(Constants.XML_ATTR_RUN_OFFICIAL);
            attrOfficial.Value = isOfficial.ToString();
            runElement.Attributes.Append(attrOfficial);

            if (milestone != null)
            {
                XmlAttribute attrMilestone = doc.CreateAttribute(Constants.XML_ATTR_RUN_MILESTONE);
                attrMilestone.Value = milestone;
                runElement.Attributes.Append(attrMilestone);
            }

            if (branch != null)
            {
                XmlAttribute attrBranch = doc.CreateAttribute(Constants.XML_ATTR_RUN_BRANCH);
                attrBranch.Value = branch;
                runElement.Attributes.Append(attrBranch);
            }

            doc.DocumentElement.AppendChild(runElement);

            return runElement;
        }


        private static void AddRunMetricElement(XmlElement runElement, string name, string value)
        {
            // First check and make sure the metric hasn't already been added.
            // If it has, it's from a previous test in the same run, so just return.
            foreach (XmlNode node in runElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element && node.Name.Equals(Constants.XML_ELEM_RUN_METRIC))
                {
                    if (node.Attributes[Constants.XML_ATTR_RUN_METRIC_NAME].Value.Equals(name))
                        return;
                }
            }

            XmlElement runMetricElement = runElement.OwnerDocument.CreateElement(Constants.XML_ELEM_RUN_METRIC);

            XmlAttribute attrName = runElement.OwnerDocument.CreateAttribute(Constants.XML_ATTR_RUN_METRIC_NAME);
            attrName.Value = name;
            runMetricElement.Attributes.Append(attrName);

            XmlText nodeValue = runElement.OwnerDocument.CreateTextNode(value);
            runMetricElement.AppendChild(nodeValue);

            runElement.AppendChild(runMetricElement);
        }


        private static XmlElement AddTestElement(XmlElement runElement, string name)
        {
            XmlElement testElement = runElement.OwnerDocument.CreateElement(Constants.XML_ELEM_TEST);

            XmlAttribute attrName = runElement.OwnerDocument.CreateAttribute(Constants.XML_ATTR_TEST_NAME);
            attrName.Value = name;
            testElement.Attributes.Append(attrName);

            runElement.AppendChild(testElement);

            return testElement;
        }


        private static void AddTestMetricElement(XmlElement testElement, string name, string value, string units, bool? isHigherBetter)
        {
            XmlElement testMetricElement = testElement.OwnerDocument.CreateElement(Constants.XML_ELEM_TEST_METRIC);

            XmlAttribute attrName = testElement.OwnerDocument.CreateAttribute(Constants.XML_ATTR_TEST_METRIC_NAME);
            attrName.Value = name;
            testMetricElement.Attributes.Append(attrName);

            if (units != null)
            {
                XmlAttribute attrUnits = testElement.OwnerDocument.CreateAttribute(Constants.XML_ATTR_TEST_METRIC_UNITS);
                attrUnits.Value = units;
                testMetricElement.Attributes.Append(attrUnits);
            }

            if (isHigherBetter.HasValue)
            {
                XmlAttribute attrIsHigherBetter = testElement.OwnerDocument.CreateAttribute(Constants.XML_ATTR_TEST_METRIC_ISHIGHERBETTER);
                attrIsHigherBetter.Value = isHigherBetter.ToString();
                testMetricElement.Attributes.Append(attrIsHigherBetter);
            }

            XmlText nodeValue = testElement.OwnerDocument.CreateTextNode(value);
            testMetricElement.AppendChild(nodeValue);

            testElement.AppendChild(testMetricElement);
        }

        private static void AddTestExceptionElement(XmlElement testElement, string exceptionData)
        {
            XmlElement testFailureElement = testElement.OwnerDocument.CreateElement(Constants.XML_ELEM_EXCEPTION);
            XmlText txtNode = testFailureElement.OwnerDocument.CreateTextNode(exceptionData);
            testFailureElement.AppendChild(txtNode);

            testElement.AppendChild(testFailureElement);
        }
    }
}
