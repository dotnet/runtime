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
    public class TCFlushClose
    {
        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom, true)]
        [XmlWriterInlineData(WriterType.AllButCustom, false)]
        public void flush_1(XmlWriterUtils utils, bool closeOutput)
        {
            Stream writerStream = new MemoryStream();
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.NewLineChars = "\r\n";
            XmlWriter w = null;
            long expectedLength = 0;
            wSettings.CloseOutput = closeOutput;

            switch (utils.WriterType)
            {
                case WriterType.WrappedWriter:
                    expectedLength = 113;
                    XmlWriter ww = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    w = WriterHelper.Create(ww, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.CharCheckingWriter:
                    expectedLength = 113;
                    XmlWriter w1 = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    XmlWriterSettings ws1 = new XmlWriterSettings();
                    ws1.CheckCharacters = true;
                    w = WriterHelper.Create(w1, ws1, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    expectedLength = 113;
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    expectedLength = 224;
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.UTF8WriterIndent:
                    wSettings.Encoding = Encoding.UTF8;
                    expectedLength = 125;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case WriterType.UnicodeWriterIndent:
                    wSettings.Encoding = Encoding.Unicode;
                    expectedLength = 248;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                default:
                    Assert.True(false, "unknown writer");
                    break;
            }

            wSettings.CloseOutput = closeOutput;

            var beginning = writerStream.Length;

            try
            {
                w.WriteStartElement("root");
                w.WriteStartElement("OneChar");
                w.WriteAttributeString("a", "myAttribute");
                w.WriteString("a");
                w.WriteEndElement();

                w.WriteStartElement("twoChars");
                w.WriteString("ab");
                w.WriteEndElement();
                w.WriteEndElement();

                Assert.Equal(writerStream.Length, beginning);

                w.Flush();

                Assert.Equal(expectedLength, writerStream.Length);
            }
            finally
            {
                w.Dispose();
                writerStream.Dispose();
            }
        }

        [Theory]
        [XmlWriterInlineData(WriterType.NoAsync | WriterType.UTF8Writer | WriterType.UnicodeWriter)]
        public void close_1(XmlWriterUtils utils)
        {
            Stream writerStream = new MemoryStream();
            XmlWriterSettings wSettings = new XmlWriterSettings();

            long expectedLength1 = 0;
            long expectedLength2 = 0;

            switch (utils.WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    expectedLength1 = 83;
                    expectedLength2 = 113;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    expectedLength1 = 164;
                    expectedLength2 = 224;
                    break;
            }

            wSettings.CloseOutput = false;

            XmlWriter w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);

            try
            {
                var beginning = writerStream.Length;

                w.WriteStartElement("root");
                w.WriteStartElement("OneChar");
                w.WriteAttributeString("a", "myAttribute");
                w.WriteString("a");
                w.WriteEndElement();

                CError.WriteLine("File Size Before Flush: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, beginning, "Before Flush");

                // Flush mid-way
                w.Flush();
                CError.WriteLine("File Size After Flush: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, expectedLength1, "After Flush");

                w.WriteStartElement("twoChars");
                w.WriteString("ab");
                w.WriteEndElement();
                w.WriteEndElement();
                w.Dispose();

                // Now check that Close() called Flush()
                CError.WriteLine("File Size After Writer.Close: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, expectedLength2, "After Writer.Close");

                // Finally, close the underlying stream, it should be flushed now
                writerStream.Flush();
                CError.WriteLine("File Size After Stream.Close: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, expectedLength2, "After Stream.Close");
            }
            catch (XmlException)
            {
                Assert.True(false);
            }
            finally
            {
                if (writerStream != null)
                    writerStream.Dispose();
                if (w != null)
                    w.Dispose();
            }

            return;
        }

        [Theory]
        [XmlWriterInlineData(WriterType.AllButCustom)]
        public void close_2(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.Dispose();
            CError.Equals(w.Settings.Indent, utils.IsIndent(), "Incorrect default value of Indent");
            CError.Equals(w.Settings.CheckCharacters, true, "Incorrect default value of CheckCharacters");
        }
    }
}
