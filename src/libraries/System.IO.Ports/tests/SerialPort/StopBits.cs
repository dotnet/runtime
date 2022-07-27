// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    [KnownFailure]
    public class StopBits_Property : PortsTest
    {
        //The default ammount of time the a transfer should take at any given baud rate and stop bits combination.
        //The bytes sent should be adjusted to take this ammount of time to transfer at the specified baud rate and stop bits combination.
        private const int DEFAULT_TIME = 750;

        //If the percentage difference between the expected time to transfer with the specified stopBits
        //and the actual time found through Stopwatch is greater then 5% then the StopBits value was not correctly
        //set and the testcase fails.
        private const double MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE = .07;

        //The default number of databits to use when testing StopBits
        private const int DEFAULT_DATABITS = 8;
        private const int NUM_TRYS = 5;

        private enum ThrowAt { Set, Open };

        #region Test Cases
        [ConditionalFact(nameof(HasNullModem))]
        public void StopBits_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default StopBits");
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyStopBits(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void StopBits_1_BeforeOpen()
        {
            Debug.WriteLine("Verifying 1 StopBits before open");
            VerifyStopBitsBeforeOpen((int)StopBits.One);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void StopBits_2_BeforeOpen()
        {
            Debug.WriteLine("Verifying 2 StopBits before open");
            VerifyStopBitsBeforeOpen((int)StopBits.Two);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void StopBits_1_AfterOpen()
        {
            Debug.WriteLine("Verifying 1 StopBits after open");
            VerifyStopBitsAfterOpen((int)StopBits.One);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void StopBits_2_AfterOpen()
        {
            Debug.WriteLine("Verifying 2 StopBits after open");
            VerifyStopBitsAfterOpen((int)StopBits.Two);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void StopBits_Int32MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue StopBits");
            VerifyException(int.MinValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void StopBits_Neg1()
        {
            Debug.WriteLine("Verifying -1 StopBits");
            VerifyException(-1, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void StopBits_0()
        {
            Debug.WriteLine("Verifying 0 StopBits");
            VerifyException(0, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void StopBits_4()
        {
            Debug.WriteLine("Verifying 4 StopBits");
            VerifyException(4, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void StopBits_Int32MaxValue()
        {
            Debug.WriteLine("Verifying Int32.MaxValue StopBits");
            VerifyException(int.MaxValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        #endregion

        #region Verification for Test Cases
        private void VerifyException(int stopBits, ThrowAt throwAt, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, stopBits, throwAt, expectedException);

                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, stopBits, expectedException);
            }
        }


        private void VerifyExceptionAtOpen(SerialPort com, int stopBits, ThrowAt throwAt, Type expectedException)
        {
            int origStopBits = (int)com.StopBits;
            SerialPortProperties serPortProp = new SerialPortProperties();

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            if (ThrowAt.Open == throwAt)
                serPortProp.SetProperty("StopBits", (StopBits)stopBits);

            try
            {
                com.StopBits = (StopBits)stopBits;
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
            com.StopBits = (StopBits)origStopBits;
        }


        private void VerifyExceptionAfterOpen(SerialPort com, int stopBits, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.StopBits = (StopBits)stopBits;

                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the StopBits after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the StopBits after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the StopBits after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }
            serPortProp.VerifyPropertiesAndPrint(com);
        }

        private void VerifyStopBitsBeforeOpen(int stopBits)
        {
            VerifyStopBitsBeforeOpen(stopBits, DEFAULT_DATABITS);
        }


        private void VerifyStopBitsBeforeOpen(int stopBits, int dataBits)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.DataBits = dataBits;
                com1.StopBits = (StopBits)stopBits;
                com1.Open();

                serPortProp.SetProperty("DataBits", dataBits);
                serPortProp.SetProperty("StopBits", (StopBits)stopBits);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyStopBits(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        private void VerifyStopBitsAfterOpen(int stopBits)
        {
            VerifyStopBitsAfterOpen(stopBits, DEFAULT_DATABITS);
        }


        private void VerifyStopBitsAfterOpen(int stopBits, int dataBits)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com1.DataBits = dataBits;
                com1.StopBits = (StopBits)stopBits;

                serPortProp.SetProperty("DataBits", dataBits);
                serPortProp.SetProperty("StopBits", (StopBits)stopBits);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyStopBits(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);

                if (com1.IsOpen)
                    com1.Close();
            }
        }


        private void VerifyStopBits(SerialPort com1)
        {
            Random rndGen = new Random(-55);
            Stopwatch sw = new Stopwatch();
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                double expectedTime, actualTime, percentageDifference;
                int numBytes = 0;
                byte shiftMask = 0xFF;
                double stopBits = -1;

                switch ((int)com1.StopBits)
                {
                    case (int)StopBits.One:
                        stopBits = 1.0;
                        break;

                    case (int)StopBits.OnePointFive:
                        stopBits = 1.5;
                        break;

                    case (int)StopBits.Two:
                        stopBits = 2.0;
                        break;
                }

                int numBytesToSend = (int)(((DEFAULT_TIME / 1000.0) * com1.BaudRate) / (stopBits + com1.DataBits + 1));
                byte[] xmitBytes = new byte[numBytesToSend];
                byte[] expectedBytes = new byte[numBytesToSend];
                byte[] rcvBytes = new byte[numBytesToSend];

                //Create a mask that when logically and'd with the transmitted byte will
                //will result in the byte received due to the leading bits being chopped
                //off due to DataBits less then 8
                shiftMask >>= 8 - com1.DataBits;

                //Generate some random bytes to read/write for this StopBits setting
                for (int i = 0; i < xmitBytes.Length; i++)
                {
                    xmitBytes[i] = (byte)rndGen.Next(0, 256);
                    expectedBytes[i] = (byte)(xmitBytes[i] & shiftMask);
                }

                com2.DataBits = com1.DataBits;
                com2.StopBits = com1.StopBits;
                com2.Open();
                actualTime = 0;

                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                int initialNumBytes;

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    com2.DiscardInBuffer();

                    IAsyncResult beginWriteResult = com1.BaseStream.BeginWrite(xmitBytes, 0, numBytesToSend, null, null);
                    while (0 == (initialNumBytes = com2.BytesToRead))
                    { }

                    sw.Start();
                    TCSupport.WaitForReadBufferToLoad(com2, numBytesToSend);
                    sw.Stop();

                    actualTime += sw.ElapsedMilliseconds;
                    actualTime += ((initialNumBytes * (stopBits + com1.DataBits + 1)) / com1.BaudRate) * 1000;
                    com1.BaseStream.EndWrite(beginWriteResult);

                    sw.Reset();
                }

                Thread.CurrentThread.Priority = ThreadPriority.Normal;
                actualTime /= NUM_TRYS;
                expectedTime = ((xmitBytes.Length * (stopBits + com1.DataBits + 1)) / com1.BaudRate) * 1000;
                percentageDifference = Math.Abs((expectedTime - actualTime) / expectedTime);

                //If the percentageDifference between the expected time and the actual time is to high
                //then the expected baud rate must not have been used and we should report an error
                if (MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE < percentageDifference)
                {
                    Fail("ERROR!!! StopBits not used Expected time:{0}, actual time:{1} percentageDifference:{2}", expectedTime, actualTime, percentageDifference, numBytes);
                }

                com2.Read(rcvBytes, 0, rcvBytes.Length);

                //Verify that the bytes we sent were the same ones we received
                for (int i = 0; i < expectedBytes.Length; i++)
                {
                    if (expectedBytes[i] != rcvBytes[i])
                    {
                        Fail("ERROR!!! Expected to read {0} actual read {1}", expectedBytes[i], rcvBytes[i]);
                    }
                }
            }
        }
        #endregion
    }
}
