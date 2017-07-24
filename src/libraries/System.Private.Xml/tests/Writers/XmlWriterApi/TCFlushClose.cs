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
        //[Variation(id=1, Desc="Verify Flush() flushes underlying stream when CloseOutput = true", Pri=1,Param="true")]
        //[Variation(id=2, Desc="Verify Flush() flushes underlying stream when CloseOutput = false", Pri=1,Param="false")]
        [Fact]
        public void flush_1()
        {
            Stream writerStream = new MemoryStream();
            XmlWriterSettings wSettings = new XmlWriterSettings();
            XmlWriter w = null;
            long expectedLength = 0;
            if (this.CurVariation.Param.ToString() == "true")
                wSettings.CloseOutput = true;
            else
                wSettings.CloseOutput = false;

            switch (WriterType)
            {
                case WriterType.WrappedWriter:
                    expectedLength = 113;
                    XmlWriter ww = WriterHelper.Create(writerStream, wSettings);
                    w = WriterHelper.Create(ww, wSettings);
                    break;
                case WriterType.CharCheckingWriter:
                    expectedLength = 113;
                    XmlWriter w1 = WriterHelper.Create(writerStream, wSettings);
                    XmlWriterSettings ws1 = new XmlWriterSettings();
                    ws1.CheckCharacters = true;
                    w = WriterHelper.Create(w1, ws1);
                    break;
                case WriterType.UTF8Writer:
                    wSettings.Encoding = Encoding.UTF8;
                    expectedLength = 113;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UnicodeWriter:
                    wSettings.Encoding = Encoding.Unicode;
                    expectedLength = 224;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UTF8WriterIndent:
                    wSettings.Encoding = Encoding.UTF8;
                    expectedLength = 125;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
                case WriterType.UnicodeWriterIndent:
                    wSettings.Encoding = Encoding.Unicode;
                    expectedLength = 248;
                    wSettings.Indent = true;
                    w = WriterHelper.Create(writerStream, wSettings);
                    break;
            }
            if (CurVariation.Param.ToString() == "true")
                wSettings.CloseOutput = true;
            else
                wSettings.CloseOutput = false;

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

                CError.WriteLine("File Size Before Flush: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, beginning, "Before Flush");

                w.Flush();

                CError.WriteLine("File Size After Flush: {0}", writerStream.Length);
                CError.Compare(writerStream.Length, expectedLength, "After Flush");
            }
            catch (Exception)
            {
                Assert.True(false);
            }
            finally
            {
                w.Dispose();
                writerStream.Dispose();
            }
            return;
        }

        //[Variation(id=3, Desc="Verify Close() flushes underlying stream when CloseOutput = true", Pri=1,Param="true")]
        //[Variation(id=4, Desc="Verify Close() flushes underlying stream when CloseOutput = false", Pri=1,Param="false")]
        [Fact]
        public void close_1()
        {
            if (WriterType != WriterType.UTF8Writer && WriterType != WriterType.UnicodeWriter)
                return;

            Stream writerStream = new MemoryStream();
            XmlWriterSettings wSettings = new XmlWriterSettings();

            long expectedLength1 = 0;
            long expectedLength2 = 0;

            switch (WriterType)
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

            if (CurVariation.Param.ToString() == "true")
                wSettings.CloseOutput = true;
            else
                wSettings.CloseOutput = false;

            XmlWriter w = WriterHelper.Create(writerStream, wSettings);

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

        //[Variation(id=5, Desc="Verify WriterSettings after Close()", Pri=1)]
        [Fact]
        public void close_2()
        {
            XmlWriter w = CreateWriter();
            w.Dispose();
            CError.Equals(w.Settings.Indent, IsIndent() ? true : false, "Incorrect default value of Indent");
            CError.Equals(w.Settings.CheckCharacters, true, "Incorrect default value of CheckCharacters");
            return;
        }
    }
}
