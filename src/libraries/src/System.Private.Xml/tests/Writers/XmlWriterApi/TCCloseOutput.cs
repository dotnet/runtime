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
    public class TCCloseOutput
    {
        [Theory]
        [XmlWriterInlineData(WriterType.UTF8Writer | WriterType.UnicodeWriter, "Stream")]
        [XmlWriterInlineData(WriterType.UTF8Writer | WriterType.UnicodeWriter, "Textwriter")]
        public void CloseOutput_1(XmlWriterUtils utils, string outputType)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            XmlWriter w = null;
            switch (utils.WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }
            Stream writerStream = new MemoryStream();
            switch (outputType)
            {
                case "Stream":
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case "Textwriter":
                    StreamWriter tw = new StreamWriter(writerStream, wSettings.Encoding);
                    w = WriterHelper.Create(tw, wSettings, overrideAsync: true, async: utils.Async);
                    break;
            }

            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            if (writerStream.CanWrite)
            {
                writerStream.Dispose();
                return;
            }
            CError.WriteLine("Error: XmlWriter closed the stream when CloseOutput = false");
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData(WriterType.UTF8Writer | WriterType.UnicodeWriter, true)]
        [XmlWriterInlineData(WriterType.UTF8Writer | WriterType.UnicodeWriter, false)]
        public void CloseOutput_2(XmlWriterUtils utils, bool closeOutput)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            switch (utils.WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }
            wSettings.CloseOutput = closeOutput;

            XmlWriter w = WriterHelper.Create("writer.out", wSettings, overrideAsync: true, async: utils.Async);
            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            // Check if you can open the file in ReadWrite mode
            Stream fs = null;
            try
            {
                fs = FilePathUtil.getStream("writer.out");/*new FileStream("writer.out", FileMode.Open, FileAccess.ReadWrite);*/
            }
            catch (Exception e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                CError.WriteLine("Uri stream is not closed by writer");
                Assert.True(false);
            }
            finally
            {
                fs.Dispose();
            }
            return;
        }

        [Theory]
        [XmlWriterInlineData(WriterType.NoAsync | WriterType.UTF8Writer | WriterType.UnicodeWriter, "Stream")]
        [XmlWriterInlineData(WriterType.NoAsync | WriterType.UTF8Writer | WriterType.UnicodeWriter, "Textwriter")]
        public void CloseOutput_3(XmlWriterUtils utils, string outputType)
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = true;
            XmlWriter w = null;

            switch (utils.WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }

            Stream writerStream = FilePathUtil.getStream("writer.out");
            switch (outputType)
            {
                case "Stream":
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case "Textwriter":
                    StreamWriter tw = new StreamWriter(writerStream, wSettings.Encoding);
                    w = WriterHelper.Create(tw, wSettings, overrideAsync: true, async: utils.Async);
                    break;
            }

            w.WriteStartElement("root");
            w.WriteEndElement();
            w.Dispose();

            if (writerStream.CanWrite)
            {
                writerStream.Dispose();
                Assert.True(false);
            }
        }

        [Theory]
        [XmlWriterInlineData(WriterType.UTF8Writer | WriterType.UnicodeWriter, "Stream")]
        [XmlWriterInlineData(WriterType.UTF8Writer | WriterType.UnicodeWriter, "Textwriter")]
        public void CloseOutput_4(XmlWriterUtils utils, string outputType)
        {
            Stream writerStream = FilePathUtil.getStream("writer.out");
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = true;
            XmlWriter w = null;

            switch (utils.WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }

            switch (outputType)
            {
                case "Stream":
                    w = WriterHelper.Create(writerStream, wSettings, overrideAsync: true, async: utils.Async);
                    break;
                case "Textwriter":
                    StreamWriter tw = new StreamWriter(writerStream, wSettings.Encoding);
                    w = WriterHelper.Create(tw, wSettings, overrideAsync: true, async: utils.Async);
                    break;
            }

            bool bResult = false;
            try
            {
                w.WriteStartDocument();
                w.WriteStartDocument();
            }
            catch (Exception e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                if (writerStream != null && writerStream.CanWrite)
                    bResult = true;
                else
                    bResult = false;
            }
            finally
            {
                writerStream.Dispose();
            }
            Assert.True(bResult);
        }
    }
}
