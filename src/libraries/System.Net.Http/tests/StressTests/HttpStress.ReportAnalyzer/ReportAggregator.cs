using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HttpStress.ReportAnalyzer
{
    public class FailureType
    {
        private List<(DateTime Timestamp, int Count)> _failures = new ();

        public string Fingerprint { get; }

        public string ErrorText { get; }

        public IEnumerable<(DateTime Timestamp, int Count)> Failures => _failures;

        public int TotalFailureCount { get; private set; }

        public FailureType(string fingerprint, string errorText)
        {
            Fingerprint = fingerprint;
            ErrorText = errorText;
        }

        internal void RegisterFailures(DateTime timeStemp, int count)
        {
            _failures.Add((timeStemp, count));
            TotalFailureCount += count;
        }
    }

    public class ReportAggregator
    {
        private Dictionary<string, FailureType> _failureTypes = new();
        public ReportAggregator()
        {

        }

        public IEnumerable<FailureType> FailureTypes => _failureTypes.Values;

        public void AppendReportXml(XDocument doc)
        {
            DateTime failureTimeStamp = DateTime.Parse(doc.Root!.Attribute("Timestamp")!.Value, CultureInfo.InvariantCulture);

            foreach (XElement failureElement in doc.Descendants("Failure"))
            {
                string fingerprint = failureElement.Attribute("FailureTypeFingerprint")!.Value;
                if (!_failureTypes.TryGetValue(fingerprint, out FailureType? failureType))
                {
                    XCData failureTextNode = (XCData)failureElement.Element("FailureText")!.FirstNode!;
                    string errorText = failureTextNode.Value;
                    failureType = new FailureType(fingerprint, errorText);
                    _failureTypes[fingerprint] = failureType;
                }

                
                int failureCount = (int)failureElement.Attribute("FailureCount")!;
                failureType.RegisterFailures(failureTimeStamp, failureCount);
            }
        }
    }
}
