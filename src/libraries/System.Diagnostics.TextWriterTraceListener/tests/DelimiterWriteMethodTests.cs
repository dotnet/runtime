// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Diagnostics;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public class DelimiterWriteMethodTests
    {
        [Fact]
        public void RunTest()
        {
            DelimitedListTraceListener delimitedWriter;
            string fileName, msg1, msg2, msg3, format, source;
            TraceEventCache eventCache;
            TraceEventType severity;
            int id;
            object[] args, data;
            FileStream stream;
            StreamWriter writer;

            try
            {
                //Scenario 1: TraceEvent (TraceEventCache eventCache, string source, TraceEventType severity, int id, string format, params object[] args)
                fileName = string.Format("{0}_1.xml", this.GetType().Name);
                CommonUtilities.DeleteFile(fileName);
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
                delimitedWriter = new DelimitedListTraceListener(writer);

                eventCache = null;
                source = "Co1971";
                severity = TraceEventType.Information;
                id = 1000;
                format = "This is an object of type string:{0} and this is of type int: {1}";
                args = new Object[] { "Hello", 6 };
                msg1 = string.Format(format, args);
                delimitedWriter.TraceEvent(eventCache, source, severity, id, format, args);
                delimitedWriter.Dispose();

                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, source));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, severity.ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, id.ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));

                CommonUtilities.DeleteFile(fileName);

                //Scenario 2: TraceEvent(TraceEventCache eventCache, string source, TraceEventType severity, int id, string message)

                fileName = string.Format("{0}_2.xml", this.GetType().Name);
                CommonUtilities.DeleteFile(fileName);
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
                delimitedWriter = new DelimitedListTraceListener(writer);

                eventCache = null;
                source = "Co1971";
                severity = TraceEventType.Information;
                id = 1000;
                msg1 = "Hello World";
                delimitedWriter.TraceEvent(eventCache, source, severity, id, msg1);
                delimitedWriter.Dispose();

                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, source));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, severity.ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, id.ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));
                CommonUtilities.DeleteFile(fileName);

                //Scenario 3: TraceData(TraceEventCache eventCache, string source, TraceEventType severity, int id, object data)

                fileName = string.Format("{0}_3.xml", this.GetType().Name);
                CommonUtilities.DeleteFile(fileName);
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
                delimitedWriter = new DelimitedListTraceListener(writer);
                object data1;
                eventCache = null;
                source = "Co1971";
                severity = TraceEventType.Information;
                id = 1000;
                data1 = Decimal.MaxValue;
                delimitedWriter.TraceData(eventCache, source, severity, id, data1);
                delimitedWriter.Dispose();

                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, source));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, severity.ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, id.ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, data1.ToString()));
                CommonUtilities.DeleteFile(fileName);

                //Scenario 4: TraceData(TraceEventCache eventCache, string source, TraceEventType severity, int id, params object[] data)

                fileName = string.Format("{0}_4.xml", this.GetType().Name);
                CommonUtilities.DeleteFile(fileName);
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
                delimitedWriter = new DelimitedListTraceListener(writer);

                eventCache = null;
                source = "Co1971";
                severity = TraceEventType.Information;
                id = 1000;
                data = new Object[] { Decimal.MaxValue, Int64.MinValue, Int32.MaxValue, "Hello" };
                msg1 = string.Format("{0}{1}{2}{3}", data);
                delimitedWriter.TraceData(eventCache, source, severity, id, data);
                delimitedWriter.Dispose();


                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, source));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, severity.ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, id.ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, data[0].ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, data[1].ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, data[2].ToString()));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, data[3].ToString()));
                CommonUtilities.DeleteFile(fileName);

                //Scenario 5: Write(string message)

                fileName = string.Format("{0}_5.xml", this.GetType().Name);
                CommonUtilities.DeleteFile(fileName);
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
                delimitedWriter = new DelimitedListTraceListener(writer);

                msg1 = "Hello World";
                delimitedWriter.Write(msg1);
                delimitedWriter.Dispose();
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));
                CommonUtilities.DeleteFile(fileName);

                //Scenario 6: WriteLine(string message)
                fileName = string.Format("{0}_6.xml", this.GetType().Name);
                CommonUtilities.DeleteFile(fileName);
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
                delimitedWriter = new DelimitedListTraceListener(writer);

                msg1 = "Hello World";
                delimitedWriter.WriteLine(msg1);
                delimitedWriter.Dispose();

                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));
                CommonUtilities.DeleteFile(fileName);

                //Scenario 7: some TraceListener methods that are not overridden
                fileName = string.Format("{0}_7.xml", this.GetType().Name);
                CommonUtilities.DeleteFile(fileName);
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
                delimitedWriter = new DelimitedListTraceListener(writer);
                msg1 = "Msg1";
                msg2 = "Msg2";
                delimitedWriter.Fail(msg1, msg2);
                msg3 = "Hello World";
                delimitedWriter.Fail(msg3);
                delimitedWriter.Dispose();

                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg2));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));
                CommonUtilities.DeleteFile(fileName);

                //Scenario 8: some TraceListener properties that are not overridden
                fileName = string.Format("{0}_8.xml", this.GetType().Name);
                CommonUtilities.DeleteFile(fileName);
                stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                writer = new StreamWriter(stream);
                delimitedWriter = new DelimitedListTraceListener(writer);

                Assert.True(delimitedWriter.Filter == null);
                Assert.True(delimitedWriter.IndentLevel == 0);
                Assert.True(delimitedWriter.IndentSize == 4);
                Assert.True(delimitedWriter.Name == string.Empty);
                Assert.True(delimitedWriter.TraceOutputOptions == TraceOptions.None);
                delimitedWriter.Dispose();
                CommonUtilities.DeleteFile(fileName);
            }
            finally
            {
                for (int i = 1; i <= 8; i++)
                {
                    fileName = string.Format("{0}_{1}.xml", this.GetType().Name, i);
                    CommonUtilities.DeleteFile(fileName);
                }
            }
        }
    }
}