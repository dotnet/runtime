// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public abstract partial class XmlWriterTestCaseBase : CTestCase
    {
        public static string nl = Environment.NewLine;

        public XmlWriterTestCaseBase() : base() { }

        public override int Init(object o)
        {
            return base.Init(o);
        }

        public XmlWriterTestModule XmlWriterTestModule
        {
            get
            {
                return (XmlWriterTestModule)this.TestModule;
            }
        }

        public WriterType WriterType
        {
            get
            {
                return this.XmlWriterTestModule.WriterFactory.WriterType;
            }
        }

        public string BaselinePath
        {
            get
            {
                return this.XmlWriterTestModule.BaselinePath;
            }
        }

        public string FullPath(string fileName)
        {
            if (fileName == null || fileName == String.Empty)
                return fileName;
            return BaselinePath + fileName;
        }

        public virtual XmlWriter CreateWriter()
        {
            return this.XmlWriterTestModule.WriterFactory.CreateWriter();
        }

        public virtual XmlWriter CreateWriter(XmlWriterSettings s)
        {
            return this.XmlWriterTestModule.WriterFactory.CreateWriter(s);
        }

        public virtual XmlReader GetReader()
        {
            return this.XmlWriterTestModule.WriterFactory.GetReader();
        }

        public bool CompareReader(string strExpected)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.CheckCharacters = false;
            readerSettings.CloseInput = true;
            readerSettings.ConformanceLevel = ConformanceLevel.Auto;

            StringReader sr = new StringReader(strExpected);
            XmlReader xrExpected = XmlReader.Create(sr, readerSettings);
            return this.XmlWriterTestModule.WriterFactory.CompareReader(xrExpected);
        }

        public bool CompareString(string strExpected)
        {
            CError.WriteLine(this.XmlWriterTestModule.WriterFactory.GetString());
            if (strExpected.Contains("~"))
                return this.XmlWriterTestModule.WriterFactory.CompareStringWithPrefixes(strExpected);


            return this.XmlWriterTestModule.WriterFactory.CompareString(strExpected);
        }

        public string RemoveSpaceInDocType(string xml)
        {
            int docPos = xml.IndexOf("<!DOCTYPE");

            if (docPos < 0)
                return xml;

            int spacePos = xml.IndexOf(' ', docPos + "<!DOCTYPE".Length + 1);
            int subsetPos = xml.IndexOf('[', docPos + "<!DOCTYPE".Length + 1);
            int closePos = xml.IndexOf('>', docPos + "<!DOCTYPE".Length + 1);

            if (spacePos + 1 == subsetPos || spacePos + 1 == closePos)
                xml = xml.Remove(spacePos, 1);

            return xml;
        }

        public bool IsIndent()
        {
            return (WriterType == WriterType.UTF8WriterIndent || WriterType == WriterType.UnicodeWriterIndent) ? true : false;
        }

        public void CheckErrorState(WriteState ws)
        {
            if (WriterType == WriterType.CharCheckingWriter)
                return;
            CError.Compare(ws, WriteState.Error, "WriteState should be Error");
        }

        public void CheckElementState(WriteState ws)
        {
            CError.Compare(ws, WriteState.Element, "WriteState should be Element");
        }

        public bool CompareBaseline(string baselineFile)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.CloseInput = true;

            XmlReader xrExpected = XmlReader.Create(FilePathUtil.getStream(FullPath(baselineFile)), readerSettings);
            return this.XmlWriterTestModule.WriterFactory.CompareReader(xrExpected);
        }

        public bool CompareBaseline2(string baselineFile)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.CloseInput = true;

            XmlReader xrExpected = XmlReader.Create(FilePathUtil.getStream(baselineFile), readerSettings);
            return this.XmlWriterTestModule.WriterFactory.CompareReader(xrExpected);
        }

        public void RaiseError(String strErrorMsg)
        {
            Exception e = new Exception(strErrorMsg);
            throw (e);
        }
    }

    public abstract partial class XmlFactoryWriterTestCaseBase : XmlWriterTestCaseBase
    {
        public XmlFactoryWriterTestCaseBase() : base()
        {
        }

        public virtual string GetString()
        {
            return this.XmlWriterTestModule.WriterFactory.GetString();
        }

        public virtual XmlWriter CreateWriter(ConformanceLevel cl)
        {
            return this.XmlWriterTestModule.WriterFactory.CreateWriter(cl);
        }

        public override XmlWriter CreateWriter(XmlWriterSettings wSettings)
        {
            return this.XmlWriterTestModule.WriterFactory.CreateWriter(wSettings);
        }
    }

    public abstract partial class TCWriteBuffer : XmlWriterTestCaseBase
    {
        public int VerifyInvalidWrite(string methodName, int iBufferSize, int iIndex, int iCount, Type exceptionType)
        {
            byte[] byteBuffer = new byte[iBufferSize];
            for (int i = 0; i < iBufferSize; i++)
                byteBuffer[i] = (byte)(i + '0');

            char[] charBuffer = new char[iBufferSize];
            for (int i = 0; i < iBufferSize; i++)
                charBuffer[i] = (char)(i + '0');

            XmlWriter w = CreateWriter();
            w.WriteStartElement("root");
            try
            {
                switch (methodName)
                {
                    case "WriteBase64":
                        w.WriteBase64(byteBuffer, iIndex, iCount);
                        break;
                    case "WriteRaw":
                        w.WriteRaw(charBuffer, iIndex, iCount);
                        break;
                    case "WriteBinHex":
                        w.WriteBinHex(byteBuffer, iIndex, iCount);
                        break;
                    case "WriteChars":
                        w.WriteChars(charBuffer, iIndex, iCount);
                        break;
                    default:
                        CError.Compare(false, "Unexpected method name " + methodName);
                        break;
                }
            }
            catch (Exception e)
            {
                CError.WriteLineIgnore("Exception: " + e.ToString());
                if (exceptionType.FullName.Equals(e.GetType().FullName))
                {
                    return TEST_PASS;
                }
                else
                {
                    CError.WriteLine("Did not throw exception of type {0}", exceptionType);
                }
            }
            w.Flush();
            return TEST_FAIL;
        }

        public byte[] StringToByteArray(string src)
        {
            byte[] base64 = new byte[src.Length * 2];

            for (int i = 0; i < src.Length; i++)
            {
                byte[] temp = System.BitConverter.GetBytes(src[i]);
                base64[2 * i] = temp[0];
                base64[2 * i + 1] = temp[1];
            }
            return base64;
        }

        public static void ensureSpace(ref byte[] buffer, int len)
        {
            if (len >= buffer.Length)
            {
                int originalLen = buffer.Length;
                byte[] newBuffer = new byte[(int)(len * 2)];
                for (int i = 0; i < originalLen; newBuffer[i] = buffer[i++])
                {
                    // Intentionally Empty
                }
                buffer = newBuffer;
            }
        }
        public static void WriteToBuffer(ref byte[] destBuff, ref int len, byte srcByte)
        {
            ensureSpace(ref destBuff, len);
            destBuff[len++] = srcByte;
        }

        public static void WriteToBuffer(ref byte[] destBuff, ref int len, byte[] srcBuff)
        {
            int srcArrayLen = srcBuff.Length;
            WriteToBuffer(ref destBuff, ref len, srcBuff, 0, (int)srcArrayLen);
        }

        public static void WriteToBuffer(ref byte[] destBuff, ref int destStart, byte[] srcBuff, int srcStart, int count)
        {
            ensureSpace(ref destBuff, destStart + count - 1);
            for (int i = srcStart; i < srcStart + count; i++)
            {
                destBuff[destStart++] = srcBuff[i];
            }
        }

        public static void WriteToBuffer(ref byte[] destBuffer, ref int destBuffLen, String strValue)
        {
            for (int i = 0; i < strValue.Length; i++)
            {
                WriteToBuffer(ref destBuffer, ref destBuffLen, System.BitConverter.GetBytes(strValue[i]));
            }

            WriteToBuffer(ref destBuffer, ref destBuffLen, System.BitConverter.GetBytes('\0'));
        }
    }
}
