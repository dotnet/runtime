// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Linq;
using System.Text;
using XmlCoreTest.Common;
using Xunit;

namespace System.Xml.Tests
{
    public partial class TCErrorConditionWriter// : XmlWriterTestCaseBase
    {
        public static string file = "writerErr.out";

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        [XmlWriterInlineData(7)]
        [XmlWriterInlineData(8)]
        public void var_01(XmlWriterUtils utils, int param)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                switch (param)
                {
                    case 1: XmlWriter w1 = WriterHelper.Create((Stream)null, overrideAsync: true, async: utils.Async); break;
                    case 2: XmlWriter w2 = WriterHelper.Create((TextWriter)null, overrideAsync: true, async: utils.Async); break;
                    case 3: XmlWriter w3 = WriterHelper.Create((StringBuilder)null, overrideAsync: true, async: utils.Async); break;
                    case 4: XmlWriter w4 = WriterHelper.Create((XmlWriter)null, overrideAsync: true, async: utils.Async); break;
                    case 5: XmlWriter w5 = WriterHelper.Create((Stream)null, ws, overrideAsync: true, async: utils.Async); break;
                    case 6: XmlWriter w6 = WriterHelper.Create((TextWriter)null, ws, overrideAsync: true, async: utils.Async); break;
                    case 7: XmlWriter w7 = WriterHelper.Create((StringBuilder)null, ws, overrideAsync: true, async: utils.Async); break;
                    case 8: XmlWriter w8 = WriterHelper.Create((XmlWriter)null, ws, overrideAsync: true, async: utils.Async); break;
                }
            }
            catch (ArgumentNullException) { return; }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData(true)]
        [XmlWriterInlineData(false)]
        public void var_02(XmlWriterUtils utils, bool param)
        {
            bool result = false;

            XmlWriter w = utils.CreateWriter();
            try
            {
                w.WriteAttributes(null, param);
            }
            catch (ArgumentNullException)
            {
                try
                {
                    w.WriteAttributes(null, param);
                }
                catch (ArgumentNullException) { result = true; }
            }
            finally
            {
                w.Dispose();
            }
            Assert.True(result);
        }

        [Theory]
        [XmlWriterInlineData(true)]
        [XmlWriterInlineData(false)]
        public void var_03(XmlWriterUtils utils, bool param)
        {
            bool result = false;

            XmlWriter w = utils.CreateWriter();
            try
            {
                w.WriteNode((XmlReader)null, param);
            }
            catch (ArgumentNullException)
            {
                try
                {
                    w.WriteNode((XmlReader)null, param);
                }
                catch (ArgumentNullException) { result = true; }
            }
            finally
            {
                w.Dispose();
            }
            Assert.True((result));
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        [XmlWriterInlineData(7)]
        [XmlWriterInlineData(8)]
        [XmlWriterInlineData(9)]
        [XmlWriterInlineData(10)]
        [XmlWriterInlineData(11)]
        [XmlWriterInlineData(12)]
        [XmlWriterInlineData(13)]
        [XmlWriterInlineData(14)]
        [XmlWriterInlineData(15)]
        [XmlWriterInlineData(16)]
        [XmlWriterInlineData(17)]
        [XmlWriterInlineData(18)]
        [XmlWriterInlineData(19)]
        [XmlWriterInlineData(20)]
        public void var_04(XmlWriterUtils utils, int param)
        {
            bool result = false;

            XmlWriter w = utils.CreateWriter();
            try
            {
                switch (param)
                {
                    case 1: w.WriteAttributeString(null, null); break;
                    case 2: w.WriteAttributeString(null, null, null); break;
                    case 3: w.WriteAttributeString("a", null, null, null); break;
                    case 4: w.WriteAttributeString(null, null, "a", null); break;
                    case 5: w.WriteDocType(null, null, null, null); break;
                    case 6: w.WriteElementString(null, null); break;
                    case 7: w.WriteElementString(null, null, null); break;
                    case 8: w.WriteElementString("a", null, null, null); break;
                    case 9: w.WriteElementString("a", null, "a", null); break;
                    case 10: w.WriteEntityRef(null); break;
                    case 11: w.WriteName(null); break;
                    case 12: w.WriteNmToken(null); break;
                    case 13: w.WriteProcessingInstruction(null, null); break;
                    case 14: w.WriteQualifiedName(null, null); break;
                    case 15: w.WriteStartAttribute(null); break;
                    case 16: w.WriteStartAttribute(null, null); break;
                    case 17: w.WriteStartAttribute("a", null, null); break;
                    case 18: w.WriteStartElement(null); break;
                    case 19: w.WriteStartElement(null, null); break;
                    case 20: w.WriteStartElement("a", null, null); break;
                }
            }
            catch (ArgumentException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: w.WriteAttributeString(null, null); break;
                        case 2: w.WriteAttributeString(null, null, null); break;
                        case 3: w.WriteAttributeString("a", null, null, null); break;
                        case 4: w.WriteAttributeString(null, null, "a", null); break;
                        case 5: w.WriteDocType(null, null, null, null); break;
                        case 6: w.WriteElementString(null, null); break;
                        case 7: w.WriteElementString(null, null, null); break;
                        case 8: w.WriteElementString("a", null, null, null); break;
                        case 9: w.WriteElementString("a", null, "a", null); break;
                        case 10: w.WriteEntityRef(null); break;
                        case 11: w.WriteName(null); break;
                        case 12: w.WriteNmToken(null); break;
                        case 13: w.WriteProcessingInstruction(null, null); break;
                        case 14: w.WriteQualifiedName(null, null); break;
                        case 15: w.WriteStartAttribute(null); break;
                        case 16: w.WriteStartAttribute(null, null); break;
                        case 17: w.WriteStartAttribute("a", null, null); break;
                        case 18: w.WriteStartElement(null); break;
                        case 19: w.WriteStartElement(null, null); break;
                        case 20: w.WriteStartElement("a", null, null); break;
                    }
                }
                catch (ArgumentException) { result = true; }
            }
            catch (NullReferenceException)
            {
                try
                {
                    switch (param)
                    {
                        case 5: w.WriteDocType(null, null, null, null); break;
                        case 10: w.WriteEntityRef(null); break;
                        case 13: w.WriteProcessingInstruction(null, null); break;
                        case 14: w.WriteQualifiedName(null, null); break;
                    }
                }
                catch (NullReferenceException)
                {
                    result = (utils.WriterType == WriterType.CharCheckingWriter);
                }
            }
            finally
            {
                w.Dispose();
            }
            Assert.True((result || param == 14 && utils.WriterType == WriterType.CustomWriter));
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        public void var_05(XmlWriterUtils utils, int param)
        {
            bool result = false;

            XmlWriter w = utils.CreateWriter();
            w.WriteStartElement("Root");
            try
            {
                switch (param)
                {
                    case 1: w.WriteBinHex(null, 0, 0); break;
                    case 2: w.WriteBase64(null, 0, 0); break;
                    case 3: w.WriteChars(null, 0, 0); break;
                    case 4: w.LookupPrefix(null); break;
                    case 5: w.WriteRaw(null, 0, 0); break;
                    case 6: w.WriteValue((object)null); break;
                }
            }
            catch (ArgumentNullException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: w.WriteBinHex(null, 0, 0); break;
                        case 2: w.WriteBase64(null, 0, 0); break;
                        case 3: w.WriteChars(null, 0, 0); break;
                        case 4: w.LookupPrefix(null); break;
                        case 5: w.WriteRaw(null, 0, 0); break;
                        case 6: w.WriteValue((object)null); break;
                    }
                }
                catch (ArgumentNullException) { result = true; }
                catch (InvalidOperationException) { result = true; }
            }
            finally
            {
                w.Dispose();
            }
            Assert.True((result));
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        [XmlWriterInlineData(7)]
        [XmlWriterInlineData(8)]
        [XmlWriterInlineData(9)]
        [XmlWriterInlineData(10)]
        [XmlWriterInlineData(11)]
        [XmlWriterInlineData(12)]
        [XmlWriterInlineData(13)]
        [XmlWriterInlineData(14)]
        [XmlWriterInlineData(15)]
        [XmlWriterInlineData(16)]
        [XmlWriterInlineData(17)]
        [XmlWriterInlineData(18)]
        [XmlWriterInlineData(19)]
        [XmlWriterInlineData(20)]
        [XmlWriterInlineData(21)]
        [XmlWriterInlineData(22)]
        [XmlWriterInlineData(23)]
        [XmlWriterInlineData(24)]
        [XmlWriterInlineData(25)]
        [XmlWriterInlineData(26)]
        [XmlWriterInlineData(27)]
        [XmlWriterInlineData(28)]
        [XmlWriterInlineData(29)]
        [XmlWriterInlineData(30)]
        [XmlWriterInlineData(31)]
        [XmlWriterInlineData(32)]
        [XmlWriterInlineData(33)]
        [XmlWriterInlineData(34)]
        [XmlWriterInlineData(35)]
        [XmlWriterInlineData(36)]
        public void var_07(XmlWriterUtils utils, int param)
        {
            bool result = false;
            int[] skipParams = new int[] { 14, 20, 21 };

            XmlWriter w = utils.CreateWriter();
            if (param != 30 && param != 31 && param != 32)
                w.WriteStartElement("Root");
            switch (param)
            {
                case 1: w.WriteComment(null); break;
                case 3: w.WriteCData(null); break;
                case 5: w.WriteRaw(null); break;
                case 6: w.WriteString(null); break;
                case 8: w.WriteValue((string)null); break;
                case 9: w.WriteWhitespace(null); break;
            }
            try
            {
                switch (param)
                {
                    case 1: w.WriteComment("\ud800\ud800"); break;
                    case 2: w.WriteCharEntity('\ud800'); break;
                    case 3: w.WriteCData("\ud800\ud800"); break;
                    case 4: w.WriteEntityRef("\ud800\ud800"); break;
                    case 5: w.WriteRaw("\ud800\ud800"); break;
                    case 6: w.WriteString("\ud800\ud800"); break;
                    case 7: w.WriteSurrogateCharEntity('\ud800', '\ud800'); break;
                    case 8: w.WriteValue("\ud800\ud800"); break;
                    case 9: w.WriteWhitespace("\ud800\ud800"); break;
                    case 10: w.WriteAttributeString("\ud800\ud800", "\ud800\ud800"); break;
                    case 11: w.WriteAttributeString("a0", "\ud800\ud800", "\ud800\ud800"); break;
                    case 12: w.WriteAttributeString("a1", "b1", "\ud800\ud800", "\ud800\ud800"); break;
                    case 13: w.WriteAttributeString("a2", "b2", "c2", "\ud800\ud800"); break;
                    case 14: w.WriteDocType("\ud800\ud800", "\ud800\ud800", "\ud800\ud800", "\ud800\ud800"); break;
                    case 15: w.WriteElementString("\ud800\ud800", "\ud800\ud800"); break;
                    case 16: w.WriteElementString("a", "\ud800\ud800", "\ud800\ud800"); break;
                    case 17: w.WriteElementString("a", "a", "\ud800\ud800", "\ud800\ud800"); break;
                    case 18: w.WriteElementString("a", "a", "a", "\ud800\ud800"); break;
                    case 19: w.WriteEntityRef("\ud800\ud800"); break;
                    case 20: w.WriteName("\ud800\ud800"); break;
                    case 21: w.WriteNmToken("\ud800\ud800"); break;
                    case 22: w.WriteProcessingInstruction("\ud800\ud800", "\ud800\ud800"); break;
                    case 23: w.WriteQualifiedName("\ud800\ud800", "\ud800\ud800"); break;
                    case 24: w.WriteStartAttribute("\ud800\ud800"); break;
                    case 25: w.WriteStartAttribute("\ud800\ud800", "\ud800\ud800"); break;
                    case 26: w.WriteStartAttribute("a3", "\ud800\ud800", "\ud800\ud800"); break;
                    case 27: w.WriteStartElement("\ud800\ud800"); break;
                    case 28: w.WriteStartElement("\ud800\ud800", "\ud800\ud800"); break;
                    case 29: w.WriteStartElement("a", "\ud800\ud800", "\ud800\ud800"); break;
                    case 30: w.WriteDocType("a", "\ud800\ud800", "\ud800\ud800", "\ud800\ud800"); break;
                    case 31: w.WriteDocType("a", "b", "\ud800\ud800", "\ud800\ud800"); break;
                    case 32: w.WriteDocType("a", "b", "c", "\ud800\ud800"); break;
                    case 33: w.WriteAttributeString("a4", "\ud800\ud800"); break;
                    case 34: w.WriteElementString("a", "\ud800\ud800"); break;
                    case 35: w.WriteProcessingInstruction("a", "\ud800\ud800"); break;
                    case 36: w.WriteQualifiedName("a", "\ud800\ud800"); break;
                }
            }
            catch (ArgumentException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: w.WriteComment("\ud800\ud800"); break;
                        case 2: w.WriteCharEntity('\ud800'); break;
                        case 3: w.WriteCData("\ud800\ud800"); break;
                        case 4: w.WriteEntityRef("\ud800\ud800"); break;
                        case 5: w.WriteRaw("\ud800\ud800"); break;
                        case 6: w.WriteString("\ud800\ud800"); break;
                        case 7: w.WriteSurrogateCharEntity('\ud800', '\ud800'); break;
                        case 8: w.WriteValue("\ud800\ud800"); break;
                        case 9: w.WriteWhitespace("\ud800\ud800"); break;
                        case 10: w.WriteAttributeString("\ud800\ud800", "\ud800\ud800"); break;
                        case 11: w.WriteAttributeString("a", "\ud800\ud800", "\ud800\ud800"); break;
                        case 12: w.WriteAttributeString("a", "b", "\ud800\ud800", "\ud800\ud800"); break;
                        case 13: w.WriteAttributeString("a", "b", "c", "\ud800\ud800"); break;
                        case 15: w.WriteElementString("\ud800\ud800", "\ud800\ud800"); break;
                        case 16: w.WriteElementString("a", "\ud800\ud800", "\ud800\ud800"); break;
                        case 17: w.WriteElementString("a", "a", "\ud800\ud800", "\ud800\ud800"); break;
                        case 18: w.WriteElementString("a", "a", "a", "\ud800\ud800"); break;
                        case 19: w.WriteEntityRef("\ud800\ud800"); break;
                        case 20: w.WriteName("\ud800\ud800"); break;
                        case 21: w.WriteNmToken("\ud800\ud800"); break;
                        case 22: w.WriteProcessingInstruction("\ud800\ud800", "\ud800\ud800"); break;
                        case 23: w.WriteQualifiedName("\ud800\ud800", "\ud800\ud800"); break;
                        case 24: w.WriteStartAttribute("\ud800\ud800"); break;
                        case 25: w.WriteStartAttribute("a", "\ud800\ud800"); break;
                        case 26: w.WriteStartAttribute("a", "b", "\ud800\ud800"); break;
                        case 27: w.WriteStartElement("\ud800\ud800"); break;
                        case 28: w.WriteStartElement("\ud800\ud800", "\ud800\ud800"); break;
                        case 29: w.WriteStartElement("a", "\ud800\ud800", "\ud800\ud800"); break;
                        case 30: w.WriteDocType("a", "\ud800\ud800", "\ud800\ud800", "\ud800\ud800"); break;
                        case 31: w.WriteDocType("a", "b", "\ud800\ud800", "\ud800\ud800"); break;
                        case 32: w.WriteDocType("a", "b", "c", "\ud800\ud800"); break;
                        case 33: w.WriteAttributeString("a", "\ud800\ud800"); break;
                        case 34: w.WriteElementString("a", "\ud800\ud800"); break;
                        case 35: w.WriteProcessingInstruction("a", "\ud800\ud800"); break;
                        case 36: w.WriteQualifiedName("a", "\ud800\ud800"); break;
                    }
                }
                catch (InvalidOperationException) { return; }
                catch (ArgumentException) { return; }
            }
            catch (XmlException)
            {
                try
                {
                    switch (param)
                    {
                        case 14: w.WriteDocType("\ud800\ud800", "\ud800\ud800", "\ud800\ud800", "\ud800\ud800"); break;
                        case 30: w.WriteDocType("a", "\ud800\ud800", "\ud800\ud800", "\ud800\ud800"); break;
                        case 31: w.WriteDocType("a", "b", "\ud800\ud800", "\ud800\ud800"); break;
                        case 32: w.WriteDocType("a", "b", "c", "\ud800\ud800"); break;
                    }
                }
                catch (XmlException)
                {
                    Assert.True((param == 14), "exception expected only for doctype");
                    return;
                }
                catch (InvalidOperationException) { Assert.True(false, "InvalidOperationException not expected here"); }
            }
            finally
            {
                try
                {
                    w.Dispose();
                }
                catch (ArgumentException)
                {
                    result = true;
                }
            }
            Assert.True(result || (utils.WriterType == WriterType.CharCheckingWriter && skipParams.Contains(param)));
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        public void var_10(XmlWriterUtils utils, int param)
        {
            int iBufferSize = 5;
            int iIndex = 0;
            int iCount = 6;
            byte[] byteBuffer = new byte[iBufferSize];
            for (int i = 0; i < iBufferSize; i++)
                byteBuffer[i] = (byte)(i + '0');

            char[] charBuffer = new char[iBufferSize];
            for (int i = 0; i < iBufferSize; i++)
                charBuffer[i] = (char)(i + '0');

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.ConformanceLevel = ConformanceLevel.Auto;
            using (XmlWriter w = utils.CreateWriter(ws))
            {
                try
                {
                    switch (param)
                    {
                        case 1: w.WriteChars(charBuffer, iIndex, iCount); break;
                        case 2: w.WriteRaw(charBuffer, iIndex, iCount); break;
                        case 3: w.WriteStartElement("a"); w.WriteBinHex(byteBuffer, iIndex, iCount); break;
                        case 4: w.WriteBase64(byteBuffer, iIndex, iCount); break;
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    try
                    {
                        switch (param)
                        {
                            case 1: w.WriteChars(charBuffer, iIndex, iCount); break;
                            case 2: w.WriteRaw(charBuffer, iIndex, iCount); break;
                            case 3: w.WriteBinHex(byteBuffer, iIndex, iCount); break;
                            case 4: w.WriteBase64(byteBuffer, iIndex, iCount); break;
                        }
                    }
                    catch (ArgumentOutOfRangeException) { return; }
                    catch (InvalidOperationException) { return; }
                }
                catch (IndexOutOfRangeException)
                {
                    try
                    {
                        switch (param)
                        {
                            case 1: w.WriteChars(charBuffer, iIndex, iCount); break;
                        }
                    }
                    catch (IndexOutOfRangeException) { Assert.True((utils.WriterType == WriterType.CharCheckingWriter)); }
                }
            }
            Assert.True(false);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void var_11(int param)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                switch (param)
                {
                    case 1: ws.ConformanceLevel = (ConformanceLevel)777; break;
                    case 2: ws.NewLineHandling = (NewLineHandling)777; break;
                    case 3: ws.ConformanceLevel = (ConformanceLevel)(-1); break;
                    case 4: ws.NewLineHandling = (NewLineHandling)(-1); break;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: ws.ConformanceLevel = (ConformanceLevel)555; break;
                        case 2: ws.NewLineHandling = (NewLineHandling)555; break;
                        case 3: ws.ConformanceLevel = (ConformanceLevel)(-1); break;
                        case 4: ws.NewLineHandling = (NewLineHandling)(-1); break;
                    }
                }
                catch (ArgumentOutOfRangeException) { return; }
            }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        [XmlWriterInlineData(7)]
        [XmlWriterInlineData(8)]
        public void var_12(XmlWriterUtils utils, int param)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            TextWriter stringWriter = new StringWriter();

            switch (param)
            {
                case 1: XmlWriter w1 = WriterHelper.Create(stringWriter, overrideAsync: true, async: utils.Async); break;
                case 2: XmlWriter w2 = WriterHelper.Create(stringWriter, overrideAsync: true, async: utils.Async); break;
                case 3: XmlWriter w3 = WriterHelper.Create(new StringBuilder(), overrideAsync: true, async: utils.Async); break;
                case 4: XmlWriter w4 = WriterHelper.Create(WriterHelper.Create(stringWriter, overrideAsync: true, async: utils.Async), overrideAsync: true, async: utils.Async); break;
                case 5: XmlWriter w5 = WriterHelper.Create(stringWriter, ws, overrideAsync: true, async: utils.Async); break;
                case 6: XmlWriter w6 = WriterHelper.Create(stringWriter, ws, overrideAsync: true, async: utils.Async); break;
                case 7: XmlWriter w7 = WriterHelper.Create(new StringBuilder(), ws, overrideAsync: true, async: utils.Async); break;
                case 8: XmlWriter w8 = WriterHelper.Create(WriterHelper.Create(stringWriter, overrideAsync: true, async: utils.Async), ws, overrideAsync: true, async: utils.Async); break;
            }
            return;
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        [XmlWriterInlineData(7)]
        [XmlWriterInlineData(8)]
        [XmlWriterInlineData(9)]
        [XmlWriterInlineData(10)]
        [XmlWriterInlineData(11)]
        [XmlWriterInlineData(12)]
        [XmlWriterInlineData(13)]
        [XmlWriterInlineData(14)]
        [XmlWriterInlineData(15)]
        [XmlWriterInlineData(16)]
        [XmlWriterInlineData(17)]
        [XmlWriterInlineData(18)]
        [XmlWriterInlineData(19)]
        [XmlWriterInlineData(20)]
        [XmlWriterInlineData(21)]
        [XmlWriterInlineData(22)]
        [XmlWriterInlineData(23)]
        [XmlWriterInlineData(24)]
        [XmlWriterInlineData(25)]
        [XmlWriterInlineData(26)]
        [XmlWriterInlineData(27)]
        public void var_13(XmlWriterUtils utils, int param)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.ConformanceLevel = ConformanceLevel.Document;
            XmlWriter w = utils.CreateWriter(ws);
            bool result = false;
            if (param != 30 && param != 31 && param != 32)
                w.WriteStartElement("Root");
            switch (param)
            {
                case 1: w.WriteComment(string.Empty); result = true; break;
                case 2: w.WriteCData(string.Empty); result = true; break;
                case 4: w.WriteRaw(string.Empty); result = true; break;
                case 5: w.WriteString(string.Empty); result = true; break;
                case 6: w.WriteValue(string.Empty); result = true; break;
                case 7: w.WriteWhitespace(string.Empty); result = true; break;
            }
            try
            {
                switch (param)
                {
                    case 3: w.WriteEntityRef(string.Empty); break;
                    case 8: w.WriteAttributeString(string.Empty, string.Empty); break;
                    case 9: w.WriteAttributeString(string.Empty, string.Empty, string.Empty); break;
                    case 10: w.WriteAttributeString(string.Empty, string.Empty, string.Empty, string.Empty); break;
                    case 11: w.WriteDocType(string.Empty, string.Empty, string.Empty, string.Empty); break;
                    case 12: w.WriteElementString(string.Empty, string.Empty); break;
                    case 13: w.WriteElementString(string.Empty, string.Empty, string.Empty); break;
                    case 14: w.WriteElementString(string.Empty, string.Empty, string.Empty, string.Empty); break;
                    case 15: w.WriteEntityRef(string.Empty); break;
                    case 16: w.WriteName(string.Empty); break;
                    case 17: w.WriteNmToken(string.Empty); break;
                    case 18: w.WriteProcessingInstruction(string.Empty, string.Empty); break;
                    case 19: w.WriteQualifiedName(string.Empty, string.Empty); break;
                    case 20: w.WriteStartAttribute(string.Empty); break;
                    case 21: w.WriteStartAttribute(string.Empty, string.Empty); break;
                    case 22: w.WriteStartAttribute(string.Empty, string.Empty, string.Empty); break;
                    case 23: w.WriteStartElement(string.Empty); break;
                    case 24: w.WriteStartElement(string.Empty, string.Empty); break;
                    case 25: w.WriteStartElement(string.Empty, string.Empty, string.Empty); break;
                    case 26: w.WriteDocType(string.Empty, string.Empty, string.Empty, string.Empty); break;
                    case 27: w.WriteProcessingInstruction(string.Empty, string.Empty); break;
                }
            }
            catch (ArgumentException)
            {
                try
                {
                    switch (param)
                    {
                        case 3: w.WriteEntityRef(string.Empty); break;
                        case 8: w.WriteAttributeString(string.Empty, string.Empty); break;
                        case 9: w.WriteAttributeString(string.Empty, string.Empty, string.Empty); break;
                        case 10: w.WriteAttributeString(string.Empty, string.Empty, string.Empty, string.Empty); break;
                        case 11: w.WriteDocType(string.Empty, string.Empty, string.Empty, string.Empty); break;
                        case 12: w.WriteElementString(string.Empty, string.Empty); break;
                        case 13: w.WriteElementString(string.Empty, string.Empty, string.Empty); break;
                        case 14: w.WriteElementString(string.Empty, string.Empty, string.Empty, string.Empty); break;
                        case 15: w.WriteEntityRef(string.Empty); break;
                        case 16: w.WriteName(string.Empty); break;
                        case 17: w.WriteNmToken(string.Empty); break;
                        case 18: w.WriteProcessingInstruction(string.Empty, string.Empty); break;
                        case 19: w.WriteQualifiedName(string.Empty, string.Empty); break;
                        case 20: w.WriteStartAttribute(string.Empty); break;
                        case 21: w.WriteStartAttribute(string.Empty, string.Empty); break;
                        case 22: w.WriteStartAttribute(string.Empty, string.Empty, string.Empty); break;
                        case 23: w.WriteStartElement(string.Empty); break;
                        case 24: w.WriteStartElement(string.Empty, string.Empty); break;
                        case 25: w.WriteStartElement(string.Empty, string.Empty, string.Empty); break;
                        case 26: w.WriteDocType(string.Empty, string.Empty, string.Empty, string.Empty); break;
                        case 27: w.WriteProcessingInstruction(string.Empty, string.Empty); break;
                    }
                }
                catch (ArgumentException) { result = true; }
            }
            finally
            {
                w.Dispose();
            }
            Assert.True((result || param == 19 && utils.WriterType == WriterType.CustomWriter));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void var_14(int param)
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                switch (param)
                {
                    case 1: ws.IndentChars = null; break;
                    case 2: ws.NewLineChars = null; break;
                }
            }
            catch (ArgumentNullException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: ws.IndentChars = null; break;
                        case 2: ws.NewLineChars = null; break;
                    }
                }
                catch (ArgumentNullException) { return; }
            }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData]
        public void var_15(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            bool isUnicode = (utils.WriterType == WriterType.UnicodeWriter || utils.WriterType == WriterType.UnicodeWriterIndent) ? true : false;
            bool isIndent = (utils.WriterType == WriterType.UTF8WriterIndent || utils.WriterType == WriterType.UnicodeWriterIndent) ? true : false;

            w.WriteElementString("a", "b");
            ((IDisposable)w).Dispose();
            ((IDisposable)w).Dispose();
            ((IDisposable)w).Dispose();
            CError.Compare(w.LookupPrefix(""), string.Empty, "LookupPrefix");
            CError.Compare(w.WriteState, WriteState.Closed, "WriteState");
            CError.Compare(w.XmlLang, null, "XmlLang");
            CError.Compare(w.XmlSpace, XmlSpace.None, "XmlSpace");
            if (utils.WriterType != WriterType.CustomWriter)
            {
                CError.Compare(w.Settings.CheckCharacters, true, "CheckCharacters");
                CError.Compare(w.Settings.CloseOutput, false, "CloseOutput");
                CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "ConformanceLevel");
                CError.Compare(w.Settings.Indent, (isIndent) ? true : false, "Indent");
                CError.Compare(w.Settings.IndentChars, "  ", "IndentChars");
                CError.Compare(w.Settings.NewLineChars, Environment.NewLine, "NewLineChars");
                CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "NewLineHandling");
                CError.Compare(w.Settings.NewLineOnAttributes, false, "NewLineOnAttributes");
                CError.Compare(w.Settings.OmitXmlDeclaration, true, "OmitXmlDeclaration");
                CError.Compare(w.Settings.Encoding.WebName, (isUnicode) ? "utf-16" : "utf-8", "Encoding");
            }
            return;
        }

        [Theory]
        [XmlWriterInlineData]
        public void var_16(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            bool isUnicode = (utils.WriterType == WriterType.UnicodeWriter || utils.WriterType == WriterType.UnicodeWriterIndent) ? true : false;
            bool isIndent = (utils.WriterType == WriterType.UTF8WriterIndent || utils.WriterType == WriterType.UnicodeWriterIndent) ? true : false;

            w.WriteElementString("a", "b");
            try
            {
                w.WriteDocType("a", "b", "c", "d");
            }
            catch (InvalidOperationException)
            {
                CError.Compare(w.LookupPrefix(""), string.Empty, "LookupPrefix");
                CError.Compare(w.WriteState, WriteState.Error, "WriteState");
                CError.Compare(w.XmlLang, null, "XmlLang");
                CError.Compare(w.XmlSpace, XmlSpace.None, "XmlSpace");
                if (utils.WriterType != WriterType.CustomWriter)
                {
                    CError.Compare(w.Settings.CheckCharacters, true, "CheckCharacters");
                    CError.Compare(w.Settings.CloseOutput, false, "CloseOutput");
                    CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "ConformanceLevel");
                    CError.Compare(w.Settings.Indent, (isIndent) ? true : false, "Indent");
                    CError.Compare(w.Settings.IndentChars, "  ", "IndentChars");
                    CError.Compare(w.Settings.NewLineChars, Environment.NewLine, "NewLineChars");
                    CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "NewLineHandling");
                    CError.Compare(w.Settings.NewLineOnAttributes, false, "NewLineOnAttributes");
                    CError.Compare(w.Settings.OmitXmlDeclaration, true, "OmitXmlDeclaration");
                    CError.Compare(w.Settings.Encoding.WebName, (isUnicode) ? "utf-16" : "utf-8", "Encoding");
                }
                return;
            }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData(WriterType.All & ~WriterType.Async)]
        public void bug601305(XmlWriterUtils utils)
        {
            CError.WriteLine("expected:");
            CError.WriteLine("<p:root xmlns:p='uri' />");
            CError.WriteLine("actual:");
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            StringWriter sw = new StringWriter();
            using (XmlWriter w = WriterHelper.Create(sw, ws, overrideAsync: true, async: utils.Async))
            {
                w.WriteStartElement("root", "uri");
                w.WriteStartAttribute("xmlns", "p", "http://www.w3.org/2000/xmlns/");
                w.WriteString("uri");
            }
            CError.Compare(sw.ToString(), "<root xmlns:p=\"uri\" xmlns=\"uri\" />", "writer output");
            return;
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        [XmlWriterInlineData(7)]
        [XmlWriterInlineData(8)]
        [XmlWriterInlineData(9)]
        [XmlWriterInlineData(10)]
        public void var17(XmlWriterUtils utils, int param)
        {
            if (utils.WriterType == WriterType.CustomWriter) return;
            XmlWriter writer = utils.CreateWriter();
            try
            {
                switch (param)
                {
                    case 1: writer.Settings.CheckCharacters = false; break;
                    case 2: writer.Settings.CloseOutput = false; break;
                    case 3: writer.Settings.ConformanceLevel = ConformanceLevel.Fragment; break;
                    case 4: writer.Settings.Encoding = Encoding.UTF8; break;
                    case 5: writer.Settings.Indent = false; break;
                    case 6: writer.Settings.IndentChars = "#"; break;
                    case 7: writer.Settings.NewLineChars = "%"; break;
                    case 8: writer.Settings.NewLineHandling = NewLineHandling.None; break;
                    case 9: writer.Settings.NewLineOnAttributes = false; break;
                    case 10: writer.Settings.OmitXmlDeclaration = true; break;
                }
            }
            catch (XmlException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: writer.Settings.CheckCharacters = false; break;
                        case 2: writer.Settings.CloseOutput = false; break;
                        case 3: writer.Settings.ConformanceLevel = ConformanceLevel.Fragment; break;
                        case 4: writer.Settings.Encoding = Encoding.UTF8; break;
                        case 5: writer.Settings.Indent = false; break;
                        case 6: writer.Settings.IndentChars = "#"; break;
                        case 7: writer.Settings.NewLineChars = "%"; break;
                        case 8: writer.Settings.NewLineHandling = NewLineHandling.None; break;
                        case 9: writer.Settings.NewLineOnAttributes = false; break;
                        case 10: writer.Settings.OmitXmlDeclaration = true; break;
                    }
                }
                catch (XmlException) { return; }
            }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        [XmlWriterInlineData(7)]
        [XmlWriterInlineData(8)]
        [XmlWriterInlineData(9)]
        [XmlWriterInlineData(10)]
        [XmlWriterInlineData(11)]
        [XmlWriterInlineData(12)]
        [XmlWriterInlineData(13)]
        [XmlWriterInlineData(14)]
        [XmlWriterInlineData(15)]
        [XmlWriterInlineData(16)]
        [XmlWriterInlineData(17)]
        [XmlWriterInlineData(18)]
        [XmlWriterInlineData(19)]
        [XmlWriterInlineData(20)]
        [XmlWriterInlineData(21)]
        [XmlWriterInlineData(22)]
        [XmlWriterInlineData(23)]
        [XmlWriterInlineData(24)]
        [XmlWriterInlineData(25)]
        [XmlWriterInlineData(26)]
        [XmlWriterInlineData(27)]
        [XmlWriterInlineData(28)]
        [XmlWriterInlineData(29)]
        public void var_18(XmlWriterUtils utils, int param)
        {
            XmlReader r = ReaderHelper.Create(new StringReader("<xmlns/>"));
            byte[] buffer = new byte[10];
            char[] chbuffer = new char[10];
            XmlWriter w = utils.CreateWriter();
            w.WriteElementString("a", "b");
            w.Dispose();
            CError.Compare(w.WriteState, WriteState.Closed, "WriteState should be Error");
            try
            {
                switch (param)
                {
                    case 1: w.WriteQualifiedName("foo", ""); break;
                    case 2: w.WriteAttributes(r, true); break;
                    case 3: w.WriteAttributeString("a", "b", "c", "d"); break;
                    case 4: w.WriteBase64(buffer, 0, 3); break;
                    case 5: w.WriteBinHex(buffer, 0, 3); break;
                    case 6: w.WriteCData("a"); break;
                    case 7: w.WriteCharEntity(char.MaxValue); break;
                    case 8: w.WriteChars(chbuffer, 1, 3); break;
                    case 9: w.WriteComment("a"); break;
                    case 10: w.WriteDocType("a", "b", "c", "d"); break;
                    case 11: w.WriteElementString("a", "b", "c", "d"); break;
                    case 12: w.WriteEndAttribute(); break;
                    case 13: w.WriteEndDocument(); break;
                    case 14: w.WriteEndElement(); break;
                    case 15: w.WriteEntityRef("a"); break;
                    case 16: w.WriteFullEndElement(); break;
                    case 17: w.WriteName("b"); break;
                    case 18: w.WriteNmToken("b"); break;
                    case 19: w.WriteNode(r, true); break;
                    case 20: w.WriteProcessingInstruction("a", "b"); break;
                    case 21: w.WriteRaw("a"); break;
                    case 22: w.WriteRaw(chbuffer, 1, 3); break;
                    case 23: w.WriteStartAttribute("a", "b", "c"); break;
                    case 24: w.WriteStartDocument(true); break;
                    case 25: w.WriteStartElement("a", "b", "c"); break;
                    case 26: w.WriteString("a"); break;
                    case 27: w.WriteSurrogateCharEntity('\uD812', '\uDD12'); break;
                    case 28: w.WriteValue(true); break;
                    case 29: w.WriteWhitespace(""); break;
                }
            }
            catch (InvalidOperationException)
            {
                try
                {
                    switch (param)
                    {
                        case 1: w.WriteQualifiedName("foo", ""); break;
                        case 3: w.WriteAttributeString("a", "b", "c", "d"); break;
                        case 4: w.WriteBase64(buffer, 0, 3); break;
                        case 5: w.WriteBinHex(buffer, 0, 3); break;
                        case 6: w.WriteCData("a"); break;
                        case 7: w.WriteCharEntity(char.MaxValue); break;
                        case 8: w.WriteChars(chbuffer, 1, 3); break;
                        case 9: w.WriteComment("a"); break;
                        case 10: w.WriteDocType("a", "b", "c", "d"); break;
                        case 11: w.WriteElementString("a", "b", "c", "d"); break;
                        case 12: w.WriteEndAttribute(); break;
                        case 13: w.WriteEndDocument(); break;
                        case 14: w.WriteEndElement(); break;
                        case 15: w.WriteEntityRef("a"); break;
                        case 16: w.WriteFullEndElement(); break;
                        case 17: w.WriteName("b"); break;
                        case 18: w.WriteNmToken("b"); break;
                        case 19: w.WriteNode(r, true); break;
                        case 20: w.WriteProcessingInstruction("a", "b"); break;
                        case 21: w.WriteRaw("a"); break;
                        case 22: w.WriteRaw(chbuffer, 1, 3); break;
                        case 23: w.WriteStartAttribute("a", "b", "c"); break;
                        case 24: w.WriteStartDocument(true); break;
                        case 25: w.WriteStartElement("a", "b", "c"); break;
                        case 26: w.WriteString("a"); break;
                        case 28: w.WriteValue(true); break;
                        case 29: w.WriteWhitespace(""); break;
                    }
                }
                catch (InvalidOperationException) { return; }
            }
            catch (ArgumentException)
            {
                try
                {
                    switch (param)
                    {
                        case 8: w.WriteChars(chbuffer, 1, 3); break;
                        case 27: w.WriteSurrogateCharEntity('\uD812', '\uDD12'); break;
                    }
                }
                catch (ArgumentException) { return; }
            }
            catch (XmlException)
            {
                try
                {
                    switch (param)
                    {
                        case 2: w.WriteAttributes(r, true); break;
                    }
                }
                catch (XmlException) { return; }
            }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        [XmlWriterInlineData(7)]
        [XmlWriterInlineData(8)]
        [XmlWriterInlineData(9)]
        [XmlWriterInlineData(10)]
        [XmlWriterInlineData(11)]
        [XmlWriterInlineData(12)]
        [XmlWriterInlineData(13)]
        [XmlWriterInlineData(14)]
        [XmlWriterInlineData(15)]
        [XmlWriterInlineData(16)]
        [XmlWriterInlineData(17)]
        [XmlWriterInlineData(18)]
        [XmlWriterInlineData(19)]
        [XmlWriterInlineData(20)]
        [XmlWriterInlineData(21)]
        [XmlWriterInlineData(22)]
        [XmlWriterInlineData(23)]
        [XmlWriterInlineData(24)]
        [XmlWriterInlineData(25)]
        [XmlWriterInlineData(26)]
        [XmlWriterInlineData(27)]
        [XmlWriterInlineData(28)]
        [XmlWriterInlineData(29)]
        public void var_19(XmlWriterUtils utils, int param)
        {
            XmlReader r = ReaderHelper.Create(new StringReader("<xmlns/>"));
            byte[] buffer = new byte[10];
            char[] chbuffer = new char[10];
            XmlWriter w = utils.CreateWriter();
            try
            {
                w.WriteStartDocument();
                w.WriteEntityRef("ent");
            }
            catch (InvalidOperationException)
            {
                CError.Compare(w.WriteState, WriteState.Error, "WriteState should be Error");
                try
                {
                    switch (param)
                    {
                        case 1: w.WriteQualifiedName("foo", ""); break;
                        case 2: w.WriteAttributes(r, true); break;
                        case 3: w.WriteAttributeString("a", "b", "c", "d"); break;
                        case 4: w.WriteBase64(buffer, 0, 3); break;
                        case 5: w.WriteBinHex(buffer, 0, 3); break;
                        case 6: w.WriteCData("a"); break;
                        case 7: w.WriteCharEntity(char.MaxValue); break;
                        case 8: w.WriteChars(chbuffer, 1, 3); break;
                        case 9: w.WriteComment("a"); break;
                        case 10: w.WriteDocType("a", "b", "c", "d"); break;
                        case 11: w.WriteElementString("a", "b", "c", "d"); break;
                        case 12: w.WriteEndAttribute(); break;
                        case 13: w.WriteEndDocument(); break;
                        case 14: w.WriteEndElement(); break;
                        case 15: w.WriteEntityRef("a"); break;
                        case 16: w.WriteFullEndElement(); break;
                        case 17: w.WriteName("b"); break;
                        case 18: w.WriteNmToken("b"); break;
                        case 19: w.WriteNode(r, true); break;
                        case 20: w.WriteProcessingInstruction("a", "b"); break;
                        case 21: w.WriteRaw("a"); break;
                        case 22: w.WriteRaw(chbuffer, 1, 3); break;
                        case 23: w.WriteStartAttribute("a", "b", "c"); break;
                        case 24: w.WriteStartDocument(true); break;
                        case 25: w.WriteStartElement("a", "b", "c"); break;
                        case 26: w.WriteString("a"); break;
                        case 27: w.WriteSurrogateCharEntity('\uD812', '\uDD12'); break;
                        case 28: w.WriteValue(true); break;
                        case 29: w.WriteWhitespace(""); break;
                    }
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        switch (param)
                        {
                            case 1: w.WriteQualifiedName("foo", ""); break;
                            case 3: w.WriteAttributeString("a", "b", "c", "d"); break;
                            case 4: w.WriteBase64(buffer, 0, 3); break;
                            case 5: w.WriteBinHex(buffer, 0, 3); break;
                            case 6: w.WriteCData("a"); break;
                            case 7: w.WriteCharEntity(char.MaxValue); break;
                            case 8: w.WriteChars(chbuffer, 1, 3); break;
                            case 9: w.WriteComment("a"); break;
                            case 10: w.WriteDocType("a", "b", "c", "d"); break;
                            case 11: w.WriteElementString("a", "b", "c", "d"); break;
                            case 12: w.WriteEndAttribute(); break;
                            case 13: w.WriteEndDocument(); break;
                            case 14: w.WriteEndElement(); break;
                            case 15: w.WriteEntityRef("a"); break;
                            case 16: w.WriteFullEndElement(); break;
                            case 17: w.WriteName("b"); break;
                            case 18: w.WriteNmToken("b"); break;
                            case 19: w.WriteNode(r, true); break;
                            case 20: w.WriteProcessingInstruction("a", "b"); break;
                            case 21: w.WriteRaw("a"); break;
                            case 22: w.WriteRaw(chbuffer, 1, 3); break;
                            case 23: w.WriteStartAttribute("a", "b", "c"); break;
                            case 24: w.WriteStartDocument(true); break;
                            case 25: w.WriteStartElement("a", "b", "c"); break;
                            case 26: w.WriteString("a"); break;
                            case 28: w.WriteValue(true); break;
                            case 29: w.WriteWhitespace(""); break;
                        }
                    }
                    catch (InvalidOperationException) { return; }
                }
                catch (ArgumentException)
                {
                    try
                    {
                        switch (param)
                        {
                            case 8: w.WriteChars(chbuffer, 1, 3); break;
                            case 27: w.WriteSurrogateCharEntity('\uD812', '\uDD12'); break;
                        }
                    }
                    catch (ArgumentException) { return; }
                }
                catch (XmlException)
                {
                    try
                    {
                        switch (param)
                        {
                            case 2: w.WriteAttributes(r, true); break;
                        }
                    }
                    catch (XmlException) { return; }
                }
            }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData]
        public void var_20(XmlWriterUtils utils)
        {
            XmlWriter w = utils.CreateWriter();
            w.WriteStartElement("root");
            try
            {
                w.WriteAttributeString("attr1", "\uD812\uD812");
                w.WriteEndElement();
            }
            catch (ArgumentException e)
            {
                CError.WriteLine(e);
                try
                {
                    w.WriteAttributeString("attr2", "\uD812\uD812");
                    w.WriteEndElement();
                }
                catch (InvalidOperationException ioe) { CError.WriteLine(ioe); return; }
                catch (ArgumentException ae) { CError.WriteLine(ae); return; }
            }
            finally
            {
                w.Dispose();
            }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        [XmlWriterInlineData(5)]
        [XmlWriterInlineData(6)]
        [XmlWriterInlineData(7)]
        [XmlWriterInlineData(8)]
        [XmlWriterInlineData(9)]
        [XmlWriterInlineData(10)]
        [XmlWriterInlineData(11)]
        [XmlWriterInlineData(12)]
        [XmlWriterInlineData(13)]
        [XmlWriterInlineData(14)]
        [XmlWriterInlineData(15)]
        [XmlWriterInlineData(16)]
        [XmlWriterInlineData(17)]
        [XmlWriterInlineData(18)]
        [XmlWriterInlineData(19)]
        [XmlWriterInlineData(20)]
        [XmlWriterInlineData(21)]
        [XmlWriterInlineData(22)]
        [XmlWriterInlineData(23)]
        [XmlWriterInlineData(24)]
        [XmlWriterInlineData(25)]
        [XmlWriterInlineData(26)]
        [XmlWriterInlineData(27)]
        [XmlWriterInlineData(28)]
        [XmlWriterInlineData(29)]
        [XmlWriterInlineData(30)]
        [XmlWriterInlineData(31)]
        [XmlWriterInlineData(32)]
        [XmlWriterInlineData(33)]
        [XmlWriterInlineData(34)]
        public void var_21(XmlWriterUtils utils, int param)
        {
            bool result = false;
            string val = "\uDE34\uD9A2";
            XmlWriter w = utils.CreateWriter();
            if (param != 13 && param != 14 && param != 15) w.WriteStartElement("a", "b");
            try
            {
                switch (param)
                {
                    case 1: w.WriteStartAttribute("c"); w.WriteValue(val); break;
                    case 2: w.WriteStartAttribute("c"); w.WriteComment(val); break;
                    case 3: w.WriteStartAttribute("c"); w.WriteCData(val); break;
                    case 4: w.WriteStartAttribute("c"); w.WriteProcessingInstruction("a", val); break;
                    case 5: w.WriteStartAttribute("c"); w.WriteRaw(val); break;
                    case 6: w.WriteValue(val); break;
                    case 7: w.WriteComment(val); break;
                    case 8: w.WriteCData(val); break;
                    case 9: w.WriteProcessingInstruction("a", val); break;
                    case 10: w.WriteRaw(val); break;
                    case 11: w.WriteAttributeString("a", val); break;
                    case 12: w.WriteCharEntity('\uDE34'); break;
                    case 13: w.WriteDocType("a", val, val, val); break;
                    case 14: w.WriteDocType("a", "b", val, val); break;
                    case 15: w.WriteDocType("a", "b", "c", val); break;
                    case 16: w.WriteElementString(val, val, val, val); break;
                    case 17: w.WriteElementString("a", val, val, val); break;
                    case 18: w.WriteElementString("a", "b", val, val); break;
                    case 19: w.WriteElementString("a", "b", "c", val); break;
                    case 20: w.WriteEntityRef(val); break;
                    case 21: w.WriteName(val); break;
                    case 22: w.WriteNmToken(val); break;
                    case 23: w.WriteQualifiedName(val, val); break;
                    case 24: w.WriteQualifiedName("a", val); break;
                    case 25: w.WriteStartAttribute(val); break;
                    case 26: w.WriteStartAttribute("a", val); break;
                    case 27: w.WriteStartAttribute("a", val, val); break;
                    case 28: w.WriteStartElement(val); break;
                    case 29: w.WriteStartElement("a", val); break;
                    case 30: w.WriteStartElement("a", val, val); break;
                    case 31: w.WriteString(val); break;
                    case 32: w.WriteWhitespace(val); break;
                    case 33: w.WriteStartAttribute("c"); w.WriteString(val); break;
                    case 34: w.WriteSurrogateCharEntity('\uD9A2', '\uDE34'); break;
                }
            }
            catch (ArgumentException e)
            {
                CError.WriteLine(e.Message);
                try
                {
                    switch (param)
                    {
                        case 1: w.WriteStartAttribute("b"); w.WriteValue(val); break;
                        case 2: w.WriteStartAttribute("b"); w.WriteComment(val); break;
                        case 3: w.WriteStartAttribute("b"); w.WriteCData(val); break;
                        case 4: w.WriteStartAttribute("b"); w.WriteProcessingInstruction("a", val); break;
                        case 5: w.WriteStartAttribute("b"); w.WriteRaw(val); break;
                        case 6: w.WriteValue(val); break;
                        case 7: w.WriteComment(val); break;
                        case 8: w.WriteCData(val); break;
                        case 9: w.WriteProcessingInstruction("a", val); break;
                        case 10: w.WriteRaw(val); break;
                        case 11: w.WriteAttributeString("a2", val); break;
                        case 12: w.WriteCharEntity('\uDE34'); break;
                        case 13: w.WriteDocType("a", val, val, val); break;
                        case 14: w.WriteDocType("a", "b", val, val); break;
                        case 15: w.WriteDocType("a", "b", "c", val); break;
                        case 16: w.WriteElementString(val, val, val, val); break;
                        case 17: w.WriteElementString("a", val, val, val); break;
                        case 18: w.WriteElementString("a", "b", val, val); break;
                        case 19: w.WriteElementString("a", "b", "c", val); break;
                        case 20: w.WriteEntityRef(val); break;
                        case 21: w.WriteName(val); break;
                        case 22: w.WriteNmToken(val); break;
                        case 23: w.WriteQualifiedName(val, val); break;
                        case 24: w.WriteQualifiedName("a", val); break;
                        case 25: w.WriteStartAttribute(val); break;
                        case 26: w.WriteStartAttribute("a", val); break;
                        case 27: w.WriteStartAttribute("a", val, val); break;
                        case 28: w.WriteStartElement(val); break;
                        case 29: w.WriteStartElement("a", val); break;
                        case 30: w.WriteStartElement("a", val, val); break;
                        case 31: w.WriteString(val); break;
                        case 32: w.WriteWhitespace(val); break;
                        case 33: w.WriteStartAttribute("b"); w.WriteString(val); break;
                        case 34: w.WriteSurrogateCharEntity('\uD9A2', '\uDE34'); break;
                    }
                }
                catch (InvalidOperationException) { CError.WriteLine(e.Message); result = true; }
                catch (ArgumentException) { CError.WriteLine(e.Message); result = true; }
            }
            catch (XmlException e)
            {
                CError.WriteLine(e.Message);
                try
                {
                    switch (param)
                    {
                        case 13: w.WriteDocType("a", val, val, val); break;
                        case 14: w.WriteDocType("a", "b", val, val); break;
                        case 15: w.WriteDocType("a", "b", "c", val); break;
                        case 21: w.WriteName(val); break;
                        case 22: w.WriteNmToken(val); break;
                    }
                }
                catch (XmlException)
                {
                    result = (utils.WriterType == WriterType.CharCheckingWriter && (param == 21 || param == 22));
                }
                catch (InvalidOperationException) { result = false; }
            }
            finally
            {
                try
                {
                    w.Dispose();
                }
                catch (ArgumentException) { result = true; }
            }
            Assert.True(result);
        }

        [Theory]
        [XmlWriterInlineData(1)]
        [XmlWriterInlineData(2)]
        [XmlWriterInlineData(3)]
        [XmlWriterInlineData(4)]
        public void bug600541(XmlWriterUtils utils, int param)
        {
            string xml = "<root a=\"a\" b=\"b\" c=\"c\" d=\"d\" />";
            switch (param)
            {
                case 1: break;
                case 2: xml = "<root b=\"b\" c=\"c\" d=\"d\" />"; break;
                case 3: xml = "<root c=\"c\" d=\"d\" />"; break;
                case 4: xml = "<root d=\"d\" />"; break;
            }
            using (XmlReader r = ReaderHelper.Create(new StringReader(xml)))
            {
                r.Read();
                CError.Compare(r.NodeType, XmlNodeType.Element, "XNT");
                CError.Compare(r.MoveToFirstAttribute(), true, "MFA");
                using (XmlWriter w = utils.CreateWriter())
                {
                    w.WriteStartElement("root");
                    switch (param)
                    {
                        case 1: break;
                        case 2: r.MoveToAttribute("b"); break;
                        case 3: r.MoveToAttribute("c"); break;
                        case 4: r.MoveToAttribute("d"); break;
                    }
                    w.WriteAttributes(r, true);
                    w.Dispose();
                    Assert.True((utils.CompareString(xml)));
                }
            }
        }

        [Theory]
        [XmlWriterInlineData]
        public void bug630890(XmlWriterUtils utils)
        {
            object obj = (object)1;
            for (int i = 0; i < 100000; i++)
            {
                obj = new object[1] { obj };
            }

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("Root");
                try
                {
                    w.WriteValue(obj);
                    CError.Compare(false, "Failed1");
                }
                catch (InvalidCastException e)
                {
                    CError.WriteLine(e);
                    try
                    {
                        w.WriteValue(obj);
                        CError.Compare(false, "Failed1");
                    }
                    catch (InvalidOperationException) { CError.WriteLine(e.Message); return; }
                    catch (InvalidCastException) { CError.WriteLine(e.Message); return; }
                }
            }
            Assert.True(false);
        }

        [Theory]
        [XmlWriterInlineData]
        public void PassingArrayWithNullOrEmptyItemsCausesWriteValueToFail(XmlWriterUtils utils)
        {
            string[] a = new string[5];
            string exp = "<b>a a1 </b>";
            a[0] = "a";
            a[1] = "a1";
            a[3] = null;
            a[4] = "";

            using (XmlWriter w = utils.CreateWriter())
            {
                w.WriteStartElement("b");
                w.WriteValue(a);
            }
            Assert.True((utils.CompareString(exp)));
        }
    }
}
