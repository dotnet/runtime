// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class AllowableDifference
    {
        public AllowableDifference(string file1Value, string file2Value)
        {
            File1Value = file1Value;
            File2Value = file2Value;
        }

        public string File1Value;
        public string File2Value;
    }

    public class CompareHelper
    {
        public static void AssertEqualWriteOutput(string expected, Action<XmlWriter> writeFunction)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var writer = new XmlTextWriter(stringWriter))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;
                    writer.IndentChar = ' ';

                    writeFunction(writer);
                }

                AssertEqualLongString(expected, stringWriter.ToString());
            }
        }

        public static void AssertEqualLongString(string expected, string actual)
        {
            if (actual != expected)
            {
                string message = "-- Expected -" + Environment.NewLine + expected + Environment.NewLine + "-- Actual -" + Environment.NewLine + actual + Environment.NewLine;
                throw new NotSupportedException(message);
            }
        }

        public List<AllowableDifference> AllowableDifferences { get; set; } = null;

        public XmlDiff Diff { get; set; }
        public bool Compare(string source, string target, out string diffNode)
        {
            diffNode = string.Empty;
            StringBuilder stringBuilder = new StringBuilder();
            if (Diff.Compare(source, target))
            {
                return true;
            }
            else
            {
                XmlDocument diffDoc = new XmlDocument();
                diffDoc.LoadXml(Diff.ToXml());
                XmlNodeList totalFailures = diffDoc.SelectNodes("/Root/Node/Diff[@DiffType]");
                XmlNodeList attrFailures = diffDoc.SelectNodes("/Root/Node/Diff[@DiffType=6]|/Root/Node/Diff[@DiffType=5]|/Root/Node/Diff[@DiffType=1]");

                if (attrFailures.Count == totalFailures.Count)
                {
                    //Check all different Nodes except allowable Nodes
                    bool allFailuresAllowed = true;
                    foreach (XmlNode node in attrFailures)
                    {
                        if (!IsAllowableFailure(node))
                        {
                            allFailuresAllowed = false;
                            stringBuilder.AppendLine(node.InnerText);
                            break;
                        }
                    }
                    diffNode = stringBuilder.ToString();
                    return allFailuresAllowed;
                }
                else
                {
                    //get all different Nodes
                    foreach (XmlNode node in totalFailures)
                    {
                        stringBuilder.AppendLine(node.InnerText);
                    }
                    diffNode = stringBuilder.ToString();
                    return false;
                }
            }
        }

        private bool IsAllowableFailure(XmlNode failedNode)
        {
            //DiffType="1" is <x /> vs. <x></x>, ignore
            XmlAttribute diffType;
            if ((diffType = failedNode.Attributes["DiffType"]) != null)
            {
                if (diffType.Value == "1") return true;
            }
            if (failedNode.ChildNodes.Count < 2) throw new ArgumentException("Unexpected node structure", "failedNode");
            if (AllowableDifferences == null) return false;

            // XmlDiff flags closing tags as different when the open tags were different
            // this flags those since they aren't interesting failures
            if (failedNode.ChildNodes[0].InnerText.StartsWith("</") &&
                failedNode.ChildNodes[1].InnerText.StartsWith("</"))
            {
                return true;
            }

            foreach (AllowableDifference diff in AllowableDifferences)
            {
                if (failedNode.ChildNodes[0].InnerText == diff.File1Value &&
                    failedNode.ChildNodes[1].InnerText == diff.File2Value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
