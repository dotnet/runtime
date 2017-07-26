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
        public XmlWriter CreateMemWriter(XmlWriterUtils utils, Stream writerStream, XmlWriterSettings settings)
        {
            XmlWriterSettings wSettings = settings.Clone();
            wSettings.CloseOutput = false;
            wSettings.OmitXmlDeclaration = true;
            wSettings.CheckCharacters = false;
            XmlWriter w = null;

            switch (utils.WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.WrappedWriter:
                    XmlWriter ww = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    w = WriterHelper.Create(ww, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.CharCheckingWriter:
                    XmlWriter cw = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    XmlWriterSettings cws = settings.Clone();
                    cws.CheckCharacters = true;
                    w = WriterHelper.Create(cw, cws, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.CustomWriter:
                    wSettings.Async = utils.Async;
                    w = new CustomWriter(writerStream, wSettings);
                    break;
                case WriterType.UTF8WriterIndent:
                    wSettings.Encoding = Encoding.UTF8;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.UnicodeWriterIndent:
                    wSettings.Encoding = Encoding.Unicode;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                default:
                    throw new Exception("Unknown writer type");
            }
            return w;
        }

        [Theory]
        [XmlWriterInlineData]
        public void XmlWellFormedWriterDoesNotThrowIndexOutOfRange0(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter w = CreateMemWriter(utils, ms, ws))
                {
                    w.WriteStartElement("foo");
                    w.WriteString(new String('a', (2048 * 3) - 50));
                    w.WriteCData(String.Empty);
                    w.WriteComment(String.Empty);
                    w.WriteCData(String.Empty);
                    w.WriteComment(String.Empty);
                }
            }
        }

        [Theory]
        [XmlWriterInlineData]
        public void XmlWellFormedWriterDoesNotThrowIndexOutOfRange1(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter w = CreateMemWriter(utils, ms, ws))
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

        [Theory]
        [XmlWriterInlineData]
        public void XmlWellFormedWriterDoesNotThrowIndexOutOfRange2(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlWriter w = CreateMemWriter(utils, ms, ws))
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

        [Theory]
        [XmlWriterInlineData]
        public void DisposedFileStreamDoesNotCauseCrash0(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(utils, ms, ws))
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

        [Theory]
        [XmlWriterInlineData]
        public void DisposedFileStreamDoesNotCauseCrash1(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (Stream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(utils, ms, ws))
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

        [Theory]
        [XmlWriterInlineData]
        public void DisposedFileStreamDoesNotCauseCrash2(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream bs = ms)
                    {
                        using (XmlWriter w = CreateMemWriter(utils, bs, ws))
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

        [Theory]
        [XmlWriterInlineData]
        public void DisposedFileStreamDoesNotCauseCrash3(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                string outputXml;
                using (Stream ms = new MemoryStream())
                {
                    using (Stream bs = ms)
                    {
                        using (XmlWriter w = CreateMemWriter(utils, bs, ws))
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

        [Theory]
        [XmlWriterInlineData]
        public void DisposedFileStreamDoesNotCauseCrash4(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(utils, ms, ws))
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

        [Theory]
        [XmlWriterInlineData]
        public void DisposedFileStreamDoesNotCauseCrash5(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(utils, ms, ws))
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

        [Theory]
        [XmlWriterInlineData(WriterType.All & ~WriterType.Async)]
        public void DisposedFileStreamDoesNotCauseCrash6(XmlWriterUtils utils)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlWriter w = CreateMemWriter(utils, ms, ws))
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

                Assert.True(false, "Exception was not thrown");
            }
            catch (ObjectDisposedException e)
            {
                CError.WriteLine("Exception: " + e);
                return;
            }
        }
    }
}
