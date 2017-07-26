// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using Xunit;

namespace System.Xml.Tests
{
    public abstract class TCWriteBuffer
    {
        public void VerifyInvalidWrite(XmlWriterUtils utils, string methodName, int iBufferSize, int iIndex, int iCount, Type exceptionType)
        {
            byte[] byteBuffer = new byte[iBufferSize];
            for (int i = 0; i < iBufferSize; i++)
                byteBuffer[i] = (byte)(i + '0');

            char[] charBuffer = new char[iBufferSize];
            for (int i = 0; i < iBufferSize; i++)
                charBuffer[i] = (char)(i + '0');

            XmlWriter w = utils.CreateWriter();
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
                    return;
                }
                else
                {
                    CError.WriteLine("Did not throw exception of type {0}", exceptionType);
                }
            }
            w.Flush();
            Assert.True(false, "Expected exception");
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
