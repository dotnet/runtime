// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public class XmlWriterTraceListenerTests : FileCleanupTestBase
    {
        private readonly string _processName;

        public XmlWriterTraceListenerTests()
        {
            using (var process = Process.GetCurrentProcess())
            {
                _processName = process.ProcessName;
            }
        }

        public static IEnumerable<object[]> ConstructorsTestData()
        {
            yield return new object[] { new XmlWriterTraceListener(new MemoryStream()), string.Empty };
            yield return new object[] { new XmlWriterTraceListener(new StreamWriter(new MemoryStream())), string.Empty };
            yield return new object[] { new XmlWriterTraceListener(Path.GetTempFileName()), string.Empty };
            yield return new object[] { new XmlWriterTraceListener(new MemoryStream(), "MemoryStreamListener"), "MemoryStreamListener" };
            yield return new object[] { new XmlWriterTraceListener(new StreamWriter(new MemoryStream()), "StreamWriterListener"), "StreamWriterListener" };
            yield return new object[] { new XmlWriterTraceListener(Path.GetTempFileName(), "FileNameListener"), "FileNameListener" };
        }

        [Theory]
        [MemberData(nameof(ConstructorsTestData))]
        public void SingleArgumentConstructorTest(XmlWriterTraceListener listener, string expectedName)
        {
            Assert.Equal(expectedName, listener.Name);
        }

        [Fact]
        public void ConstructorThrows_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("stream", () => new XmlWriterTraceListener((Stream)null));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => new XmlWriterTraceListener((TextWriter)null));
            AssertExtensions.Throws<ArgumentNullException>("stream", () => new XmlWriterTraceListener((Stream)null, "trace listener name"));
            AssertExtensions.Throws<ArgumentNullException>("writer", () => new XmlWriterTraceListener((TextWriter)null, "trace listener name"));
        }

        [Fact]
        public void Close_NoWriteSuccess()
        {
            string file = GetTestFilePath();
            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.Close();
                listener.Write("Hello");
            }

            Assert.False(File.Exists(file));
        }

        [Fact]
        public void Close_WriteBeforeAndAfter()
        {
            string file = GetTestFilePath();
            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.Write("Hello");
                listener.Close();
                listener.Write("Goodbye");
            }

            string text = File.ReadAllText(file);
            Assert.Contains("Hello", text);
            Assert.DoesNotContain("Goodbye", text);
        }

        [Fact]
        public void Close_AfterXPathNavigatorTraced()
        {
            string xml1 = "<Error xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\">" +
            "<Info>Exception thrown</Info>" +
                   "<Detail>Failed when trying to connect to server</Detail>" +
                   "</Error>";
            XPathNavigator navigator1 = XDocument.Parse(xml1).CreateNavigator();

            string file = GetTestFilePath();
            using (var listener = new XmlWriterTraceListener(file))
            {
                // shouldn't fail.
                listener.TraceData(null, "Trace", TraceEventType.Information, 100, navigator1);
                listener.Close();
            }
        }

        [Fact]
        public void ListenerWithFilter()
        {
            // Ensure we use an arbitrary ID that doesn't match the process ID or thread ID.
            int traceTransferId = 1;
            using (Process p = Process.GetCurrentProcess())
            {
                while (traceTransferId == p.Id || traceTransferId == Environment.CurrentManagedThreadId)
                {
                    traceTransferId++;
                }
            }

            string file = GetTestFilePath();
            Guid guid = Guid.NewGuid();
            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.Filter = new EventTypeFilter(SourceLevels.Error);
                listener.Write("Hello");
                listener.Fail("Goodbye");

                listener.Filter = new EventTypeFilter(SourceLevels.Critical);
                listener.Fail("InexistentFailure");
                listener.TraceEvent(null, "Trace", TraceEventType.Information, 1, "format", null);
                listener.TraceData(null, "Trace", TraceEventType.Critical, 1, "shouldbehere");
                listener.TraceData(null, "Trace", TraceEventType.Error, 1, "shouldnotbehere");
                listener.TraceData(null, "Trace", TraceEventType.Error, 1, "ghost", "not", "here");
                listener.TraceData(null, "Trace", TraceEventType.Critical, 1, "existent", ".net", "code");

                listener.TraceTransfer(null, "Transfer", traceTransferId, "this is a transfer", guid);
            }

            string text = File.ReadAllText(file);
            Assert.Contains("Goodbye", text);
            Assert.DoesNotContain("Hello", text);
            Assert.DoesNotContain("InexistentFailure", text);
            Assert.DoesNotContain("format", text);
            Assert.Contains("shouldbehere", text);
            Assert.DoesNotContain("shouldnotbehere", text);

            Assert.DoesNotContain("<DataItem>ghost</DataItem>", text);
            Assert.DoesNotContain("<DataItem>not</DataItem>", text);
            Assert.DoesNotContain("<DataItem>here</DataItem>", text);
            Assert.Contains("<DataItem>existent</DataItem><DataItem>.net</DataItem><DataItem>code</DataItem>", text);

            // Desktop has a boolean to turn on filtering in TraceTransfer due to a bug.
            // https://referencesource.microsoft.com/#System/compmod/system/diagnostics/XmlWriterTraceListener.cs,26
            Assert.DoesNotContain('"' + traceTransferId.ToString(CultureInfo.InvariantCulture) + '"', text);
            Assert.DoesNotContain("this is a transfer", text);
            Assert.DoesNotContain("Transfer", text);
            Assert.DoesNotContain(guid.ToString("B"), text);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("Normal Message", "Normal Message")]
        [InlineData("<Escaped Message>", "&lt;Escaped Message&gt;")]
        [InlineData("&\"\'", "&amp;\"\'")]
        [InlineData("Hello\n\r", "Hello\n\r")]
        public void WriteTest(string message, string expectedXml)
        {
            string file = GetTestFilePath();

            DateTime date;
            using (var listener = new XmlWriterTraceListener(file))
            {
                // Let's cache the date right before creating the trace so that later on we don't get different values if for some reason teh date changed.
                date = DateTime.Now;
                listener.Write(message);
            }


            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "0", TraceEventType.Information, date, null);

            XmlNode node = document.GetElementsByTagName("ApplicationData")[0];
            Assert.Equal(expectedXml, node.InnerXml);
            Assert.Equal(message, node.InnerText);
        }

        [Theory]
        [InlineData("Fail:", null)]
        [InlineData("Fail:", "the process failed when running")]
        public void FailTest(string message, string detailMessage)
        {
            string file = GetTestFilePath();
            DateTime date;
            using (var listener = new XmlWriterTraceListener(file))
            {
                // Let's cache the date right before creating the trace so that later on we don't get different values if for some reason teh date changed.
                date = DateTime.Now;
                listener.Fail(message, detailMessage);
            }

            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "0", TraceEventType.Error, date, null);

            XmlNode node = document.GetElementsByTagName("ApplicationData")[0];
            string actualMessage = node.InnerText;
            string expectedMessage = detailMessage != null ? $"{message} {detailMessage}" : message;
            Assert.Equal(expectedMessage, actualMessage);
            Assert.Equal(expectedMessage.Length, actualMessage.Length);
        }

        [Theory]
        [InlineData("This is a format without args", null)]
        [InlineData("This is my {0} to {1} a trace with {0} {2} format {3}", new object[] { "test", "try", "", 3 })]
        public void TraceEventFormat(string format, object[] args)
        {
            string file = GetTestFilePath();
            var eventCache = new TraceEventCache();
            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.TraceEvent(eventCache, "Trace", TraceEventType.Resume, 1, format, args);
            }

            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "1", TraceEventType.Resume, eventCache.DateTime, eventCache);

            XmlNode node = document.GetElementsByTagName("ApplicationData")[0];
            string actualMessage = node.InnerText;
            string expectedMessage = args != null ? string.Format(format, args) : format;
            Assert.Equal(expectedMessage, actualMessage);
            Assert.Equal(expectedMessage.Length, actualMessage.Length);
        }

        public static IEnumerable<object[]> TraceData_OneObject_Data()
        {
            yield return new object[] { "My string data", (TraceEventType)(-1) };
            string xml = "<Error xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"> " +
            "<Info>Exception thrown</Info>" +
                   "<Detail>Failed when trying to connect to server</Detail>" +
                   "</Error>";

            XPathNavigator navigator = XDocument.Parse(xml).CreateNavigator();
            yield return new object[] { navigator, TraceEventType.Error };
            yield return new object[] { null, TraceEventType.Critical };

        }

        [Theory]
        [MemberData(nameof(TraceData_OneObject_Data))]
        public void TraceData_OneObject(object data, TraceEventType eventType)
        {
            string file = GetTestFilePath();
            var eventCache = new TraceEventCache();

            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.TraceData(eventCache, "Trace", eventType, 100, data);
            }

            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "100", eventType, eventCache.DateTime, eventCache);

            if (data == null)
            {
                Assert.Equal(0, document.GetElementsByTagName("DataItem").Count);
                return;
            }

            XmlNode node = document.GetElementsByTagName("DataItem")[0];
            if (data is string strData)
            {
                Assert.Equal(strData, node.InnerText);
            }
            else if (data is XPathNavigator navigatorData)
            {
                var doc = new XmlDocument();
                // navigatorData.InnerXml is formatted with \n\r \t and spaces. So we load it to a document to get the plain xml
                doc.LoadXml(navigatorData.InnerXml);
                Assert.Equal(node.InnerXml, doc.OuterXml);
            }

            string expectedString = $"<TraceData xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><DataItem>{node.InnerXml}</DataItem></TraceData>";
            Assert.Equal(expectedString, document.GetElementsByTagName("ApplicationData")[0].InnerXml);
        }

        [Fact]
        public void TraceData_NullDataParams()
        {
            string file = GetTestFilePath();
            var eventCache = new TraceEventCache();

            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.TraceData(eventCache, "Trace", TraceEventType.Information, 100, null);
            }

            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "100", TraceEventType.Information, eventCache.DateTime, eventCache);

            Assert.Equal(0, document.GetElementsByTagName("DataItem").Count);
        }

        [Fact]
        public void TraceData_NullObjectArray()
        {
            string file = GetTestFilePath();
            var eventCache = new TraceEventCache();

            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.TraceData(eventCache, "Trace", TraceEventType.Information, 100, null, null, null);
            }

            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "100", TraceEventType.Information, eventCache.DateTime, eventCache);

            XmlNodeList nodes = document.GetElementsByTagName("DataItem");
            Assert.Equal(3, nodes.Count);

            foreach (XmlNode node in nodes)
            {
                Assert.Equal(string.Empty, node.InnerText);
            }

            Assert.Equal("<TraceData xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\"><DataItem></DataItem><DataItem></DataItem><DataItem></DataItem></TraceData>", document.GetElementsByTagName("ApplicationData")[0].InnerXml);
        }

        [Fact]
        public void TraceData_MultipleXPathNavigators()
        {
            string xml1 = "<Error xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\">" +
            "<Info>Exception thrown</Info>" +
                   "<Detail>Failed when trying to connect to server</Detail>" +
                   "</Error>";
            XPathNavigator navigator1 = XDocument.Parse(xml1).CreateNavigator();

            string xml2 = "<TestTrace xmlns=\"http://schemas.microsoft.com/2004/06/E2ETraceEvent\">" +
            "<TraceInformationTest>This is some information</TraceInformationTest>" +
                   "</TestTrace>";
            XPathNavigator navigator2 = XDocument.Parse(xml2).CreateNavigator();

            string file = GetTestFilePath();
            var eventCache = new TraceEventCache();
            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.TraceData(eventCache, "Trace", TraceEventType.Information, 100, navigator1, navigator2);
            }

            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "100", TraceEventType.Information, eventCache.DateTime, eventCache);

            XmlNodeList nodes = document.GetElementsByTagName("DataItem");
            Assert.Equal(2, nodes.Count);

            Assert.Equal(xml1, nodes[0].InnerXml);
            Assert.Equal(xml2, nodes[1].InnerXml);

        }

        [Fact]
        public void TraceTransferTest()
        {
            string file = GetTestFilePath();
            var eventCache = new TraceEventCache();
            Guid guid = Guid.NewGuid();
            string message = "Transfer Message";
            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.TraceTransfer(eventCache, "Trace", 0, message, guid);
            }

            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "0", TraceEventType.Transfer, eventCache.DateTime, eventCache);

            XmlNode node = document.GetElementsByTagName("Correlation")[0];
            Assert.Equal(guid.ToString("B"), node.Attributes.GetNamedItem("RelatedActivityID").Value);

            Assert.Equal(message, document.GetElementsByTagName("ApplicationData")[0].InnerText);
        }

        [Fact]
        public void TraceEvent_WithLogicalOperationStack()
        {
            string file = GetTestFilePath();
            var eventCache = new TraceEventCache();
            string message = "Event with operation stack";
            Trace.CorrelationManager.StartLogicalOperation(Guid.NewGuid());
            Trace.CorrelationManager.StartLogicalOperation(Guid.NewGuid());

            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.TraceOutputOptions = TraceOptions.LogicalOperationStack;
                listener.TraceEvent(eventCache, "Trace", TraceEventType.Resume, 1, message);
            }

            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "1", TraceEventType.Resume, eventCache.DateTime, eventCache);

            XmlNodeList nodes = document.GetElementsByTagName("LogicalOperation");
            Assert.Equal(2, nodes.Count);

            Stack stack = eventCache.LogicalOperationStack;
            int i = 0;
            foreach (object correlationId in stack)
            {
                Assert.Equal(correlationId.ToString(), nodes[i++].InnerText);
            }

            Trace.CorrelationManager.StopLogicalOperation();

            Assert.StartsWith(message, document.GetElementsByTagName("ApplicationData")[0].InnerText);
            Assert.Contains("LogicalOperationStack", document.GetElementsByTagName("ApplicationData")[0].InnerXml);
            Assert.Equal(eventCache.Timestamp.ToString(CultureInfo.InvariantCulture), document.GetElementsByTagName("Timestamp")[0].InnerText);
        }

        [Fact]
        public void TraceEvent_WithLogicalOperationStack_Empty()
        {
            string file = GetTestFilePath();
            var eventCache = new TraceEventCache();
            string message = "Empty operation stack";

            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.TraceOutputOptions = TraceOptions.LogicalOperationStack;
                listener.TraceEvent(eventCache, "Trace", TraceEventType.Resume, 1, message);
            }

            var document = new XmlDocument();
            document.Load(file);

            Assert.Equal(0, document.GetElementsByTagName("LogicalOperation").Count);
            Assert.Equal(string.Empty, document.GetElementsByTagName("LogicalOperationStack")[0].InnerText);
        }

        [Fact]
        public void TraceEvent_WithStackTrace()
        {
            string file = GetTestFilePath();
            var eventCache = new TraceEventCache();
            string message = "Event with callstack";
            using (var listener = new XmlWriterTraceListener(file))
            {
                listener.TraceOutputOptions = TraceOptions.Callstack;
                listener.TraceEvent(eventCache, "Trace", TraceEventType.Resume, 1, message);
            }

            var document = new XmlDocument();
            document.Load(file);

            ValidateSystemInfo(document, "1", TraceEventType.Resume, eventCache.DateTime, eventCache);

            Assert.Equal(eventCache.Callstack, document.GetElementsByTagName("Callstack")[0].InnerText);
            Assert.StartsWith(message, document.GetElementsByTagName("ApplicationData")[0].InnerText);
            Assert.Contains("Callstack", document.GetElementsByTagName("ApplicationData")[0].InnerXml);
            Assert.Equal(eventCache.Timestamp.ToString(CultureInfo.InvariantCulture), document.GetElementsByTagName("Timestamp")[0].InnerText);
        }

        private void ValidateSystemInfo(XmlDocument document, string eventId, TraceEventType eventType, DateTime date, TraceEventCache eventCache)
        {
            // TraceEventCache uses DateTime.UtcNow, whereas XmlWriterTraceListener uses DateTime.Now.
            date = date.ToLocalTime();

            XmlNode node = document.GetElementsByTagName("EventID")[0];
            Assert.Equal(eventId, node.InnerText);

            node = document.GetElementsByTagName("Type")[0];
            Assert.Equal("3", node.InnerText);

            node = document.GetElementsByTagName("SubType")[0];
            Assert.Equal(eventType.ToString(), node.Attributes.GetNamedItem("Name").Value);
            Assert.Equal("0", node.InnerText);

            node = document.GetElementsByTagName("Level")[0];
            int sev = (int)eventType;
            if (sev > 255)
                sev = 255;
            if (sev < 0)
                sev = 0;
            Assert.Equal(sev.ToString(), node.InnerText);

            node = document.GetElementsByTagName("TimeCreated")[0];
            var nodeDate = DateTime.Parse(node.Attributes.GetNamedItem("SystemTime").Value);
            Assert.InRange(date - nodeDate, TimeSpan.FromHours(-1), TimeSpan.FromHours(1)); // allow some wiggle room in how close the dates need to be

            node = document.GetElementsByTagName("Source")[0];
            Assert.Equal("Trace", node.Attributes.GetNamedItem("Name").Value);

            node = document.GetElementsByTagName("Correlation")[0];
            Guid guid = eventCache != null ? Trace.CorrelationManager.ActivityId : Guid.Empty;
            Assert.Equal(guid.ToString("B"), node.Attributes.GetNamedItem("ActivityID").Value);

            node = document.GetElementsByTagName("Execution")[0];
            Assert.Equal(_processName, node.Attributes.GetNamedItem("ProcessName").Value);
            if (eventCache != null)
            {
                Assert.Equal(((uint)eventCache.ProcessId).ToString(CultureInfo.InvariantCulture), node.Attributes.GetNamedItem("ProcessID").Value);
            }

            node = document.GetElementsByTagName("Computer")[0];
            Assert.Equal(Environment.MachineName, node.InnerText);
        }
    }
}
