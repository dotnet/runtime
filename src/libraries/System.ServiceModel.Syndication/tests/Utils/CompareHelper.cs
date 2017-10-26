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
        public bool Compare(string source, string target)
        {
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
                    bool allFailuresAllowed = true;
                    foreach (XmlNode node in attrFailures)
                    {
                        if (!IsAllowableFailure(node))
                        {
                            allFailuresAllowed = false;
                            break;
                        }
                    }
                    return allFailuresAllowed;
                }
                else
                {
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
