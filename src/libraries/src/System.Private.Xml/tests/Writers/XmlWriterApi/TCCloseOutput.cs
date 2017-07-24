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
        //[Variation(id=1, Desc="Check that underlying stream is NOT CLOSED when CloseOutput = FALSE and Create(Stream)", Pri=0, Param="Stream")]
        //[Variation(id=2, Desc="Check that underlying stream is NOT CLOSED when CloseOutput = FALSE and Create(TextWriter)", Pri=0, Param="Textwriter")]
        [Fact]
        public void CloseOutput_1()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            XmlWriter w = null;
            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }
            Stream writerStream = new MemoryStream();
            switch (CurVariation.Param.ToString())
            {
                case "Stream":
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case "Textwriter":
                    StreamWriter tw = new StreamWriter(writerStream, wSettings.Encoding);
                    w = WriterHelper.Create(tw, wSettings);
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

        //[Variation(id=3, Desc="Check that underlying stream is CLOSED when CloseOutput = FALSE and Create(Uri)", Pri=0, Param="false")]
        //[Variation(id=4, Desc="Check that underlying stream is CLOSED when CloseOutput = TRUE and Create(Uri)", Pri=0, Param="true")]
        [Fact]
        public void CloseOutput_2()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }
            wSettings.CloseOutput = Boolean.Parse(CurVariation.Param.ToString());

            XmlWriter w = WriterHelper.Create("writer.out", wSettings);
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

        //[Variation(id=5, Desc="Check that underlying stream is CLOSED when CloseOutput = TRUE and Create(Stream)", Pri=0, Param="Stream")]
        //[Variation(id=6, Desc="Check that underlying stream is CLOSED when CloseOutput = TRUE and Create(Textwriter)", Pri=0, Param="Textwriter")]
        [Fact]
        public void CloseOutput_3()
        {
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = true;
            XmlWriter w = null;

            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }
            Stream writerStream = FilePathUtil.getStream("writer.out");
            switch (CurVariation.Param.ToString())
            {
                case "Stream":
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case "Textwriter":
                    StreamWriter tw = new StreamWriter(writerStream, wSettings.Encoding);
                    w = WriterHelper.Create(tw, wSettings);
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
            else
                return;
        }

        //[Variation(id=7, Desc="Writer should not close underlying stream when an exception is thrown before Close (Stream)", Pri=1, Param="Stream")]
        //[Variation(id=8, Desc="Writer should not close underlying stream when an exception is thrown before Close (Textwriter)", Pri=1, Param="Textwriter")]
        [Fact]
        public void CloseOutput_4()
        {
            Stream writerStream = FilePathUtil.getStream("writer.out");
            XmlWriterSettings wSettings = new XmlWriterSettings();
            wSettings.CloseOutput = true;
            XmlWriter w = null;

            switch (WriterType)
            {
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    break;
            }

            switch (CurVariation.Param.ToString())
            {
                case "Stream":
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case "Textwriter":
                    StreamWriter tw = new StreamWriter(writerStream, wSettings.Encoding);
                    w = WriterHelper.Create(tw, wSettings);
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
