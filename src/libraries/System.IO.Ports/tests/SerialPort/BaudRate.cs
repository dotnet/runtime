// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class BaudRate_Property : PortsTest
    {
        //The default ammount of time the a transfer should take at any given baud rate.
        //The bytes sent should be adjusted to take this ammount of time to transfer at the specified baud rate.
        private const int DEFAULT_TIME = 750;

        //If the percentage difference between the expected BaudRate and the actual baudrate
        //found through Stopwatch is greater then 5% then the BaudRate value was not correctly
        //set and the testcase fails.
        private const double MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE = .07;

        private const int NUM_TRYS = 5;

        private enum ThrowAt { Set, Open };

        #region Test Cases
        [KnownFailure] // either hanging or really slow
        [ConditionalFact(nameof(HasNullModem))]
        public void BaudRate_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default BaudRate");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyBaudRate(com1);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [KnownFailure] // currently not supported on linux (can be emulated)
        [ConditionalFact(nameof(HasNullModem))]
        public void BaudRate_14400()
        {
            Debug.WriteLine("Verifying 14400 BaudRate");
            VerifyBaudRate(14400);
        }

        [KnownFailure] // currently not supported on linux (can be emulated)
        [ConditionalFact(nameof(HasNullModem))]
        public void BaudRate_28800()
        {
            Debug.WriteLine("Verifying 28800 BaudRate");
            VerifyBaudRate(28800);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void BaudRate_1200()
        {
            Debug.WriteLine("Verifying 1200 BaudRate");
            VerifyBaudRate(1200);
        }

        [KnownFailure] // either hanging or really slow
        [ConditionalFact(nameof(HasNullModem))]
        public void BaudRate_115200()
        {
            Debug.WriteLine("Verifying 115200 BaudRate");
            VerifyBaudRate(115200);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BaudRate_MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue BaudRate");
            VerifyException(int.MinValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BaudRate_Neg1()
        {
            Debug.WriteLine("Verifying -1 BaudRate");
            VerifyException(-1, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BaudRate_Zero()
        {
            Debug.WriteLine("Verifying 0 BaudRate");
            VerifyException(0, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void BaudRate_MaxValue()
        {
            Debug.WriteLine("Verifying Int32.MaxValue BaudRate");
            VerifyException(int.MaxValue, ThrowAt.Open, typeof(ArgumentOutOfRangeException));
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyException(int baudRate, ThrowAt throwAt, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, baudRate, throwAt, expectedException);

                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, baudRate, expectedException);
            }
        }

        private void VerifyExceptionAtOpen(SerialPort com, int baudRate, ThrowAt throwAt, Type expectedException)
        {
            int origBaudRate = com.BaudRate;
            SerialPortProperties serPortProp = new SerialPortProperties();

            if (null == expectedException && throwAt == ThrowAt.Open)
            {
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("BaudRate", baudRate);
            }
            else
            {
                serPortProp.SetAllPropertiesToDefaults();
            }

            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            if (ThrowAt.Open == throwAt)
                serPortProp.SetProperty("BaudRate", baudRate);

            try
            {
                com.BaudRate = baudRate;

                if (ThrowAt.Open == throwAt)
                    com.Open();

                if (null != expectedException)
                {
                    Assert.Fail($"ERROR!!! Expected Open() to throw {expectedException} and nothing was thrown");
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Assert.Fail($"ERROR!!! Expected Open() NOT to throw an exception and {e.GetType()} was thrown");
                }
                else if (e.GetType() != expectedException)
                {
                    Assert.Fail($"ERROR!!! Expected Open() throw {expectedException} and {e.GetType()} was thrown: {e}");
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);

            com.BaudRate = origBaudRate;
        }

        private void VerifyExceptionAfterOpen(SerialPort com, int baudRate, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.BaudRate = baudRate;
                if (null != expectedException)
                {
                    Assert.Fail($"ERROR!!! Expected setting the BaudRate after Open() to throw {expectedException} and nothing was thrown");
                }
                else
                {
                    serPortProp.SetProperty("BaudRate", baudRate);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Assert.Fail($"ERROR!!! Expected setting the BaudRate after Open() NOT to throw an exception and {e.GetType()} was thrown");
                }
                else if (e.GetType() != expectedException)
                {
                    Assert.Fail($"ERROR!!! Expected setting the BaudRate after Open() throw {expectedException} and {e.GetType()} was thrown");
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);
        }

        private void VerifyBaudRate(int baudRate)
        {
            Debug.WriteLine("Verifying setting BaudRate BEFORE a call to Open has been made");
            VerifyBaudRateAtOpen(baudRate);

            Debug.WriteLine("Verifying setting BaudRate AFTER a call to Open has been made");
            VerifyBaudRateAfterOpen(baudRate);
        }

        private void VerifyBaudRateAtOpen(int baudRate)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                com1.BaudRate = baudRate;
                com1.Open();
                serPortProp.SetProperty("BaudRate", baudRate);
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyBaudRate(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyBaudRateAfterOpen(int baudRate)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                com1.Open();
                com1.BaudRate = baudRate;
                serPortProp.SetProperty("BaudRate", baudRate);
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyBaudRate(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyBaudRate(SerialPort com1)
        {
            int numBytesToSend = Math.Max((int)((com1.BaudRate * (DEFAULT_TIME / 1000.0)) / 10.0), 64);
            byte[] xmitBytes = new byte[numBytesToSend];
            byte[] rcvBytes = new byte[numBytesToSend];
            Random rndGen = new Random();
            Stopwatch sw = new Stopwatch();
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                double expectedTime, actualTime, percentageDifference;

                //Generate some random byte to read/write at this baudrate
                for (int i = 0; i < xmitBytes.Length; i++)
                {
                    xmitBytes[i] = (byte)rndGen.Next(0, 256);
                }

                // Note - this was originally com2.ReadBufferSize = numBytesToSend, but
                // that seems to cause a deadlock with FTDI-based devices where you can't actually
                // get all the data buffered so you can't read it back
                // At 115200 we were seeing numBytesToSend = 8640, however, we only get 8627 waiting in the input buffer
                // this might be an FTDI bug, but it's not a System.Io.SerialPort bug
                com2.ReadBufferSize = numBytesToSend + 16;
                com2.BaudRate = com1.BaudRate;
                com2.Open();

                actualTime = 0;

                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                for (int i = 0; i < NUM_TRYS; i++)
                {
                    int bytesToRead = 0;

                    com2.DiscardInBuffer();

                    IAsyncResult beginWriteResult = com1.BaseStream.BeginWrite(xmitBytes, 0, xmitBytes.Length, null, null);
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
                    actualTime += ((bytesToRead * 10.0) / com1.BaudRate) * 1000;
                    com1.BaseStream.EndWrite(beginWriteResult);
                    sw.Reset();
                }

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                expectedTime = ((xmitBytes.Length * 10.0) / com1.BaudRate) * 1000;
                actualTime /= NUM_TRYS;
                percentageDifference = Math.Abs((expectedTime - actualTime) / expectedTime);

                //If the percentageDifference between the expected time and the actual time is to high
                //then the expected baud rate must not have been used and we should report an error
                if (MAX_ACCEPTABLE_PERCENTAGE_DIFFERENCE < percentageDifference)
                {
                    Assert.Fail(string.Format("ERROR!!! BuadRate not used Expected time:{0}, actual time:{1} percentageDifference:{2}",
                        expectedTime, actualTime, percentageDifference));
                }

                com2.Read(rcvBytes, 0, rcvBytes.Length);

                //Verify that the bytes we sent were the same ones we received
                Assert.Equal(xmitBytes, rcvBytes);
            }
        }
        #endregion
    }
}
