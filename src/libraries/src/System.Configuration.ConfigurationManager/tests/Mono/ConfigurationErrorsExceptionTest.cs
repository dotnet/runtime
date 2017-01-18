// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// ConfigurationErrorsExceptionTest.cs
//
// Author:
//	Gert Driesen  <drieseng@users.sourceforge.net>
//
// Copyright (C) 2008 Gert Driesen
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Configuration;
using System.Configuration.Internal;
using System.IO;
using System.Xml;

using Xunit;

namespace MonoTests.System.Configuration
{
    public class ConfigurationErrorsExceptionTest
    {
        [Fact] // ctor ()
        public void Constructor1()
        {
            ConfigurationErrorsException cee = new ConfigurationErrorsException();
            Assert.NotNull(cee.BareMessage);
            Assert.True(cee.BareMessage.IndexOf("'" + typeof(ConfigurationErrorsException).FullName + "'") != -1, "#2:" + cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Null(cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Equal(cee.BareMessage, cee.Message);
        }

        [Fact] // ctor (String)
        public void Constructor2()
        {
            string msg;
            ConfigurationErrorsException cee;

            msg = "MSG";
            cee = new ConfigurationErrorsException(msg);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Null(cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Same(msg, cee.Message);

            msg = null;
            cee = new ConfigurationErrorsException(msg);
            Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Null(cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Equal(cee.BareMessage, cee.Message);
        }

        [Fact] // ctor (String, Exception)
        public void Constructor3()
        {
            string msg;
            Exception inner;
            ConfigurationErrorsException cee;

            msg = "MSG";
            inner = new Exception();
            cee = new ConfigurationErrorsException(msg, inner);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Null(cee.Filename);
            Assert.Same(inner, cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Same(msg, cee.Message);

            msg = null;
            inner = null;
            cee = new ConfigurationErrorsException(msg, inner);
            Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Null(cee.Filename);
            Assert.Same(inner, cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Equal(cee.BareMessage, cee.Message);
        }

        [Fact] // ctor (String, XmlNode)
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        public void Constructor4()
        {
            using (var temp = new TempDirectory())
            {
                string msg;
                XmlNode node;
                ConfigurationErrorsException cee;

                string xmlfile = Path.Combine(temp.Path, "test.xml");
                XmlDocument doc = new XmlDocument();
                doc.AppendChild(doc.CreateElement("test"));
                doc.Save(xmlfile);

                msg = "MSG";
                node = new XmlDocument();
                cee = new ConfigurationErrorsException(msg, node);
                Assert.Same(msg, cee.BareMessage);
                Assert.NotNull(cee.Data);
                Assert.Equal(0, cee.Data.Count);
                Assert.Null(cee.Filename);
                Assert.Null(cee.InnerException);
                Assert.Equal(0, cee.Line);
                Assert.Same(msg, cee.Message);

                doc = new XmlDocument();
                doc.Load(xmlfile);

                msg = "MSG";
                node = doc.DocumentElement;
                cee = new ConfigurationErrorsException(msg, node);
                Assert.Same(msg, cee.BareMessage);
                Assert.NotNull(cee.Data);
                Assert.Equal(0, cee.Data.Count);
                Assert.Null(cee.Filename);
                Assert.Null(cee.InnerException);
                Assert.Equal(0, cee.Line);
                Assert.Same(msg, cee.Message);

                doc = new ConfigXmlDocument();
                doc.Load(xmlfile);

                msg = "MSG";
                node = doc.DocumentElement;
                cee = new ConfigurationErrorsException(msg, node);
                Assert.Same(msg, cee.BareMessage);
                Assert.NotNull(cee.Data);
                Assert.Equal(0, cee.Data.Count);
                Assert.Equal(xmlfile, cee.Filename);
                Assert.Null(cee.InnerException);
                Assert.Equal(1, cee.Line);
                Assert.Equal(msg + " (" + xmlfile + " line 1)", cee.Message);

                msg = null;
                node = null;
                cee = new ConfigurationErrorsException(msg, node);
                Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
                Assert.NotNull(cee.Data);
                Assert.Equal(0, cee.Data.Count);
                Assert.Null(cee.Filename);
                Assert.Null(cee.InnerException);
                Assert.Equal(0, cee.Line);
                Assert.Equal(cee.BareMessage, cee.Message);
            }
        }

        [Fact] // ctor (String, Exception, XmlNode)
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        public void Constructor6()
        {
            using (var temp = new TempDirectory())
            {
                string msg;
                Exception inner;
                XmlNode node;
                ConfigurationErrorsException cee;

                string xmlfile = Path.Combine(temp.Path, "test.xml");
                XmlDocument doc = new XmlDocument();
                doc.AppendChild(doc.CreateElement("test"));
                doc.Save(xmlfile);

                msg = "MSG";
                inner = new Exception();
                node = new XmlDocument();
                cee = new ConfigurationErrorsException(msg, inner, node);
                Assert.Same(msg, cee.BareMessage);
                Assert.NotNull(cee.Data);
                Assert.Equal(0, cee.Data.Count);
                Assert.Null(cee.Filename);
                Assert.Same(inner, cee.InnerException);
                Assert.Equal(0, cee.Line);
                Assert.Same(msg, cee.Message);

                doc = new XmlDocument();
                doc.Load(xmlfile);

                msg = null;
                inner = null;
                node = doc.DocumentElement;
                cee = new ConfigurationErrorsException(msg, inner, node);
                Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
                Assert.NotNull(cee.Data);
                Assert.Equal(0, cee.Data.Count);
                Assert.Null(cee.Filename);
                Assert.Same(inner, cee.InnerException);
                Assert.Equal(0, cee.Line);
                Assert.Equal(cee.BareMessage, cee.Message);

                doc = new ConfigXmlDocument();
                doc.Load(xmlfile);

                msg = null;
                inner = null;
                node = doc.DocumentElement;
                cee = new ConfigurationErrorsException(msg, inner, node);
                Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
                Assert.NotNull(cee.Data);
                Assert.Equal(0, cee.Data.Count);
                Assert.Equal(xmlfile, cee.Filename);
                Assert.Same(inner, cee.InnerException);
                Assert.Equal(1, cee.Line);
                Assert.Equal(cee.BareMessage + " (" + xmlfile + " line 1)", cee.Message);

                msg = null;
                inner = null;
                node = null;
                cee = new ConfigurationErrorsException(msg, inner, node);
                Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
                Assert.NotNull(cee.Data);
                Assert.Equal(0, cee.Data.Count);
                Assert.Null(cee.Filename);
                Assert.Same(inner, cee.InnerException);
                Assert.Equal(0, cee.Line);
                Assert.Equal(cee.BareMessage, cee.Message);
            }
        }

        [Fact] // ctor (String, String, Int32)
        public void Constructor8()
        {
            string msg;
            string filename;
            int line;
            ConfigurationErrorsException cee;

            msg = "MSG";
            filename = "abc.txt";
            line = 7;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(line, cee.Line);
            Assert.Equal("MSG (abc.txt line 7)", cee.Message);

            msg = null;
            filename = null;
            line = 0;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Equal(cee.BareMessage, cee.Message);

            msg = null;
            filename = "abc.txt";
            line = 5;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(5, cee.Line);
            Assert.Equal(cee.BareMessage + " (abc.txt line 5)", cee.Message);

            msg = "MSG";
            filename = null;
            line = 5;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(5, cee.Line);
            Assert.Equal(msg + " (line 5)", cee.Message);

            msg = "MSG";
            filename = "abc.txt";
            line = 0;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Equal(msg + " (abc.txt)", cee.Message);

            msg = null;
            filename = null;
            line = 4;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(4, cee.Line);
            Assert.Equal(cee.BareMessage + " (line 4)", cee.Message);

            msg = string.Empty;
            filename = string.Empty;
            line = 0;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Same(msg, cee.Message);

            msg = string.Empty;
            filename = "abc.txt";
            line = 6;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(6, cee.Line);
            Assert.Equal(msg + " (abc.txt line 6)", cee.Message);

            msg = "MSG";
            filename = string.Empty;
            line = 6;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(6, cee.Line);
            Assert.Equal(msg + " (line 6)", cee.Message);

            msg = string.Empty;
            filename = string.Empty;
            line = 4;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(4, cee.Line);
            Assert.Equal(msg + " (line 4)", cee.Message);

            msg = "MSG";
            filename = string.Empty;
            line = 0;
            cee = new ConfigurationErrorsException(msg, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Null(cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Equal(msg, cee.Message);
        }

        [Fact] // ctor (String, Exception, String, Int32)
        public void Constructor9()
        {
            string msg;
            Exception inner;
            string filename;
            int line;
            ConfigurationErrorsException cee;

            msg = "MSG";
            inner = new Exception();
            filename = "abc.txt";
            line = 7;
            cee = new ConfigurationErrorsException(msg, inner, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Same(inner, cee.InnerException);
            Assert.Equal(line, cee.Line);
            Assert.Equal(msg + " (abc.txt line 7)", cee.Message);

            msg = null;
            inner = null;
            filename = null;
            line = 0;
            cee = new ConfigurationErrorsException(msg, inner, filename, line);
            Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Same(inner, cee.InnerException);
            Assert.Equal(0, cee.Line);
            Assert.Equal(cee.BareMessage, cee.Message);

            msg = null;
            inner = new Exception();
            filename = null;
            line = 7;
            cee = new ConfigurationErrorsException(msg, inner, filename, line);
            Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Same(inner, cee.InnerException);
            Assert.Equal(line, cee.Line);
            Assert.Equal(cee.BareMessage + " (line 7)", cee.Message);

            msg = string.Empty;
            inner = new Exception();
            filename = string.Empty;
            line = 7;
            cee = new ConfigurationErrorsException(msg, inner, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Same(inner, cee.InnerException);
            Assert.Equal(line, cee.Line);
            Assert.Equal(" (line 7)", cee.Message);

            msg = string.Empty;
            inner = new Exception();
            filename = "abc.txt";
            line = 7;
            cee = new ConfigurationErrorsException(msg, inner, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Same(inner, cee.InnerException);
            Assert.Equal(line, cee.Line);
            Assert.Equal(" (abc.txt line 7)", cee.Message);

            msg = "MSG";
            inner = new Exception();
            filename = null;
            line = 7;
            cee = new ConfigurationErrorsException(msg, inner, filename, line);
            Assert.Same(msg, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Same(inner, cee.InnerException);
            Assert.Equal(line, cee.Line);
            Assert.Equal(cee.BareMessage + " (line 7)", cee.Message);

            msg = null;
            inner = new Exception();
            filename = "abc.txt";
            line = 7;
            cee = new ConfigurationErrorsException(msg, inner, filename, line);
            Assert.Equal(new ConfigurationErrorsException().Message, cee.BareMessage);
            Assert.NotNull(cee.Data);
            Assert.Equal(0, cee.Data.Count);
            Assert.Same(filename, cee.Filename);
            Assert.Same(inner, cee.InnerException);
            Assert.Equal(line, cee.Line);
            Assert.Equal(cee.BareMessage + " (abc.txt line 7)", cee.Message);
        }

        [Fact] // GetFilename (XmlReader)
        public void GetFilename1_Reader_Null()
        {
            XmlReader reader = null;
            string filename = ConfigurationErrorsException.GetFilename(reader);
            Assert.Null(filename);
        }

        [Fact] // GetFilename (XmlNode)
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        public void GetFilename2()
        {
            using (var temp = new TempDirectory())
            {
                XmlNode node;
                string filename;

                string xmlfile = Path.Combine(temp.Path, "test.xml");
                XmlDocument doc = new XmlDocument();
                doc.AppendChild(doc.CreateElement("test"));
                doc.Save(xmlfile);

                node = new XmlDocument();
                filename = ConfigurationErrorsException.GetFilename(node);
                Assert.Null(filename);

                doc = new XmlDocument();
                doc.Load(xmlfile);

                node = doc.DocumentElement;
                filename = ConfigurationErrorsException.GetFilename(node);
                Assert.Null(filename);

                doc = new ConfigXmlDocument();
                doc.Load(xmlfile);

                node = doc.DocumentElement;
                filename = ConfigurationErrorsException.GetFilename(node);
                Assert.Equal(xmlfile, filename);
            }
        }

        [Fact] // GetFilename (XmlNode)
        public void GetFilename2_Node_Null()
        {
            XmlNode node = null;
            string filename = ConfigurationErrorsException.GetFilename(node);
            Assert.Null(filename);
        }

        [Fact] // GetLineNumber (XmlReader)
        public void GetLineNumber1()
        {
            using (var temp = new TempDirectory())
            {
                string xmlfile = Path.Combine(temp.Path, "test.xml");
                XmlDocument doc = new XmlDocument();
                doc.AppendChild(doc.CreateElement("test"));
                doc.Save(xmlfile);

                using (XmlReader reader = new XmlTextReader(xmlfile))
                {
                    int line = ConfigurationErrorsException.GetLineNumber(reader);
                    Assert.Equal(0, line);
                }

                using (XmlErrorReader reader = new XmlErrorReader(xmlfile))
                {
                    int line = ConfigurationErrorsException.GetLineNumber(reader);
                    Assert.Equal(666, line);
                }
            }
        }

        [Fact] // GetLineNumber (XmlReader)
        public void GetLineNumber1_Reader_Null()
        {
            XmlReader reader = null;
            int line = ConfigurationErrorsException.GetLineNumber(reader);
            Assert.Equal(0, line);
        }

        [Fact] // GetLineNumber (XmlNode)
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        public void GetLineNumber2()
        {
            using (var temp = new TempDirectory())
            {
                XmlNode node;
                int line;

                string xmlfile = Path.Combine(temp.Path, "test.xml");
                XmlDocument doc = new XmlDocument();
                doc.AppendChild(doc.CreateElement("test"));
                doc.Save(xmlfile);

                node = new XmlDocument();
                line = ConfigurationErrorsException.GetLineNumber(node);
                Assert.Equal(0, line);

                doc = new XmlDocument();
                doc.Load(xmlfile);

                node = doc.DocumentElement;
                line = ConfigurationErrorsException.GetLineNumber(node);
                Assert.Equal(0, line);

                doc = new ConfigXmlDocument();
                doc.Load(xmlfile);

                node = doc.DocumentElement;
                line = ConfigurationErrorsException.GetLineNumber(node);
                Assert.Equal(1, line);
            }
        }

        [Fact] // GetLineNumber (XmlNode)
        public void GetLineNumber2_Node_Null()
        {
            XmlNode node = null;
            int line = ConfigurationErrorsException.GetLineNumber(node);
            Assert.Equal(0, line);
        }

        class XmlErrorReader : XmlTextReader, IConfigErrorInfo
        {
            public XmlErrorReader(string filename) : base(filename)
            {
            }

            string IConfigErrorInfo.Filename
            {
                get { return "FILE"; }
            }

            int IConfigErrorInfo.LineNumber
            {
                get { return 666; }
            }
        }
    }
}

