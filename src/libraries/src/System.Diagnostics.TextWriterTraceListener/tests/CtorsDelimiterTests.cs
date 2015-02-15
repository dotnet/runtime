// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Diagnostics;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public class CtorsDelimiterTests
    {
        private DelimitedListTraceListener _delimitedListener;

        [Fact]
        public void Test01()
        {
            string msg1, msg2;
            string name;
            string fileName = string.Format("{0}_3.txt", this.GetType().Name);
            CommonUtilities.DeleteFile(fileName);

            try
            {
                FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                name = "AnyName";
                _delimitedListener = new DelimitedListTraceListener(stream, name);
                Assert.True(_delimitedListener.Delimiter == ";");
                Assert.True(_delimitedListener.Name == name);

                msg1 = "Msg1";
                _delimitedListener.WriteLine(msg1);
                _delimitedListener.Delimiter = ",";
                msg2 = "Msg2";
                _delimitedListener.WriteLine(msg2);
                _delimitedListener.Flush();
                _delimitedListener.Dispose();

                name = "";
                _delimitedListener.Name = name;
                Assert.True(_delimitedListener.Name == name);

                Assert.True(_delimitedListener.Delimiter == ",");
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg2));
            }
            finally
            {
                CommonUtilities.DeleteFile(fileName);
            }
        }

        [Fact]
        public void Test02()
        {
            string msg1, msg2;
            string fileName = string.Format("{0}_4.txt", this.GetType().Name);
            CommonUtilities.DeleteFile(fileName);

            try
            {
                FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                StreamWriter writer = new StreamWriter(stream);

                _delimitedListener = new DelimitedListTraceListener(writer);
                Assert.True(_delimitedListener.Delimiter == ";");
                Assert.True(_delimitedListener.Name == "");
                Assert.True(_delimitedListener.Writer == writer);

                msg1 = "Msg1";
                _delimitedListener.WriteLine(msg1);
                _delimitedListener.Delimiter = ",";
                msg2 = "Msg2";
                _delimitedListener.WriteLine(msg2);
                _delimitedListener.Flush();
                _delimitedListener.Dispose();

                Assert.True(_delimitedListener.Delimiter == ",");
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg2));
            }
            finally
            {
                CommonUtilities.DeleteFile(fileName);
            }
        }

        [Fact]
        public void Test03()
        {
            string msg1, msg2;
            string name;
            string fileName = string.Format("{0}_5.txt", this.GetType().Name);
            CommonUtilities.DeleteFile(fileName);

            try
            {
                FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                StreamWriter writer = new StreamWriter(stream);
                name = "AnyName";

                _delimitedListener = new DelimitedListTraceListener(writer, name);
                Assert.True(_delimitedListener.Delimiter == ";");
                Assert.True(_delimitedListener.Name == name);
                Assert.True(_delimitedListener.Writer == writer);

                msg1 = "Msg1";
                _delimitedListener.WriteLine(msg1);
                _delimitedListener.Delimiter = ",";
                msg2 = "Msg2";
                _delimitedListener.WriteLine(msg2);
                _delimitedListener.Flush();
                _delimitedListener.Dispose();

                name = "";
                _delimitedListener.Name = name;
                Assert.True(_delimitedListener.Name == name);

                Assert.True(_delimitedListener.Delimiter == ",");
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg2));
            }
            finally
            {
                CommonUtilities.DeleteFile(fileName);
            }
        }

        [Fact]
        public void Test04()
        {
            string msg1, msg2, msg3;
            string fileName = string.Format("{0}_7.txt", this.GetType().Name);

            try
            {
                CommonUtilities.DeleteFile(fileName);

                FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                _delimitedListener = new DelimitedListTraceListener(stream);
                Assert.True(_delimitedListener.Delimiter == ";");
                Assert.True(_delimitedListener.Name == "");

                msg1 = "Msg1";
                _delimitedListener.WriteLine(msg1);
                _delimitedListener.Delimiter = ",";
                msg2 = "Msg2";
                _delimitedListener.WriteLine(msg2);

                _delimitedListener.Delimiter = ",,,,";
                msg3 = "Msg3";
                _delimitedListener.WriteLine(msg3);
                Assert.True(_delimitedListener.Delimiter == ",,,,");

                _delimitedListener.Flush();
                _delimitedListener.Dispose();

                Assert.True(_delimitedListener.Delimiter == ",,,,", "Error! Delimiter wrong");
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg1));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg2));
                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(DelimitedListTraceListener) }, new string[] { fileName }, msg3));
            }
            finally
            {
                CommonUtilities.DeleteFile(fileName);
            }
        }

        [Fact]
        public void Test05()
        {
            string fileName = string.Format("{0}_8.txt", this.GetType().Name);
            try
            {
                CommonUtilities.DeleteFile(fileName);
                FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                _delimitedListener = new DelimitedListTraceListener(stream);
                Assert.True(_delimitedListener.Delimiter == ";");
                Assert.True(_delimitedListener.Name == "");

                _delimitedListener.Delimiter = "";
                _delimitedListener.Dispose();
            }
            catch (ArgumentException)
            {
                _delimitedListener.Dispose();
            }
            finally
            {
                CommonUtilities.DeleteFile(fileName);
            }
        }
    }
}