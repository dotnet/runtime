using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HttpStress.ReportAnalyzer
{
    internal class FailureType
    {
        public string Fingerprint { get; }

        public string ErrorText { get; }

        public int Count { get; set; }

        public FailureType(string fingerprint, string errorText)
        {
            Fingerprint = fingerprint;
            ErrorText = errorText;
        }
    }

    internal class ReportAggregator
    {
        private Dictionary<string, FailureType> _failures = new();
        public ReportAggregator()
        {

        }

        public void AppendReportXml(XDocument doc)
        {
            foreach (XElement failureElement in doc.Descendants("Failure"))
            {
                string fingerprint = failureElement.Attribute("FailureTypeFingerprint")!.Value;
                if (!_failures.TryGetValue(fingerprint, out FailureType? failureType))
                {
                    XCData failureTextNode = (XCData)failureElement.Element("FailureText")!.FirstNode!;
                    string errorText = failureTextNode.Value;
                    failureType = new FailureType(fingerprint, errorText);
                }

                failureType.Count++;
            }
        }
    }
}
