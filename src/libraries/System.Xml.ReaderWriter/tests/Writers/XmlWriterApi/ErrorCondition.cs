// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using System.Linq;
using System.Text;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    //[TestCase(Name = "ErrorCondition")]
    public partial class TCErrorConditionWriter : XmlWriterTestCaseBase
    {
        public override int Init(object o)
        {
            int i = base.Init(0);
            return i;
        }

        public static string file = "writerErr.out";

        //[Variation(Desc = "XmlWriter.Create((Stream)null)", Pri = 2, Param = 1)]
        //[Variation(Desc = "XmlWriter.Create((TextWriter)null)", Pri = 2, Param = 2)]
        //[Variation(Desc = "XmlWriter.Create((StringBuilder)null)", Pri = 2, Param = 3)]
        //[Variation(Desc = "XmlWriter.Create((XmlWriter)null)", Pri = 2, Param = 4)]
        //[Variation(Desc = "XmlWriter.Create((Stream)null, ws)", Pri = 2, Param = 5)]
        //[Variation(Desc = "XmlWriter.Create((TextWriter)null, ws)", Pri = 2, Param = 6)]
        //[Variation(Desc = "XmlWriter.Create((StringBuilder)null, ws)", Pri = 2, Param = 7)]
        //[Variation(Desc = "XmlWriter.Create((XmlWriter)null, ws)", Pri = 2, Param = 8)]
        public int var_01()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            int param = (int)CurVariation.Param;
            try
            {
                switch (param)
                {
                    case 1: XmlWriter w1 = WriterHelper.Create((Stream)null); break;
                    case 2: XmlWriter w2 = WriterHelper.Create((TextWriter)null); break;
                    case 3: XmlWriter w3 = WriterHelper.Create((StringBuilder)null); break;
                    case 4: XmlWriter w4 = WriterHelper.Create((XmlWriter)null); break;
                    case 5: XmlWriter w5 = WriterHelper.Create((Stream)null, ws); break;
                    case 6: XmlWriter w6 = WriterHelper.Create((TextWriter)null, ws); break;
                    case 7: XmlWriter w7 = WriterHelper.Create((StringBuilder)null, ws); break;
                    case 8: XmlWriter w8 = WriterHelper.Create((XmlWriter)null, ws); break;
                }
            }
            catch (ArgumentNullException) { return TEST_PASS; }
            return TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriter.WriteAttributes(null, true)", Pri = 2, Param = true)]
        //[Variation(Desc = "XmlWriter.WriteAttributes(null, false)", Pri = 2, Param = false)]
        public int var_02()
        {
            bool param = (bool)CurVariation.Param;
            bool result = false;

            XmlWriter w = CreateWriter();
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
            return (result) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriter.WriteNode((XmlReader)null, true)", Pri = 2, Param = true)]
        //[Variation(Desc = "XmlWriter.WriteNode((XmlReader)null, false)", Pri = 2, Param = false)]
        public int var_03()
        {
            bool param = (bool)CurVariation.Param;
            bool result = false;

            XmlWriter w = CreateWriter();
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
            return (result) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriter.WriteAttributeString(null, null)", Pri = 2, Param = 1)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString(null, null, null)", Pri = 2, Param = 2)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString('a', null, null, null)", Pri = 2, Param = 3)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString(null, null, 'a', null)", Pri = 2, Param = 4)]
        //[Variation(Desc = "XmlWriter.WriteDocType(null, null, null, null)", Pri = 2, Param = 5)]
        //[Variation(Desc = "XmlWriter.WriteElementString(null, null)", Pri = 2, Param = 6)]
        //[Variation(Desc = "XmlWriter.WriteElementString(null, null, null)", Pri = 2, Param = 7)]
        //[Variation(Desc = "XmlWriter.WriteElementString('a', null, null, null)", Pri = 2, Param = 8)]
        //[Variation(Desc = "XmlWriter.WriteElementString('a', null, 'a', null)", Pri = 2, Param = 9)]
        //[Variation(Desc = "XmlWriter.WriteEntityRef(null)", Pri = 2, Param = 10)]
        //[Variation(Desc = "XmlWriter.WriteName(null)", Pri = 2, Param = 11)]
        //[Variation(Desc = "XmlWriter.WriteNmToken(null)", Pri = 2, Param = 12)]
        //[Variation(Desc = "XmlWriter.WriteProcessingInstruction(null, null)", Pri = 2, Param = 13)]
        //[Variation(Desc = "XmlWriter.WriteQualifiedName(null, null)", Pri = 2, Param = 14)]
        //[Variation(Desc = "XmlWriter.WriteStartAttribute(null)", Pri = 2, Param = 15)]
        //[Variation(Desc = "XmlWriter.WriteStartAttribute(null, null)", Pri = 2, Param = 16)]
        //[Variation(Desc = "XmlWriter.WriteStartAttribute('a', null, null)", Pri = 2, Param = 17)]
        //[Variation(Desc = "XmlWriter.WriteStartElement(null)", Pri = 2, Param = 18)]
        //[Variation(Desc = "XmlWriter.WriteStartElement(null, null)", Pri = 2, Param = 19)]
        //[Variation(Desc = "XmlWriter.WriteStartElement('a', null, null)", Pri = 2, Param = 20)]
        public int var_04()
        {
            int param = (int)CurVariation.Param;
            bool result = false;

            XmlWriter w = CreateWriter();
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
                catch (NullReferenceException) { result = (WriterType == WriterType.CharCheckingWriter) ? true : false; }
            }
            finally
            {
                w.Dispose();
            }
            return (result || param == 14 && WriterType == WriterType.CustomWriter) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriter.WriteBinHex(null)", Pri = 2, Param = 1)]
        //[Variation(Desc = "XmlWriter.WriteBase64(null)", Pri = 2, Param = 2)]
        //[Variation(Desc = "XmlWriter.WriteChars(null)", Pri = 2, Param = 3)]
        //[Variation(Desc = "XmlWriter.LookupPrefix(null)", Pri = 2, Param = 4)]
        //[Variation(Desc = "XmlWriter.WriteRaw(null)", Pri = 2, Param = 5)]
        //[Variation(Desc = "XmlWriter.WriteValue((object)null)", Pri = 2, Param = 6)]
        public int var_05()
        {
            int param = (int)CurVariation.Param;
            bool result = false;

            XmlWriter w = CreateWriter();
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
            return (result) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriter.WriteComment", Pri = 2, Param = 1)]
        //[Variation(Desc = "XmlWriter.WriteCharEntity", Pri = 2, Param = 2)]
        //[Variation(Desc = "XmlWriter.WriteCData", Pri = 2, Param = 3)]
        //[Variation(Desc = "XmlWriter.WriteEntityRef", Pri = 2, Param = 4)]
        //[Variation(Desc = "XmlWriter.WriteRaw", Pri = 2, Param = 5)]
        //[Variation(Desc = "XmlWriter.WriteString", Pri = 2, Param = 6)]
        //[Variation(Desc = "XmlWriter.WriteSurrogateCharEntity", Pri = 2, Param = 7)]
        //[Variation(Desc = "XmlWriter.WriteValue((string)null)", Pri = 2, Param = 8)]
        //[Variation(Desc = "XmlWriter.WriteWhitespace", Pri = 2, Param = 9)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString('\ud800', '\ud800')", Pri = 2, Param = 10)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString('a', '\ud800', '\ud800')", Pri = 2, Param = 11)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString('a', 'b', '\ud800', '\ud800')", Pri = 2, Param = 12)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString('a', 'b', 'c', '\ud800')", Pri = 2, Param = 13)]
        //[Variation(Desc = "XmlWriter.WriteDocType('\ud800', '\ud800', '\ud800', '\ud800')", Pri = 2, Param = 14)]
        //[Variation(Desc = "XmlWriter.WriteElementString('\ud800', '\ud800')", Pri = 2, Param = 15)]
        //[Variation(Desc = "XmlWriter.WriteElementString('a', '\ud800', '\ud800')", Pri = 2, Param = 16)]
        //[Variation(Desc = "XmlWriter.WriteElementString('a', 'b', '\ud800', '\ud800')", Pri = 2, Param = 17)]
        //[Variation(Desc = "XmlWriter.WriteElementString('a', 'b', 'c', '\ud800')", Pri = 2, Param = 18)]
        //[Variation(Desc = "XmlWriter.WriteEntityRef('\ud800')", Pri = 2, Param = 19)]
        //[Variation(Desc = "XmlWriter.WriteName('\ud800')", Pri = 2, Param = 20)]
        //[Variation(Desc = "XmlWriter.WriteNmToken('\ud800')", Pri = 2, Param = 21)]
        //[Variation(Desc = "XmlWriter.WriteProcessingInstruction('\ud800', '\ud800')", Pri = 2, Param = 22)]
        //[Variation(Desc = "XmlWriter.WriteQualifiedName('\ud800', '\ud800')", Pri = 2, Param = 23)]
        //[Variation(Desc = "XmlWriter.WriteStartAttribute('\ud800')", Pri = 2, Param = 24)]
        //[Variation(Desc = "XmlWriter.WriteStartAttribute('\ud800', '\ud800')", Pri = 2, Param = 25)]
        //[Variation(Desc = "XmlWriter.WriteStartAttribute('a', '\ud800', '\ud800')", Pri = 2, Param = 26)]
        //[Variation(Desc = "XmlWriter.WriteStartElement('\ud800')", Pri = 2, Param = 27)]
        //[Variation(Desc = "XmlWriter.WriteStartElement('\ud800', '\ud800')", Pri = 2, Param = 28)]
        //[Variation(Desc = "XmlWriter.WriteStartElement('a', '\ud800', '\ud800')", Pri = 2, Param = 29)]
        //[Variation(Desc = "XmlWriter.WriteDocType('a', '\ud800', '\ud800', '\ud800')", Pri = 2, Param = 30)]
        //[Variation(Desc = "XmlWriter.WriteDocType('a', 'b', '\ud800', '\ud800')", Pri = 2, Param = 31)]
        //[Variation(Desc = "XmlWriter.WriteDocType('a', 'b', 'c', '\ud800')", Pri = 2, Param = 32)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString('a', '\ud800')", Pri = 2, Param = 33)]
        //[Variation(Desc = "XmlWriter.WriteElementString('a', '\ud800')", Pri = 2, Param = 34)]
        //[Variation(Desc = "XmlWriter.WriteProcessingInstruction('a', '\ud800')", Pri = 2, Param = 35)]
        //[Variation(Desc = "XmlWriter.WriteQualifiedName('a', '\ud800')", Pri = 2, Param = 36)]
        public int var_07()
        {
            int param = (int)CurVariation.Param;
            bool result = false;
            int[] skipParams = new int[] { 14, 20, 21 };

            XmlWriter w = CreateWriter();
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
                catch (InvalidOperationException) { return TEST_PASS; }
                catch (ArgumentException) { return TEST_PASS; }
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
                catch (XmlException) { return (param == 14) ? TEST_PASS : TEST_FAIL; }
                catch (InvalidOperationException) { return TEST_FAIL; }
            }
            finally
            {
                try
                {
                    w.Dispose();
                }
                catch (ArgumentException) { result = true; }
            }
            return (result || (WriterType == WriterType.CharCheckingWriter && skipParams.Contains(param))) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriter:WriteChars with wrong size throws wrong exception", Pri = 2, Param = 1)]
        //[Variation(Desc = "XmlWriter:WriteRaw with wrong size throws wrong exception", Pri = 2, Param = 2)]
        //[Variation(Desc = "XmlWriter:WriteBinHex with wrong size throws wrong exception", Pri = 2, Param = 3)]
        //[Variation(Desc = "XmlWriter:WriteBase64 with wrong size throws wrong exception", Pri = 2, Param = 4)]
        public int var_10()
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
            using (XmlWriter w = CreateWriter(ws))
            {
                try
                {
                    switch ((int)CurVariation.Param)
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
                        switch ((int)CurVariation.Param)
                        {
                            case 1: w.WriteChars(charBuffer, iIndex, iCount); break;
                            case 2: w.WriteRaw(charBuffer, iIndex, iCount); break;
                            case 3: w.WriteBinHex(byteBuffer, iIndex, iCount); break;
                            case 4: w.WriteBase64(byteBuffer, iIndex, iCount); break;
                        }
                    }
                    catch (ArgumentOutOfRangeException) { return TEST_PASS; }
                    catch (InvalidOperationException) { return TEST_PASS; }
                }
                catch (IndexOutOfRangeException)
                {
                    try
                    {
                        switch ((int)CurVariation.Param)
                        {
                            case 1: w.WriteChars(charBuffer, iIndex, iCount); break;
                        }
                    }
                    catch (IndexOutOfRangeException) { return (WriterType == WriterType.CharCheckingWriter) ? TEST_PASS : TEST_FAIL; }
                }
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriterSettings.ConformanceLevel - invalid values", Pri = 2, Param = 1)]
        //[Variation(Desc = "XmlWriterSettings.NewLineHandling - invalid values", Pri = 2, Param = 2)]
        //[Variation(Desc = "XmlWriterSettings.ConformanceLevel - invalid values", Pri = 2, Param = 3)]
        //[Variation(Desc = "XmlWriterSettings.NewLineHandling - invalid values", Pri = 2, Param = 4)]
        public int var_11()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                switch ((int)CurVariation.Param)
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
                    switch ((int)CurVariation.Param)
                    {
                        case 1: ws.ConformanceLevel = (ConformanceLevel)555; break;
                        case 2: ws.NewLineHandling = (NewLineHandling)555; break;
                        case 3: ws.ConformanceLevel = (ConformanceLevel)(-1); break;
                        case 4: ws.NewLineHandling = (NewLineHandling)(-1); break;
                    }
                }
                catch (ArgumentOutOfRangeException) { return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriter.Create((Stream))", Pri = 2, Param = 1)]
        //[Variation(Desc = "XmlWriter.Create((TextWriter))", Pri = 2, Param = 2)]
        //[Variation(Desc = "XmlWriter.Create((StringBuilder)l)", Pri = 2, Param = 3)]
        //[Variation(Desc = "XmlWriter.Create((XmlWriter))", Pri = 2, Param = 4)]
        //[Variation(Desc = "XmlWriter.Create((Stream), ws)", Pri = 2, Param = 5)]
        //[Variation(Desc = "XmlWriter.Create((TextWriter), ws)", Pri = 2, Param = 6)]
        //[Variation(Desc = "XmlWriter.Create((StringBuilder), ws)", Pri = 2, Param = 7)]
        //[Variation(Desc = "XmlWriter.Create((XmlWriter), ws)", Pri = 2, Param = 8)]
        public int var_12()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            TextWriter stringWriter = new StringWriter();
            int param = (int)CurVariation.Param;

            switch (param)
            {
                case 1: XmlWriter w1 = WriterHelper.Create(stringWriter); break;
                case 2: XmlWriter w2 = WriterHelper.Create(stringWriter); break;
                case 3: XmlWriter w3 = WriterHelper.Create(new StringBuilder()); break;
                case 4: XmlWriter w4 = WriterHelper.Create(WriterHelper.Create(stringWriter)); break;
                case 5: XmlWriter w5 = WriterHelper.Create(stringWriter, ws); break;
                case 6: XmlWriter w6 = WriterHelper.Create(stringWriter, ws); break;
                case 7: XmlWriter w7 = WriterHelper.Create(new StringBuilder(), ws); break;
                case 8: XmlWriter w8 = WriterHelper.Create(WriterHelper.Create(stringWriter), ws); break;
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "XmlWriter.WriteComment(String.Empty)", Pri = 2, Param = 1)]           
        //[Variation(Desc = "XmlWriter.WriteCData(String.Empty)", Pri = 2, Param = 2)]
        //[Variation(Desc = "XmlWriter.WriteEntityRef(String.Empty)", Pri = 2, Param = 3)]
        //[Variation(Desc = "XmlWriter.WriteRaw(String.Empty)", Pri = 2, Param = 4)]
        //[Variation(Desc = "XmlWriter.WriteString(String.Empty)", Pri = 2, Param = 5)]
        //[Variation(Desc = "XmlWriter.WriteValue(String.Empty)", Pri = 2, Param = 6)]
        //[Variation(Desc = "XmlWriter.WriteWhitespace(String.Empty)", Pri = 2, Param = 7)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString(String.Empty)", Pri = 2, Param = 8)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString(String.Empty, String.Empty, String.Empty)", Pri = 2, Param = 9)]
        //[Variation(Desc = "XmlWriter.WriteAttributeString(String.Empty, String.Empty, 'String.Empty, String.Empty)", Pri = 2, Param = 10)]          
        //[Variation(Desc = "XmlWriter.WriteDocType(String.Empty, String.Empty, String.Empty, String.Empty)", Pri = 2, Param = 11)]
        //[Variation(Desc = "XmlWriter.WriteElementString(String.Empty, String.Empty)", Pri = 2, Param = 12)]
        //[Variation(Desc = "XmlWriter.WriteElementString(String.Empty, String.Empty, String.Empty)", Pri = 2, Param = 13)]
        //[Variation(Desc = "XmlWriter.WriteElementString(String.Empty, String.Empty, String.Empty, String.Empty)", Pri = 2, Param = 14)]
        //[Variation(Desc = "XmlWriter.WriteEntityRef(String.Empty)", Pri = 2, Param = 15)]
        //[Variation(Desc = "XmlWriter.WriteName(String.Empty)", Pri = 2, Param = 16)]
        //[Variation(Desc = "XmlWriter.WriteNmToken(String.Empty)", Pri = 2, Param = 17)]
        //[Variation(Desc = "XmlWriter.WriteProcessingInstruction(String.Empty, String.Empty)", Pri = 2, Param = 18)]
        //[Variation(Desc = "XmlWriter.WriteQualifiedName(String.Empty, String.Empty)", Pri = 2, Param = 19)]
        //[Variation(Desc = "XmlWriter.WriteStartAttribute(String.Empty)", Pri = 2, Param = 20)]
        //[Variation(Desc = "XmlWriter.WriteStartAttribute(String.Empty, String.Empty)", Pri = 2, Param = 21)]
        //[Variation(Desc = "XmlWriter.WriteStartAttribute(String.Empty, String.Empty, String.Empty)", Pri = 2, Param = 22)]
        //[Variation(Desc = "XmlWriter.WriteStartElement(String.Empty)", Pri = 2, Param = 23)]
        //[Variation(Desc = "XmlWriter.WriteStartElement(String.Empty, String.Empty)", Pri = 2, Param = 24)]
        //[Variation(Desc = "XmlWriter.WriteStartElement(String.Empty, String.Empty, String.Empty)", Pri = 2, Param = 25)]
        //[Variation(Desc = "XmlWriter.WriteDocType(String.Empty, String.Empty, String.Empty, String.Empty)", Pri = 2, Param = 26)]
        //[Variation(Desc = "XmlWriter.WriteProcessingInstruction(String.Empty, String.Empty)", Pri = 2, Param = 27)]
        public int var_13()
        {
            int param = (int)CurVariation.Param;
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.ConformanceLevel = ConformanceLevel.Document;
            XmlWriter w = CreateWriter(ws);
            bool result = false;
            if (param != 30 && param != 31 && param != 32)
                w.WriteStartElement("Root");
            switch (param)
            {
                case 1: w.WriteComment(String.Empty); result = true; break;
                case 2: w.WriteCData(String.Empty); result = true; break;
                case 4: w.WriteRaw(String.Empty); result = true; break;
                case 5: w.WriteString(String.Empty); result = true; break;
                case 6: w.WriteValue(String.Empty); result = true; break;
                case 7: w.WriteWhitespace(String.Empty); result = true; break;
            }
            try
            {
                switch (param)
                {
                    case 3: w.WriteEntityRef(String.Empty); break;
                    case 8: w.WriteAttributeString(String.Empty, String.Empty); break;
                    case 9: w.WriteAttributeString(String.Empty, String.Empty, String.Empty); break;
                    case 10: w.WriteAttributeString(String.Empty, String.Empty, String.Empty, String.Empty); break;
                    case 11: w.WriteDocType(String.Empty, String.Empty, String.Empty, String.Empty); break;
                    case 12: w.WriteElementString(String.Empty, String.Empty); break;
                    case 13: w.WriteElementString(String.Empty, String.Empty, String.Empty); break;
                    case 14: w.WriteElementString(String.Empty, String.Empty, String.Empty, String.Empty); break;
                    case 15: w.WriteEntityRef(String.Empty); break;
                    case 16: w.WriteName(String.Empty); break;
                    case 17: w.WriteNmToken(String.Empty); break;
                    case 18: w.WriteProcessingInstruction(String.Empty, String.Empty); break;
                    case 19: w.WriteQualifiedName(String.Empty, String.Empty); break;
                    case 20: w.WriteStartAttribute(String.Empty); break;
                    case 21: w.WriteStartAttribute(String.Empty, String.Empty); break;
                    case 22: w.WriteStartAttribute(String.Empty, String.Empty, String.Empty); break;
                    case 23: w.WriteStartElement(String.Empty); break;
                    case 24: w.WriteStartElement(String.Empty, String.Empty); break;
                    case 25: w.WriteStartElement(String.Empty, String.Empty, String.Empty); break;
                    case 26: w.WriteDocType(String.Empty, String.Empty, String.Empty, String.Empty); break;
                    case 27: w.WriteProcessingInstruction(String.Empty, String.Empty); break;
                }
            }
            catch (ArgumentException)
            {
                try
                {
                    switch (param)
                    {
                        case 3: w.WriteEntityRef(String.Empty); break;
                        case 8: w.WriteAttributeString(String.Empty, String.Empty); break;
                        case 9: w.WriteAttributeString(String.Empty, String.Empty, String.Empty); break;
                        case 10: w.WriteAttributeString(String.Empty, String.Empty, String.Empty, String.Empty); break;
                        case 11: w.WriteDocType(String.Empty, String.Empty, String.Empty, String.Empty); break;
                        case 12: w.WriteElementString(String.Empty, String.Empty); break;
                        case 13: w.WriteElementString(String.Empty, String.Empty, String.Empty); break;
                        case 14: w.WriteElementString(String.Empty, String.Empty, String.Empty, String.Empty); break;
                        case 15: w.WriteEntityRef(String.Empty); break;
                        case 16: w.WriteName(String.Empty); break;
                        case 17: w.WriteNmToken(String.Empty); break;
                        case 18: w.WriteProcessingInstruction(String.Empty, String.Empty); break;
                        case 19: w.WriteQualifiedName(String.Empty, String.Empty); break;
                        case 20: w.WriteStartAttribute(String.Empty); break;
                        case 21: w.WriteStartAttribute(String.Empty, String.Empty); break;
                        case 22: w.WriteStartAttribute(String.Empty, String.Empty, String.Empty); break;
                        case 23: w.WriteStartElement(String.Empty); break;
                        case 24: w.WriteStartElement(String.Empty, String.Empty); break;
                        case 25: w.WriteStartElement(String.Empty, String.Empty, String.Empty); break;
                        case 26: w.WriteDocType(String.Empty, String.Empty, String.Empty, String.Empty); break;
                        case 27: w.WriteProcessingInstruction(String.Empty, String.Empty); break;
                    }
                }
                catch (ArgumentException) { result = true; }
            }
            finally
            {
                w.Dispose();
            }
            return (result || param == 19 && WriterType == WriterType.CustomWriter) ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriterSettings.IndentChars - invalid values", Pri = 2, Param = 1)]
        //[Variation(Desc = "XmlWriterSettings.NewLineChars - invalid values", Pri = 2, Param = 2)]
        public int var_14()
        {
            XmlWriterSettings ws = new XmlWriterSettings();
            try
            {
                switch ((int)CurVariation.Param)
                {
                    case 1: ws.IndentChars = null; break;
                    case 2: ws.NewLineChars = null; break;
                }
            }
            catch (ArgumentNullException)
            {
                try
                {
                    switch ((int)CurVariation.Param)
                    {
                        case 1: ws.IndentChars = null; break;
                        case 2: ws.NewLineChars = null; break;
                    }
                }
                catch (ArgumentNullException) { return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriter properties after Dispose", Pri = 2)]
        public int var_15()
        {
            XmlWriter w = CreateWriter();
            bool isUnicode = (WriterType == WriterType.UnicodeWriter || WriterType == WriterType.UnicodeWriterIndent) ? true : false;
            bool isIndent = (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent) ? true : false;

            w.WriteElementString("a", "b");
            ((IDisposable)w).Dispose();
            ((IDisposable)w).Dispose();
            ((IDisposable)w).Dispose();
            CError.Compare(w.LookupPrefix(""), String.Empty, "LookupPrefix");
            CError.Compare(w.WriteState, WriteState.Closed, "WriteState");
            CError.Compare(w.XmlLang, null, "XmlLang");
            CError.Compare(w.XmlSpace, XmlSpace.None, "XmlSpace");
            if (WriterType != WriterType.CustomWriter)
            {
                CError.Compare(w.Settings.CheckCharacters, true, "CheckCharacters");
                CError.Compare(w.Settings.CloseOutput, false, "CloseOutput");
                CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "ConformanceLevel");
                CError.Compare(w.Settings.Indent, (isIndent) ? true : false, "Indent");
                CError.Compare(w.Settings.IndentChars, "  ", "IndentChars");
                CError.Compare(w.Settings.NewLineChars, nl, "NewLineChars");
                CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "NewLineHandling");
                CError.Compare(w.Settings.NewLineOnAttributes, false, "NewLineOnAttributes");
                CError.Compare(w.Settings.OmitXmlDeclaration, true, "OmitXmlDeclaration");
                CError.Compare(w.Settings.Encoding.WebName, (isUnicode) ? "utf-16" : "utf-8", "Encoding");
            }
            return TEST_PASS;
        }

        //[Variation(Desc = "XmlWriter properties after Error", Pri = 2)]
        public int var_16()
        {
            XmlWriter w = CreateWriter();
            bool isUnicode = (WriterType == WriterType.UnicodeWriter || WriterType == WriterType.UnicodeWriterIndent) ? true : false;
            bool isIndent = (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent) ? true : false;

            w.WriteElementString("a", "b");
            try
            {
                w.WriteDocType("a", "b", "c", "d");
            }
            catch (InvalidOperationException)
            {
                CError.Compare(w.LookupPrefix(""), String.Empty, "LookupPrefix");
                CError.Compare(w.WriteState, WriteState.Error, "WriteState");
                CError.Compare(w.XmlLang, null, "XmlLang");
                CError.Compare(w.XmlSpace, XmlSpace.None, "XmlSpace");
                if (WriterType != WriterType.CustomWriter)
                {
                    CError.Compare(w.Settings.CheckCharacters, true, "CheckCharacters");
                    CError.Compare(w.Settings.CloseOutput, false, "CloseOutput");
                    CError.Compare(w.Settings.ConformanceLevel, ConformanceLevel.Document, "ConformanceLevel");
                    CError.Compare(w.Settings.Indent, (isIndent) ? true : false, "Indent");
                    CError.Compare(w.Settings.IndentChars, "  ", "IndentChars");
                    CError.Compare(w.Settings.NewLineChars, nl, "NewLineChars");
                    CError.Compare(w.Settings.NewLineHandling, NewLineHandling.Replace, "NewLineHandling");
                    CError.Compare(w.Settings.NewLineOnAttributes, false, "NewLineOnAttributes");
                    CError.Compare(w.Settings.OmitXmlDeclaration, true, "OmitXmlDeclaration");
                    CError.Compare(w.Settings.Encoding.WebName, (isUnicode) ? "utf-16" : "utf-8", "Encoding");
                }
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "XmlWriter.WriteStartElement() should inspect attributes before emitting the element tag", Pri = 2)]
        public int bug601305()
        {
            CError.WriteLine("expected:");
            CError.WriteLine("<p:root xmlns:p='uri' />");
            CError.WriteLine("actual:");
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true;
            StringWriter sw = new StringWriter();
            using (XmlWriter w = WriterHelper.Create(sw, ws))
            {
                w.WriteStartElement("root", "uri");
                w.WriteStartAttribute("xmlns", "p", "http://www.w3.org/2000/xmlns/");
                w.WriteString("uri");
            }
            CError.Compare(sw.ToString(), "<root xmlns:p=\"uri\" xmlns=\"uri\" />", "writer output");
            return TEST_PASS;
        }

        //[Variation(Desc = "writer.Settings.CheckCharacters - readonly", Pri = 2, Param = 1)]
        //[Variation(Desc = "writer.Settings.CloseOutput - readonly", Pri = 2, Param = 2)]
        //[Variation(Desc = "writer.Settings.ConformanceLevel - readonly", Pri = 2, Param = 3)]
        //[Variation(Desc = "writer.Settings.Encoding - readonly", Pri = 2, Param = 4)]
        //[Variation(Desc = "writer.Settings.Indent - readonly", Pri = 2, Param = 5)]
        //[Variation(Desc = "writer.Settings.IndentChars - readonly", Pri = 2, Param = 6)]
        //[Variation(Desc = "writer.Settings.NewLineChars - readonly", Pri = 2, Param = 7)]
        //[Variation(Desc = "writer.Settings.NewLineHandling - readonly", Pri = 2, Param = 8)]
        //[Variation(Desc = "writer.Settings.NewLineOnAttributes - readonly", Pri = 2, Param = 9)]
        //[Variation(Desc = "writer.Settings.OmitXmlDeclaration - readonly", Pri = 2, Param = 10)]
        public int var17()
        {
            if (WriterType == WriterType.CustomWriter) return TEST_PASS;
            int param = (int)CurVariation.Param;
            XmlWriter writer = CreateWriter();
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
                catch (XmlException) { return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "WriteQualifiedName after close", Pri = 2, Param = 1)]
        //[Variation(Desc = "WriteAttributes after close", Pri = 2, Param = 2)]
        //[Variation(Desc = "WriteAttributeString after close", Pri = 2, Param = 3)]
        //[Variation(Desc = "WriteBase64 after close", Pri = 2, Param = 4)]
        //[Variation(Desc = "WriteBinHex after close", Pri = 2, Param = 5)]
        //[Variation(Desc = "WriteCData after close", Pri = 2, Param = 6)]
        //[Variation(Desc = "WriteCharEntity after close", Pri = 2, Param = 7)]
        //[Variation(Desc = "WriteChars after close", Pri = 2, Param = 8)]
        //[Variation(Desc = "WriteComment after close", Pri = 2, Param = 9)]
        //[Variation(Desc = "WriteDocType after close", Pri = 2, Param = 10)]
        //[Variation(Desc = "WriteElementString after close", Pri = 2, Param = 11)]
        //[Variation(Desc = "WriteEndAttribute after close", Pri = 2, Param = 12)]
        //[Variation(Desc = "WriteEndDocument after close", Pri = 2, Param = 13)]
        //[Variation(Desc = "WriteEndElement after close", Pri = 2, Param = 14)]
        //[Variation(Desc = "WriteEntityRef after close", Pri = 2, Param = 15)]
        //[Variation(Desc = "WriteFullEndElement after close", Pri = 2, Param = 16)]
        //[Variation(Desc = "WriteName after close", Pri = 2, Param = 17)]
        //[Variation(Desc = "WriteNmToken after close", Pri = 2, Param = 18)]
        //[Variation(Desc = "WriteNode after close", Pri = 2, Param = 19)]
        //[Variation(Desc = "WriteProcessingInstruction after close", Pri = 2, Param = 20)]
        //[Variation(Desc = "WriteRaw(string) after close", Pri = 2, Param = 21)]
        //[Variation(Desc = "WriteRaw(buffer) after close", Pri = 2, Param = 22)]
        //[Variation(Desc = "WriteStartAttribute after close", Pri = 2, Param = 23)]
        //[Variation(Desc = "WriteStartDocument after close", Pri = 2, Param = 24)]
        //[Variation(Desc = "WriteStartElement after close", Pri = 2, Param = 25)]
        //[Variation(Desc = "WriteString after close", Pri = 2, Param = 26)]
        //[Variation(Desc = "WriteSurrogateCharEntity after close", Pri = 2, Param = 27)]
        //[Variation(Desc = "WriteValue after close", Pri = 2, Param = 28)]
        //[Variation(Desc = "WriteWhitespace after close", Pri = 2, Param = 29)]            
        public int var_18()
        {
            int param = (int)CurVariation.Param;
            XmlReader r = ReaderHelper.Create(new StringReader("<xmlns/>"));
            byte[] buffer = new byte[10];
            char[] chbuffer = new char[10];
            XmlWriter w = CreateWriter();
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
                    case 7: w.WriteCharEntity(Char.MaxValue); break;
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
                        case 7: w.WriteCharEntity(Char.MaxValue); break;
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
                catch (InvalidOperationException) { return TEST_PASS; }
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
                catch (ArgumentException) { return TEST_PASS; }
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
                catch (XmlException) { return TEST_PASS; }
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "WriteQualifiedName after error", Pri = 2, Param = 1)]
        //[Variation(Desc = "WriteAttributes after error", Pri = 2, Param = 2)]
        //[Variation(Desc = "WriteAttributeString after error", Pri = 2, Param = 3)]
        //[Variation(Desc = "WriteBase64 after error", Pri = 2, Param = 4)]
        //[Variation(Desc = "WriteBinHex after error", Pri = 2, Param = 5)]
        //[Variation(Desc = "WriteCData after error", Pri = 2, Param = 6)]
        //[Variation(Desc = "WriteCharEntity after error", Pri = 2, Param = 7)]
        //[Variation(Desc = "WriteChars after error", Pri = 2, Param = 8)]
        //[Variation(Desc = "WriteComment after error", Pri = 2, Param = 9)]
        //[Variation(Desc = "WriteDocType after error", Pri = 2, Param = 10)]
        //[Variation(Desc = "WriteElementString after error", Pri = 2, Param = 11)]
        //[Variation(Desc = "WriteEndAttribute after error", Pri = 2, Param = 12)]
        //[Variation(Desc = "WriteEndDocument after error", Pri = 2, Param = 13)]
        //[Variation(Desc = "WriteEndElement after error", Pri = 2, Param = 14)]
        //[Variation(Desc = "WriteEntityRef after error", Pri = 2, Param = 15)]
        //[Variation(Desc = "WriteFullEndElement after error", Pri = 2, Param = 16)]
        //[Variation(Desc = "WriteName after error", Pri = 2, Param = 17)]
        //[Variation(Desc = "WriteNmToken after error", Pri = 2, Param = 18)]
        //[Variation(Desc = "WriteNode after error", Pri = 2, Param = 19)]
        //[Variation(Desc = "WriteProcessingInstruction after error", Pri = 2, Param = 20)]
        //[Variation(Desc = "WriteRaw(string) after error", Pri = 2, Param = 21)]
        //[Variation(Desc = "WriteRaw(buffer) after error", Pri = 2, Param = 22)]
        //[Variation(Desc = "WriteStartAttribute after error", Pri = 2, Param = 23)]
        //[Variation(Desc = "WriteStartDocument after error", Pri = 2, Param = 24)]
        //[Variation(Desc = "WriteStartElement after error", Pri = 2, Param = 25)]
        //[Variation(Desc = "WriteString after error", Pri = 2, Param = 26)]
        //[Variation(Desc = "WriteSurrogateCharEntity after error", Pri = 2, Param = 27)]
        //[Variation(Desc = "WriteValue after error", Pri = 2, Param = 28)]
        //[Variation(Desc = "WriteWhitespace after error", Pri = 2, Param = 29)]
        public int var_19()
        {
            int param = (int)CurVariation.Param;
            XmlReader r = ReaderHelper.Create(new StringReader("<xmlns/>"));
            byte[] buffer = new byte[10];
            char[] chbuffer = new char[10];
            XmlWriter w = CreateWriter();
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
                        case 7: w.WriteCharEntity(Char.MaxValue); break;
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
                            case 7: w.WriteCharEntity(Char.MaxValue); break;
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
                    catch (InvalidOperationException) { return TEST_PASS; }
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
                    catch (ArgumentException) { return TEST_PASS; }
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
                    catch (XmlException) { return TEST_PASS; }
                }
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "Assert when write attribute value with invalid surrogate pair and encoding ASCII")]
        public int var_20()
        {
            XmlWriter w = CreateWriter();
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
                catch (InvalidOperationException ioe) { CError.WriteLine(ioe); return TEST_PASS; }
                catch (ArgumentException ae) { CError.WriteLine(ae); return TEST_PASS; }
            }
            finally
            {
                w.Dispose();
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "WriteValue attr swap surrogate pair", Param = 1)]
        //[Variation(Desc = "WriteComment attr swap surrogate pair", Param = 2)]
        //[Variation(Desc = "WriteCData attr swap surrogate pair", Param = 3)]
        //[Variation(Desc = "WriteProcessingInstruction attr swap surrogate pair", Param = 4)]
        //[Variation(Desc = "WriteRaw attr swap surrogate pair", Param = 5)]
        //[Variation(Desc = "WriteValue elem swap surrogate pair", Param = 6)]
        //[Variation(Desc = "WriteComment elem swap surrogate pair", Param = 7)]
        //[Variation(Desc = "WriteCData elem swap surrogate pair", Param = 8)]
        //[Variation(Desc = "WriteProcessingInstruction elem swap surrogate pair", Param = 9)]
        //[Variation(Desc = "WriteRaw elem swap surrogate pair", Param = 10)]
        //[Variation(Desc = "WriteAttributeString swap surrogate pair", Param = 11)]
        //[Variation(Desc = "WriteCharEntity swap surrogate pair", Param = 12)]
        //[Variation(Desc = "WriteDocType pubid swap surrogate pair", Param = 13)]
        //[Variation(Desc = "WriteDocType sysid swap surrogate pair", Param = 14)]
        //[Variation(Desc = "WriteDocType subset swap surrogate pair", Param = 15)]
        //[Variation(Desc = "WriteElementString1 swap surrogate pair", Param = 16)]
        //[Variation(Desc = "WriteElementString2 swap surrogate pair", Param = 17)]
        //[Variation(Desc = "WriteElementString3 swap surrogate pair", Param = 18)]
        //[Variation(Desc = "WriteElementString4 swap surrogate pair", Param = 19)]
        //[Variation(Desc = "WriteEntityRef swap surrogate pair", Param = 20)]
        //[Variation(Desc = "WriteName swap surrogate pair", Param = 21)]
        //[Variation(Desc = "WriteNmToken swap surrogate pair", Param = 22)]
        //[Variation(Desc = "WriteQualifiedName1 swap surrogate pair", Param = 23)]
        //[Variation(Desc = "WriteQualifiedName2 swap surrogate pair", Param = 24)]
        //[Variation(Desc = "WriteStartAttribute1 swap surrogate pair", Param = 25)]
        //[Variation(Desc = "WriteStartAttribute2 swap surrogate pair", Param = 26)]
        //[Variation(Desc = "WriteStartAttribute3 swap surrogate pair", Param = 27)]
        //[Variation(Desc = "WriteStartElemen1 swap surrogate pair", Param = 28)]
        //[Variation(Desc = "WriteStartElemen2 swap surrogate pair", Param = 29)]
        //[Variation(Desc = "WriteStartElemen3 swap surrogate pair", Param = 30)]
        //[Variation(Desc = "WriteString swap surrogate pair", Param = 31)]
        //[Variation(Desc = "WriteWhitespace swap surrogate pair", Param = 32)]
        //[Variation(Desc = "WriteString attr swap surrogate pair", Param = 33)]
        //[Variation(Desc = "WriteSurrogateCharEntity swap surrogate pair", Param = 34)]
        public int var_21()
        {
            int param = (int)this.CurVariation.Param;
            bool result = false;
            string val = "\uDE34\uD9A2";
            XmlWriter w = CreateWriter();
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
                catch (XmlException) { result = (WriterType == WriterType.CharCheckingWriter && (param == 21 || param == 22)) ? true : false; }
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
            return result ? TEST_PASS : TEST_FAIL;
        }

        //[Variation(Desc = "System.Xml.XmlWriter::WriteAttributes", Param = 1)]
        //[Variation(Desc = "System.Xml.XmlWriter::WriteAttributes", Param = 2)]
        //[Variation(Desc = "System.Xml.XmlWriter::WriteAttributes", Param = 3)]
        //[Variation(Desc = "System.Xml.XmlWriter::WriteAttributes", Param = 4)]
        public int bug600541()
        {
            int param = (int)this.CurVariation.Param;
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
                using (XmlWriter w = CreateWriter())
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
                    return (CompareString(xml)) ? TEST_PASS : TEST_FAIL;
                }
            }
        }

        //[Variation(Desc = "Schema.XmlUntypedStringConverter::ToString")]
        public int bug630890()
        {
            object obj = (object)1;
            for (int i = 0; i < 100000; i++)
            {
                obj = new object[1] { obj };
            }

            using (XmlWriter w = CreateWriter())
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
                    catch (InvalidOperationException) { CError.WriteLine(e.Message); return TEST_PASS; }
                    catch (InvalidCastException) { CError.WriteLine(e.Message); return TEST_PASS; }
                }
            }
            return TEST_FAIL;
        }

        //[Variation(Desc = "XMLWriter.WriteValue fails with ArgumentNullException if passed an array which has null/empty items")]
        public int PassingArrayWithNullOrEmptyItemsCausesWriteValueToFail()
        {
            string[] a = new string[5];
            string exp = "<b>a a1 </b>";
            a[0] = "a";
            a[1] = "a1";
            a[3] = null;
            a[4] = "";

            using (XmlWriter w = CreateWriter())
            {
                w.WriteStartElement("b");
                w.WriteValue(a);
            }
            return (CompareString(exp)) ? TEST_PASS : TEST_FAIL;
        }
    }
}

