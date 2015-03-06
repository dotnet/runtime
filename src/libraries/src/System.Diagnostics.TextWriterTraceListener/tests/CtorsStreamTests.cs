// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Diagnostics;
using Xunit;

namespace System.Diagnostics.TextWriterTraceListenerTests
{
    public class CtorsStreamTests
    {
        private TextWriterTraceListener _textWriter;
        private Stream _stream;
        private string _fileName;

        [Fact]
        public void Test01()
        {
            //[] Stream is null
            _stream = null;
            Assert.Throws<ArgumentNullException>(delegate () { new TextWriterTraceListener(_stream); });

            //[] Stream is closed
            _stream = new MemoryStream();
            _stream.Dispose();
            Assert.Throws<ArgumentException>(delegate () { new TextWriterTraceListener(_stream); });

            //[] Stream is readonly
            _stream = new MemoryStream(new byte[256], false);
            Assert.Throws<ArgumentException>(delegate () { new TextWriterTraceListener(_stream); });
        }

        [Fact]
        public void Test02()
        {
            try
            {
                //Testing ctor(Stream)
                //[] Vanilla - pass a valid stream. Also check via FileName return the expected value
                string msg1;
                _fileName = string.Format("{0}_2.xml", this.GetType().Name);
                CommonUtilities.DeleteFile(_fileName);

                _stream = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.Write);
                _textWriter = new TextWriterTraceListener(_stream);
                msg1 = "HelloWorld";
                _textWriter.WriteLine(msg1);
                _textWriter.Dispose();

                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(TextWriterTraceListener) }, new string[] { _fileName }, msg1));
            }
            finally
            {
                CommonUtilities.DeleteFile(_fileName);
            }
        }

        [Fact]
        public void Test03()
        {
            string msg1, name;
            name = "MyXMLTraceWriter";
            string fileName = string.Format("{0}_3.xml", this.GetType().Name);

            //Testing ctor(Stream, string)
            //Scenario 3: Vanilla - Pass in a valid name and check with the Name property
            try
            {
                CommonUtilities.DeleteFile(fileName);
                _stream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
                _textWriter = new TextWriterTraceListener(_stream, name);
                Assert.True(_textWriter.Name == name);
                msg1 = "HelloWorld";
                _textWriter.WriteLine(msg1);
                _textWriter.Dispose();

                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(TextWriterTraceListener) }, new string[] { fileName }, msg1));
            }
            finally
            {
                CommonUtilities.DeleteFile(fileName);
            }
        }

        [Fact]
        public void Test04()
        {
            string msg1, name;
            name = null;
            _fileName = string.Format("{0}_4.xml", this.GetType().Name);

            //Scenario 4: name is null -its set as the empty string
            try
            {
                CommonUtilities.DeleteFile(_fileName);

                _stream = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.Write);
                _textWriter = new TextWriterTraceListener(_stream, name);
                Assert.True(_textWriter.Name == string.Empty);

                msg1 = "HelloWorld";
                _textWriter.WriteLine(msg1);
                _textWriter.Dispose();

                Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(TextWriterTraceListener) }, new string[] { _fileName }, msg1));
            }
            finally
            {
                CommonUtilities.DeleteFile(_fileName);
            }
        }

        [Fact]
        public void Test05()
        {
            string msg1;
            string[] names = { string.Empty, new string('a', 100000), "hell0<", "><&" };
            string fileName = string.Format("{0}_5.xml", this.GetType().Name);
            //Scenario 6: Other interesting string values - empty, very long, interesting characters (ones that will clash with xml like <, >, etc)
            try
            {
                foreach (string name in names)
                {
                    CommonUtilities.DeleteFile(fileName);
                    _stream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write);
                    _textWriter = new TextWriterTraceListener(_stream, name);

                    Assert.True(_textWriter.Name == name);
                    msg1 = "HelloWorld";
                    _textWriter.WriteLine(msg1);
                    _textWriter.Dispose();

                    Assert.True(CommonUtilities.TestListenerContent(new Type[] { typeof(TextWriterTraceListener) }, new string[] { fileName }, msg1));
                }
            }
            finally
            {
                CommonUtilities.DeleteFile(fileName);
            }
        }
    }
}