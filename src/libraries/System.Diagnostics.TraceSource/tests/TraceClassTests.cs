// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace System.Diagnostics.TraceSourceTests
{
    internal static class HelperMethods
    {
        public static string GetFileContents(string fileName)
        {
            // Read the contents of the TextFile.
            using (FileStream fs = File.Open(fileName, FileMode.Open))
            {
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, (int)fs.Length);
                return System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            }
        }

        public static bool CheckStrings(string fileName, string expectedString)
        {
            string actualString = GetFileContents(fileName);
            if (expectedString.Equals(actualString))
            {
                return true;
            }

            return false;
        }
    }

    public class TraceClassTests
    {
        [Fact]
        public void TraceTest01()
        {
            Refresh("TraceClassTests_TextWriterTraceListener1");
            try
            {
                using (FileStream fs = File.Create("TraceClassTests_TextWriterTraceListener1"))
                {
                    TextWriterTraceListener textTL = new TextWriterTraceListener(fs);
                    Trace.Listeners.Add(textTL);

                    Trace.WriteLine("Message start.");
                    Trace.IndentSize = 2;
                    Trace.IndentLevel = 2;
                    Trace.Write("This message should be indented.");
                    Trace.TraceError("This error not be indented.");
                    Trace.TraceError("{0}", "This error is indendented");
                    Trace.TraceWarning("This warning is indented");
                    Trace.TraceWarning("{0}", "This warning is also indented");
                    Trace.TraceInformation("This information in indented");
                    Trace.TraceInformation("{0}", "This information is also indented");
                    Trace.IndentSize = 0;
                    Trace.IndentLevel = 0;
                    Trace.WriteLine("Message end.");
                    textTL.Dispose();

                    Assert.True(HelperMethods.CheckStrings("TraceClassTests_TextWriterTraceListener1", String.Format("Message start.\r\n    This message should be indented.{0} Error: 0 : This error not be indented.\r\n    {0} Error: 0 : This error is indendented\r\n    {0} Warning: 0 : This warning is indented\r\n    {0} Warning: 0 : This warning is also indented\r\n    {0} Information: 0 : This information in indented\r\n    {0} Information: 0 : This information is also indented\r\nMessage end.\r\n", "DEFAULT_APPNAME"))); //DEFAULT_APPNAME this a bug which needs to be fixed.
                }
            }
            finally
            {
                Refresh("TraceClassTests_TextWriterTraceListener1");
            }
        }

        [Fact]
        public void TraceTest02()
        {
            //Check the same using Indent() and Unindent() message.
            Refresh("TraceClassTests_TextWriterTraceListener2");
            try
            {
                using (FileStream fs = File.Create("TraceClassTests_TextWriterTraceListener2"))
                {
                    TextWriterTraceListener textTL = new TextWriterTraceListener(fs);
                    Trace.Listeners.Add(textTL);
                    Trace.IndentSize = 2;
                    Trace.WriteLineIf(true, "Message start.");
                    Trace.Indent();
                    Trace.Indent();
                    Trace.WriteIf(true, "This message should be indented.");
                    Trace.WriteIf(false, "This message should be ignored.");
                    Trace.Indent();
                    Trace.WriteLine("This should not be indented.");
                    Trace.WriteLineIf(false, "This message will be ignored");
                    Trace.Fail("This failure is reported", "with a detailed message");
                    Trace.Assert(false);
                    Trace.Assert(false, "This assert is reported");
                    Trace.Assert(true, "This assert is not reported");
                    Trace.Unindent();
                    Trace.Unindent();
                    Trace.Unindent();
                    Trace.WriteLine("Message end.");
                    textTL.Dispose();

                    Assert.True(HelperMethods.CheckStrings("TraceClassTests_TextWriterTraceListener2", "Message start.\r\n    This message should be indented.This should not be indented.\r\n      Fail: This failure is reported with a detailed message\r\n      Fail: \r\n      Fail: This assert is reported\r\nMessage end.\r\n"));
                }
            }
            finally
            {
                Refresh("TraceClassTests_TextWriterTraceListener2");
            }
        }

        private static void Refresh(string fileName)
        {
            Trace.Refresh();
            Trace.Listeners.RemoveAt(0);
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }
}
