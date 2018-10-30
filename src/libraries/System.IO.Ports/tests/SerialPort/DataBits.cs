// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class DataBits_Property : PortsTest
    {
        //The default number of bytes to read/write to verify the speed of the port
        //and that the bytes were transfered successfully
        private const int DEFAULT_BYTE_SIZE = 512;

        //If the percentage difference between the expected time to transfer with the specified dataBits
        //and the actual time found through Stopwatch is greater then 5% then the DataBits value was not correctly
        //set and the testcase fails.
        private const double MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE = .05;

        private const int NUM_TRYS = 5;

        private enum ThrowAt { Set, Open };

        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void DataBits_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default DataBits");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDataBits(com1, DEFAULT_BYTE_SIZE);


                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DataBits_7_BeforeOpen()
        {
            Debug.WriteLine("Verifying 7 DataBits before open");
            VerifyDataBitsBeforeOpen(7, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DataBits_8_BeforeOpen()
        {
            Debug.WriteLine("Verifying 8 DataBits before open");
            VerifyDataBitsBeforeOpen(8, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DataBits_7_AfterOpen()
        {
            Debug.WriteLine("Verifying 7 DataBits after open");
            VerifyDataBitsAfterOpen(7, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void DataBits_8_AfterOpen()
        {
            Debug.WriteLine("Verifying 8 DataBits after open");
            VerifyDataBitsAfterOpen(8, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DataBits_Int32MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue DataBits");
            VerifyException(int.MinValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DataBits_Neg8()
        {
            Debug.WriteLine("Verifying -8 DataBits");
            VerifyException(-8, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DataBits_Neg1()
        {
            Debug.WriteLine("Verifying -1 DataBits");
            VerifyException(-1, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DataBits_0()
        {
            Debug.WriteLine("Verifying 0 DataBits");
            VerifyException(0, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DataBits_1()
        {
            Debug.WriteLine("Verifying 1 DataBits");
            VerifyException(1, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DataBits_4()
        {
            Debug.WriteLine("Verifying 4 DataBits");
            VerifyException(4, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DataBits_9()
        {
            Debug.WriteLine("Verifying 9 DataBits");
            VerifyException(9, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DataBits_Int32MaxValue()
        {
            Debug.WriteLine("Verifying Int32.MaxValue DataBits");
            VerifyException(int.MaxValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        #endregion

        #region Verification for Test Cases
        private void VerifyException(int dataBits, ThrowAt throwAt, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, dataBits, throwAt, expectedException);
                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, dataBits, expectedException);
            }
        }


        private void VerifyExceptionAtOpen(SerialPort com, int dataBits, ThrowAt throwAt, Type expectedException)
        {
            int origDataBits = com.DataBits;
            SerialPortProperties serPortProp = new SerialPortProperties();

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            if (ThrowAt.Open == throwAt)
                serPortProp.SetProperty("DataBits", dataBits);

            try
            {
                com.DataBits = dataBits;

                if (ThrowAt.Open == throwAt)
                    com.Open();

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
            com.DataBits = origDataBits;
        }


        private void VerifyExceptionAfterOpen(SerialPort com, int dataBits, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.DataBits = dataBits;
                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the DataBits after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the DataBits after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the DataBits after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }
            serPortProp.VerifyPropertiesAndPrint(com);
        }

        private void VerifyDataBitsBeforeOpen(int dataBits, int numBytesToSend)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.DataBits = dataBits;
                com1.Open();

                serPortProp.SetProperty("DataBits", dataBits);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDataBits(com1, numBytesToSend);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyDataBitsAfterOpen(int dataBits, int numBytesToSend)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com1.DataBits = dataBits;

                serPortProp.SetProperty("DataBits", dataBits);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyDataBits(com1, numBytesToSend);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyDataBits(SerialPort com1, int numBytesToSend)
        {
            byte[] xmitBytes = new byte[numBytesToSend];
            byte[] expectedBytes = new byte[numBytesToSend];
            byte[] rcvBytes = new byte[numBytesToSend];
            Random rndGen = new Random();
            Stopwatch sw = new Stopwatch();
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                double expectedTime, actualTime, percentageDifference;
                int numBytes = 0;
                byte shiftMask = 0xFF;

                //Create a mask that when logicaly and'd with the transmitted byte will
                //will result in the byte recievied due to the leading bits being chopped
                //off due to DataBits less then 8
                shiftMask >>= 8 - com1.DataBits;

                //Generate some random bytes to read/write for this DataBits setting
                for (int i = 0; i < xmitBytes.Length; i++)
                {
                    xmitBytes[i] = (byte)rndGen.Next(0, 256);
                    expectedBytes[i] = (byte)(xmitBytes[i] & shiftMask);
                }

                com2.DataBits = com1.DataBits;
                com2.Open();

                actualTime = 0;

                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    IAsyncResult beginWriteResult;
                    int bytesToRead = 0;

                    com2.DiscardInBuffer();
                    beginWriteResult = com1.BaseStream.BeginWrite(xmitBytes, 0, xmitBytes.Length, null, null);
                    while (0 == (bytesToRead = com2.BytesToRead))
                    {
                    }

                    sw.Start();
                    while (numBytesToSend > com2.BytesToRead)
                    {
                        //Wait for all of the bytes to reach the input buffer of com2
                    }

                    sw.Stop();
                    actualTime += sw.ElapsedMilliseconds;
                    actualTime += ((bytesToRead * (2.0 + com1.DataBits)) / com1.BaudRate) * 1000;
                    com1.BaseStream.EndWrite(beginWriteResult);
                    sw.Reset();
                }

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                expectedTime = ((xmitBytes.Length * (2.0 + com1.DataBits)) / com1.BaudRate) * 1000;
                actualTime /= NUM_TRYS;
                percentageDifference = Math.Abs((expectedTime - actualTime) / expectedTime);

                //If the percentageDifference between the expected time and the actual time is to high
                //then the expected baud rate must not have been used and we should report an error
                if (MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE < percentageDifference)
                {
                    Fail("ERROR!!! DataBits not used Expected time:{0}, actual time:{1} percentageDifference:{2}", expectedTime, actualTime, percentageDifference, numBytes);
                }

                com2.Read(rcvBytes, 0, rcvBytes.Length);
            }

            //Verify that the bytes we sent were the same ones we received
            for (int i = 0; i < expectedBytes.Length; i++)
            {
                Assert.Equal(expectedBytes[i], rcvBytes[i]);
            }
        }
        #endregion
    }
}
