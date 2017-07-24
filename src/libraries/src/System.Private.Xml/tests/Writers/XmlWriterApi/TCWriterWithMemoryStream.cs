// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Text;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public class TCWriterWithMemoryStream
    {
        public XmlWriter CreateMemWriter(Stream writerStream, XmlWriterSettings settings)
        {
            XmlWriterSettings wSettings = settings.Clone();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = false;
            XmlWriter w = null;

            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.WrappedWriter:
                    XmlWriter ww = WriterHelper.Create(writerStream, wSettings);
                    w = WriterHelper.Create(ww, wSettings);
                    break;
                case WriterType.CharCheckingWriter:
                    XmlWriter cw = WriterHelper.Create(writerStream, wSettings);
                    XmlWriterSettings cws = settings.Clone();
                    cws.CheckCharacters = true;
                    w = WriterHelper.Create(cw, cws);
                    break;
                case WriterType.CustomWriter:
                    w = new CustomWriter(writerStream, wSettings);
                    break;
                case WriterType.UTF8WriterIndent:
                    wSettings.Encoding = Encoding.UTF8;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UnicodeWriterIndent:
                    wSettings.Encoding = Encoding.Unicode;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                default:
                    throw new Exception("Unknown writer type");
            }
            return w;
        }

        //[Variation(Desc = "XmlWellFormedWriter.Close() throws IndexOutOfRangeException")]
        [Fact]
        public void TFS_661130()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter w = CreateMemWriter(ms, ws))
                {
                    w.WriteStartElement("foo");
                    w.WriteString(new String('a', (2048 * 3) - 50));
                    w.WriteCData(String.Empty);
                    w.WriteComment(String.Empty);
                    w.WriteCData(String.Empty);
                    w.WriteComment(String.Empty);
                }
            }
            return;
        }

        //[Variation(Desc = "XmlWellFormedWriter.Close() throws IndexOutOfRangeException")]
        [Fact]
        public void XmlWellFormedWriterCloseThrowsIndexOutOfRangeException()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter w = CreateMemWriter(ms, ws))
                {
                    w.WriteStartElement("foo");
                    w.WriteString(new String('a', (2048 * 3) - 50));
                    w.WriteRaw("");
                    w.WriteCData("");
                    w.WriteComment("");
                    w.WriteCData("");
                    w.WriteRaw("");
                    w.WriteCData("");
                }
            }
            return;
        }

        //[Variation(Desc = "XmlWellFormedWriter.Close() throws IndexOutOfRangeException")]
        [Fact]
        public void TFS_661130b()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter w = CreateMemWriter(ms, ws))
                {
                    w.WriteStartElement("foo");
                    w.WriteString(new String('a', (2048 * 3) - 50));
                    w.WriteRaw("");
                    w.WriteCData("");
                    w.WriteString("");
                    w.WriteRaw("");
                    w.WriteCData("");
                    w.WriteString("");
                    w.WriteRaw("");
                    w.WriteCData("");
                    w.WriteString("");
                }
            }
            return;
        }

        //[Variation(Desc = "860167.IPublisher.PublishPackage crashes due to disposed.MS", Param = "FileStream")]
        [Fact]
        public void TFS_860167()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteElementString("elem", "text");
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            outputXml = reader.ReadToEnd();
                        }
                    }
                }
                CError.WriteLine("actual: " + outputXml);
                CError.Compare(outputXml, "<elem>text</elem>", "wrong xml");
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to disposed.FS")]
        [Fact]
        public void TFS_860167a()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (Stream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteElementString("elem", "text");
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            outputXml = reader.ReadToEnd();
                        }
                    }
                }
                CError.WriteLine("actual: " + outputXml);
                CError.Compare(outputXml, "<elem>text</elem>", "wrong xml");
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to disposed.BS with MS")]
        [Fact]
        public void TFS_860167e()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream bs = ms)
                    {
                        using (XmlWriter w = CreateMemWriter(bs, ws))
                        {
                            w.WriteElementString("elem", "text");
                            w.Flush();
                            bs.Position = 0;
                            using (StreamReader reader = new StreamReader(bs))
                            {
                                outputXml = reader.ReadToEnd();
                            }
                        }
                    }
                }
                CError.WriteLine("actual: " + outputXml);
                CError.Compare(outputXml, "<elem>text</elem>", "wrong xml");
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to disposed.BS with FS")]
        [Fact]
        public void TFS_860167f()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (Stream ms = new MemoryStream())
                {
                    using (Stream bs = ms)
                    {
                        using (XmlWriter w = CreateMemWriter(bs, ws))
                        {
                            w.WriteElementString("elem", "text");
                            w.Flush();
                            bs.Position = 0;
                            using (StreamReader reader = new StreamReader(bs))
                            {
                                outputXml = reader.ReadToEnd();
                            }
                        }
                    }
                }
                CError.WriteLine("actual: " + outputXml);
                CError.Compare(outputXml, "<elem>text</elem>", "wrong xml");
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to dispose.MS.WriteRaw")]
        [Fact]
        public void TFS_860167b()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteStartElement("foo");
                        w.WriteString(new String('a', (2048 * 3) - 50));
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteComment("");
                        w.WriteCData("");
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteEndElement();
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            reader.ReadToEnd();
                        }
                    }
                }
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to dispose.MS.WriteComment")]
        [Fact]
        public void TFS_860167c()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteStartElement("foo");
                        w.WriteString(new String('a', (2048 * 3) - 50));
                        w.WriteCData(String.Empty);
                        w.WriteComment(String.Empty);
                        w.WriteCData(String.Empty);
                        w.WriteComment(String.Empty);
                        w.WriteEndElement();
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            reader.ReadToEnd();
                        }
                    }
                }
                Assert.True(false);
            }
            catch (Exception e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }

        //[Variation(Desc = "IPublisher.PublishPackage crashes due to dispose.MS.WriteCData")]
        [Fact]
        public void TFS_860167d()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(ms, ws))
                    {
                        w.WriteStartElement("foo");
                        w.WriteString(new String('a', (2048 * 3) - 50));
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteString("");
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteString("");
                        w.WriteRaw("");
                        w.WriteCData("");
                        w.WriteString("");
                        w.Flush();
                        ms.Position = 0;
                        using (StreamReader reader = new StreamReader(ms))
                        {
                            reader.ReadToEnd();
                        }
                    }
                }
                Assert.True(false);
            }
            catch (ObjectDisposedException e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }
    }
}
