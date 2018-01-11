// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
        private List<AllowableDifference> _allowableDifferences = null;

        private XmlDiff _diff;

        public List<AllowableDifference> AllowableDifferences
        {
            get
            {
                return _allowableDifferences;
            }
            set
            {
                _allowableDifferences = value;
            }
        }

        public XmlDiff Diff
        {
            get
            {
                return _diff;
            }
            set
            {
                _diff = value;
            }
        }
        public bool Compare(string source, string target, out string diffNode)
        {
            diffNode = string.Empty;
            StringBuilder stringBuilder = new StringBuilder();
            if (_diff.Compare(source, target))
            {
                return true;
            }
            else
            {
                XmlDocument diffDoc = new XmlDocument();
                diffDoc.LoadXml(_diff.ToXml());
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
