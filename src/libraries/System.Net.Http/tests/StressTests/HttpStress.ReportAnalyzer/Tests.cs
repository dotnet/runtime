using System.Reflection;
using System.Xml.Linq;

namespace HttpStress.ReportAnalyzer
{
    public class Tests
    {
        public void ReportAggregator_ParseSingle_FailureCountsAreCorrect()
        {
            XDocument xml = XDocument.Parse(TestXml1);
            ReportAggregator aggregator = new ReportAggregator();
            aggregator.AppendReportXml(xml);

            AssertEqual(4, aggregator.FailureTypes.Count());

            FailureType f1 = aggregator.FailureTypes.Single(f => f.Fingerprint == "EBA38C05ED6F0C346F3E716E9546F4DE")!;
            AssertEqual(1, f1.TotalFailureCount);

            FailureType f2 = aggregator.FailureTypes.Single(f => f.Fingerprint == "E5AFD1DA395B3519B6A8E84980B25C2B")!;
            AssertEqual(2, f2.TotalFailureCount);

            FailureType f3 = aggregator.FailureTypes.Single(f => f.Fingerprint == "BC9DB81C31CC4D22AC28A795D246CDBE")!;
            AssertEqual(3, f3.TotalFailureCount);

            FailureType f4 = aggregator.FailureTypes.Single(f => f.Fingerprint == "E80C7EDC6927F77D67E487F71395B1DA")!;
            AssertEqual(4, f4.TotalFailureCount);
        }

        public void ReportAggregator_ParseMultiple_FailureCountsAreCorrect()
        {
            XDocument xml1 = XDocument.Parse(TestXml1);
            XDocument xml2 = XDocument.Parse(TestXml2);
            ReportAggregator aggregator = new ReportAggregator();
            aggregator.AppendReportXml(xml1);
            aggregator.AppendReportXml(xml2);

            FailureType f1 = aggregator.FailureTypes.Single(f => f.Fingerprint == "EBA38C05ED6F0C346F3E716E9546F4DE")!;
            AssertEqual(1, f1.TotalFailureCount);

            FailureType f2 = aggregator.FailureTypes.Single(f => f.Fingerprint == "E5AFD1DA395B3519B6A8E84980B25C2B")!;
            AssertEqual(12, f2.TotalFailureCount);

            FailureType f3 = aggregator.FailureTypes.Single(f => f.Fingerprint == "BC9DB81C31CC4D22AC28A795D246CDBE")!;
            AssertEqual(23, f3.TotalFailureCount);

            FailureType f4 = aggregator.FailureTypes.Single(f => f.Fingerprint == "E80C7EDC6927F77D67E487F71395B1DA")!;
            AssertEqual(4, f4.TotalFailureCount);
        }

        public void ReportAggregator_ParseMultiple_TimestampsAreCorrect()
        {
            XDocument xml1 = XDocument.Parse(TestXml1);
            XDocument xml2 = XDocument.Parse(TestXml2);
            ReportAggregator aggregator = new ReportAggregator();
            aggregator.AppendReportXml(xml1);
            aggregator.AppendReportXml(xml2);

            FailureType f2 = aggregator.FailureTypes.Single(f => f.Fingerprint == "E5AFD1DA395B3519B6A8E84980B25C2B")!;
            (DateTime TimeStamp, int Count)[] failures = f2.Failures.OrderBy(f => f.Timestamp).ToArray();
            AssertEqual(2, failures.Length);

            AssertEqual(2, failures[0].Count);
            AssertEqual(new DateTime(2021, 09, 15, 22, 17, 30), failures[0].TimeStamp);

            AssertEqual(10, failures[1].Count);
            AssertEqual(new DateTime(2021, 09, 16, 10, 11, 12), failures[1].TimeStamp);
        }

        private static void AssertEqual<T>(T expected, T actual)
        {
            if (!expected!.Equals(actual))
            {
                throw new Exception($"Expected: {expected} Actual: {actual}");
            }
        }

        private const string TestXml1 = @"
<StressRunReport HttpVersion=""1.1"" Timestamp=""09/15/2021 22:17:30"" OSDescription=""Microsoft Windows 10.0.19043"">
  <Failure FailureTypeFingerprint = ""EBA38C05ED6F0C346F3E716E9546F4DE"" FailureCount=""1"">
    <FailureText><![CDATA[System.Exception: a
 ---> System.InvalidOperationException: lol1]]></FailureText>
  </Failure>
  <Failure FailureTypeFingerprint = ""E5AFD1DA395B3519B6A8E84980B25C2B"" FailureCount=""2"">
    <FailureText><![CDATA[System.Exception: b
 ---> System.InvalidOperationException: lol2]]></FailureText>
  </Failure>
  <Failure FailureTypeFingerprint = ""BC9DB81C31CC4D22AC28A795D246CDBE"" FailureCount=""3"">
    <FailureText><![CDATA[System.Exception: boo
 ---> System.InvalidOperationException: lol3]]></FailureText>
  </Failure>
  <Failure FailureTypeFingerprint = ""E80C7EDC6927F77D67E487F71395B1DA"" FailureCount=""4"">
    <FailureText><![CDATA[System.Exception: boo
 ---> System.InvalidOperationException: lol4]]></FailureText>
  </Failure>
</StressRunReport>
";

        private const string TestXml2 = @"
<StressRunReport HttpVersion=""1.1"" Timestamp=""09/16/2021 10:11:12"" OSDescription=""Microsoft Windows 10.0.19043"">
  <Failure FailureTypeFingerprint = ""E5AFD1DA395B3519B6A8E84980B25C2B"" FailureCount=""10"">
    <FailureText><![CDATA[System.Exception: b
 ---> System.InvalidOperationException: lol2]]></FailureText>
  </Failure>
  <Failure FailureTypeFingerprint = ""BC9DB81C31CC4D22AC28A795D246CDBE"" FailureCount=""20"">
    <FailureText><![CDATA[System.Exception: boo
 ---> System.InvalidOperationException: lol3]]></FailureText>
  </Failure>
</StressRunReport>
";
    }

    public static class TestRunner
    {
        public static void RunAll()
        {
            Tests test = new Tests();
            MethodInfo[] testMethods = typeof(Tests).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).ToArray();
            int successCount = 0;
            foreach (MethodInfo method in testMethods)
            {
                Console.WriteLine($"{method.Name} ...");
                try
                {
                    method.Invoke(test, Array.Empty<object>());
                    Console.WriteLine("SUCEEDED");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FAILED: " + ex);
                }
            }

            Console.WriteLine($"\n\n{successCount} / {testMethods.Length} tests succeeded");
        }
    }
}
