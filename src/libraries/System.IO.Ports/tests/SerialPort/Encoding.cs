// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class Encoding_Property : PortsTest
    {
        //The default number of bytes to read/write to verify the speed of the port
        //and that the bytes were transfered successfully
        private const int DEFAULT_CHAR_ARRAY_SIZE = 8;

        //The maximum time we will wait for all of encoded bytes to be received
        private const int MAX_WAIT_TIME = 1250;

        private enum ThrowAt { Set, Open };

        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default Encoding");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyEncoding(com1);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_ASCIIEncoding_BeforeOpen()
        {
            Debug.WriteLine("Verifying ASCIIEncoding Encoding before open");
            VerifyEncodingBeforeOpen(new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_UTF8Encoding_BeforeOpen()
        {
            Debug.WriteLine("Verifying UTF8Encoding Encoding before open");
            VerifyEncodingBeforeOpen(new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_UTF32Encoding_BeforeOpen()
        {
            Debug.WriteLine("Verifying UTF32Encoding Encoding before open");
            VerifyEncodingBeforeOpen(new UTF32Encoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_UnicodeEncoding_BeforeOpen()
        {
            Debug.WriteLine("Verifying UnicodeEncoding Encoding before open");
            VerifyEncodingBeforeOpen(new UnicodeEncoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_ASCIIEncoding_AfterOpen()
        {
            Debug.WriteLine("Verifying ASCIIEncoding Encoding after open");
            VerifyEncodingAfterOpen(new ASCIIEncoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_UTF8Encoding_AfterOpen()
        {
            Debug.WriteLine("Verifying UTF8Encoding Encoding after open");
            VerifyEncodingAfterOpen(new UTF8Encoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_UTF32Encoding_AfterOpen()
        {
            Debug.WriteLine("Verifying UTF32Encoding Encoding after open");
            VerifyEncodingAfterOpen(new UTF32Encoding());
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_UnicodeEncoding_AfterOpen()
        {
            Debug.WriteLine("Verifying UnicodeEncoding Encoding after open");
            VerifyEncodingAfterOpen(new UnicodeEncoding());
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Encoding_ISCIIAssemese()
        {
            Debug.WriteLine("Verifying ISCIIAssemese Encoding");
            VerifyException(Encoding.GetEncoding(57006), ThrowAt.Set, typeof(ArgumentException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Encoding_UTF7()
        {
            Debug.WriteLine("Verifying UTF7Encoding Encoding");
            VerifyException(LegacyUTF7Encoding, ThrowAt.Set, typeof(ArgumentException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Encoding_Null()
        {
            Debug.WriteLine("Verifying null Encoding");
            VerifyException(null, ThrowAt.Set, typeof(ArgumentNullException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_IBM_Latin1()
        {
            Debug.WriteLine("Verifying IBM Latin-1 Encoding before open");
            VerifyEncodingBeforeOpen(Encoding.GetEncoding(1047));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Encoding_Japanese_JIS()
        {
            Debug.WriteLine("Verifying Japanese (JIS) Encoding before open");
            VerifyException(Encoding.GetEncoding(50220), ThrowAt.Set, typeof(ArgumentException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Encoding_ChineseSimplified_GB18030()
        {
            Debug.WriteLine("Verifying Chinese Simplified (GB18030) Encoding before open");
            VerifyEncodingBeforeOpen(Encoding.GetEncoding(54936));
        }

        #endregion

        #region Verification for Test Cases
        private void VerifyException(Encoding encoding, ThrowAt throwAt, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, encoding, throwAt, expectedException);

                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, encoding, expectedException);
            }
        }

        private void VerifyExceptionAtOpen(SerialPort com, Encoding encoding, ThrowAt throwAt, Type expectedException)
        {
            Encoding origEncoding = com.Encoding;
            SerialPortProperties serPortProp = new SerialPortProperties();

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            if (ThrowAt.Open == throwAt)
                serPortProp.SetProperty("Encoding", encoding);

            try
            {
                com.Encoding = encoding;

                if (ThrowAt.Open == throwAt)
                    com.Open();

                _ = com.Encoding;

                com.Encoding = origEncoding;

                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);
            com.Encoding = origEncoding;
        }

        private void VerifyExceptionAfterOpen(SerialPort com, Encoding encoding, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.Encoding = encoding;

                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the Encoding after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the Encoding after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the Encoding after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);
        }


        private void VerifyEncodingBeforeOpen(Encoding encoding)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Encoding = encoding;
                com1.Open();
                serPortProp.SetProperty("Encoding", encoding);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyEncoding(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyEncodingAfterOpen(Encoding encoding)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com1.Encoding = encoding;
                serPortProp.SetProperty("Encoding", encoding);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyEncoding(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyEncoding(SerialPort com1)
        {
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                int origReadTimeout = com1.ReadTimeout;
                char[] xmitChars = TCSupport.GetRandomChars(DEFAULT_CHAR_ARRAY_SIZE, true);
                byte[] xmitBytes;
                char[] rcvChars = new char[DEFAULT_CHAR_ARRAY_SIZE];
                char[] expectedChars;
                int waitTime = 0;

                xmitBytes = com1.Encoding.GetBytes(xmitChars);
                expectedChars = com1.Encoding.GetChars(xmitBytes);

                com2.Open();
                com2.Encoding = com1.Encoding;

                com2.Write(xmitChars, 0, xmitChars.Length);

                //        for (int i=0; i<xmitChars.Length; ++i) {
                //            Debug.WriteLine("{0},", (int)xmitChars[i]);
                //        }

                while (com1.BytesToRead < xmitBytes.Length)
                {
                    Thread.Sleep(50);
                    waitTime += 50;

                    if (MAX_WAIT_TIME < waitTime)
                    {
                        Fail("ERROR!!! Expected BytesToRead={0} actual={1}", xmitBytes.Length, com1.BytesToRead);
                    }
                }

                com1.Read(rcvChars, 0, rcvChars.Length);

                Assert.Equal(expectedChars.Length, rcvChars.Length);

                for (int i = 0; i < rcvChars.Length; i++)
                {
                    Assert.Equal(expectedChars[i], rcvChars[i]);
                }
                com1.ReadTimeout = origReadTimeout;
            }
        }

        #endregion
    }
}
