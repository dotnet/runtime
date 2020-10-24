// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Linq;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    [KnownFailure]
    public class ReadBufferSize_Property : PortsTest
    {
        private const int MAX_RANDOM_BUFFER_SIZE = 1024 * 16;
        private const int LARGE_BUFFER_SIZE = 1024 * 128;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadBufferSize_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default ReadBufferSize before Open");

                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);

                Debug.WriteLine("Verifying default ReadBufferSize after Open");

                com1.Open();
                serPortProp = new SerialPortProperties();
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);

                Debug.WriteLine("Verifying default ReadBufferSize after Close");

                com1.Close();
                serPortProp = new SerialPortProperties();
                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_AfterOpen()
        {
            VerifyException(1024, null, typeof(InvalidOperationException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_NEG1()
        {
            VerifyException(-1, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_Int32MinValue()
        {
            VerifyException(int.MinValue, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_0()
        {
            VerifyException(0, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_1()
        {
            Debug.WriteLine("Verifying setting ReadBufferSize=1");
            VerifyException(1, typeof(IOException), typeof(InvalidOperationException), true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_2()
        {
            Debug.WriteLine("Verifying setting ReadBufferSize=");
            VerifyReadBufferSize(2);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_Smaller()
        {
            using (var com = new SerialPort())
            {
                uint newReadBufferSize = (uint)com.ReadBufferSize;

                newReadBufferSize /= 2; //Make the new buffer size half the original size
                newReadBufferSize &= 0xFFFFFFFE; //Make sure the new buffer size is even by clearing the lowest order bit

                VerifyReadBufferSize((int)newReadBufferSize);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_Larger()
        {
            VerifyReadBufferSize(((new SerialPort()).ReadBufferSize) * 2);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_Odd()
        {
            Debug.WriteLine("Verifying setting ReadBufferSize=Odd");

            VerifyException(((new SerialPort()).ReadBufferSize) * 2 + 1, typeof(IOException), typeof(InvalidOperationException), true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_Even()
        {
            Debug.WriteLine("Verifying setting ReadBufferSize=Even");
            VerifyReadBufferSize(((new SerialPort()).ReadBufferSize) * 2);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_Rnd()
        {
            Random rndGen = new Random(-55);
            uint newReadBufferSize = (uint)rndGen.Next(MAX_RANDOM_BUFFER_SIZE);

            newReadBufferSize &= 0xFFFFFFFE; //Make sure the new buffer size is even by clearing the lowest order bit

            //        if (!VerifyReadBufferSize((int)newReadBufferSize)){
            VerifyReadBufferSize(11620);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void ReadBufferSize_Large()
        {
            VerifyReadBufferSize(LARGE_BUFFER_SIZE);
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyException(int newReadBufferSize, Type expectedExceptionBeforeOpen, Type expectedExceptionAfterOpen)
        {
            VerifyException(newReadBufferSize, expectedExceptionBeforeOpen, expectedExceptionAfterOpen, false);
        }

        private void VerifyException(int newReadBufferSize, Type expectedExceptionBeforeOpen, Type expectedExceptionAfterOpen, bool throwAtOpen)
        {
            VerifyExceptionBeforeOpen(newReadBufferSize, expectedExceptionBeforeOpen, throwAtOpen);
            VerifyExceptionAfterOpen(newReadBufferSize, expectedExceptionAfterOpen);
        }

        private void VerifyExceptionBeforeOpen(int newReadBufferSize, Type expectedException, bool throwAtOpen)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                try
                {
                    com.ReadBufferSize = newReadBufferSize;
                    if (throwAtOpen)
                        com.Open();

                    if (null != expectedException)
                    {
                        Fail("Err_707278ahpa!!! expected exception {0} and nothing was thrown", expectedException);
                    }
                }
                catch (Exception e)
                {
                    if (null == expectedException)
                    {
                        Fail("Err_201890ioyun Expected no exception to be thrown and following was thrown \n{0}", e);
                    }
                    else if (e.GetType() != expectedException)
                    {
                        Fail("Err_545498ahpba!!! expected exception {0} and {1} was thrown", expectedException, e.GetType());
                    }
                }
            }
        }

        private void VerifyExceptionAfterOpen(int newReadBufferSize, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int originalReadBufferSize = com.ReadBufferSize;

                com.Open();
                try
                {
                    com.ReadBufferSize = newReadBufferSize;
                    Fail("Err_561567anhbp!!! expected exception {0} and nothing was thrown", expectedException);
                }
                catch (Exception e)
                {
                    if (e.GetType() != expectedException)
                    {
                        Fail("Err_21288ajpbam!!! expected exception {0} and {1} was thrown", expectedException, e.GetType());
                    }
                    else if (originalReadBufferSize != com.ReadBufferSize)
                    {
                        Fail("Err_454987ahbopa!!! expected ReadBufferSize={0} and actual={1}", originalReadBufferSize, com.ReadBufferSize);
                    }
                    VerifyReadBufferSize(com);
                }
            }
        }

        private void VerifyReadBufferSize(int newReadBufferSize)
        {
            Debug.WriteLine("Verifying setting ReadBufferSize={0} BEFORE a call to Open() has been made", newReadBufferSize);
            VerifyReadBufferSizeBeforeOpen(newReadBufferSize);
        }

        private void VerifyReadBufferSizeBeforeOpen(int newReadBufferSize)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                byte[] xmitBytes = new byte[newReadBufferSize];
                byte[] rcvBytes;
                SerialPortProperties serPortProp = new SerialPortProperties();
                Random rndGen = new Random(-55);
                int newBytesToRead;

                for (int i = 0; i < xmitBytes.Length; i++)
                {
                    xmitBytes[i] = (byte)rndGen.Next(0, 256);
                }

                if (newReadBufferSize < 4096)
                    newBytesToRead = Math.Min(4096, xmitBytes.Length + (xmitBytes.Length / 2));
                else
                    newBytesToRead = Math.Min(newReadBufferSize, xmitBytes.Length);

                rcvBytes = new byte[newBytesToRead];

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.ReadBufferSize = newReadBufferSize;
                serPortProp.SetProperty("ReadBufferSize", newReadBufferSize);

                com1.Open();
                com2.Open();

                int origBaudRate = com1.BaudRate;

                com2.BaudRate = 115200;
                com1.BaudRate = 115200;

                for (int j = 0; j < 1; j++)
                {
                    com2.Write(xmitBytes, 0, xmitBytes.Length);
                    com2.Write(xmitBytes, xmitBytes.Length / 2, xmitBytes.Length / 2);

                    TCSupport.WaitForReadBufferToLoad(com1, newBytesToRead);

                    Thread.Sleep(250);
                    //This is to wait for the bytes to be received after the buffer is full

                    serPortProp.SetProperty("BytesToRead", newBytesToRead);
                    serPortProp.SetProperty("BaudRate", 115200);

                    Debug.WriteLine("Verifying properties after bytes have been written");
                    serPortProp.VerifyPropertiesAndPrint(com1);

                    com1.Read(rcvBytes, 0, newBytesToRead);

                    Assert.Equal(xmitBytes.Take(newReadBufferSize), rcvBytes.Take(newReadBufferSize));

                    Debug.WriteLine("Verifying properties after bytes have been read");
                    serPortProp.SetProperty("BytesToRead", 0);
                    serPortProp.VerifyPropertiesAndPrint(com1);
                }

                com2.Write(xmitBytes, 0, xmitBytes.Length);
                com2.Write(xmitBytes, xmitBytes.Length / 2, xmitBytes.Length / 2);

                TCSupport.WaitForReadBufferToLoad(com1, newBytesToRead);

                serPortProp.SetProperty("BytesToRead", newBytesToRead);
                Debug.WriteLine("Verifying properties after writing bytes");
                serPortProp.VerifyPropertiesAndPrint(com1);

                com2.BaudRate = origBaudRate;
                com1.BaudRate = origBaudRate;
            }
        }

        private void VerifyReadBufferSize(SerialPort com1)
        {
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                int readBufferSize = com1.ReadBufferSize;
                byte[] xmitBytes = new byte[1024];
                SerialPortProperties serPortProp = new SerialPortProperties();
                Random rndGen = new Random(-55);
                int bytesToRead = readBufferSize < 4096 ? 4096 : readBufferSize;
                int origReadTimeout = com1.ReadTimeout;
                int bytesRead;

                for (int i = 0; i < xmitBytes.Length; i++)
                {
                    xmitBytes[i] = (byte)rndGen.Next(0, 256);
                }

                //bytesToRead = Math.Min(4096, xmitBytes.Length + (xmitBytes.Length / 2));

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("ReadBufferSize", readBufferSize);
                serPortProp.SetProperty("BaudRate", 115200);

                com2.Open();

                com2.BaudRate = 115200;
                com1.BaudRate = 115200;

                for (int i = 0; i < bytesToRead / xmitBytes.Length; i++)
                {
                    com2.Write(xmitBytes, 0, xmitBytes.Length);
                }

                com2.Write(xmitBytes, 0, xmitBytes.Length / 2);

                TCSupport.WaitForReadBufferToLoad(com1, bytesToRead);

                Thread.Sleep(250); //This is to wait for the bytes to be received after the buffer is full

                var rcvBytes = new byte[(int)(bytesToRead * 1.5)];
                if (bytesToRead != (bytesRead = com1.Read(rcvBytes, 0, rcvBytes.Length)))
                {
                    Fail("Err_2971ahius Did not read all expected bytes({0}) bytesRead={1} ReadBufferSize={2}", bytesToRead, bytesRead, com1.ReadBufferSize);
                }

                for (int i = 0; i < bytesToRead; i++)
                {
                    if (rcvBytes[i] != xmitBytes[i % xmitBytes.Length])
                    {
                        Fail("Err_70929apba!!!: Expected to read byte {0} actual={1} at {2}", xmitBytes[i % xmitBytes.Length], rcvBytes[i], i);
                    }
                }

                serPortProp.SetProperty("BytesToRead", 0);
                serPortProp.VerifyPropertiesAndPrint(com1);

                com1.ReadTimeout = 250;

                // "Err_1707ahspb!!!: After reading all bytes from buffer ReadByte() did not timeout");
                Assert.Throws<TimeoutException>(() => com1.ReadByte());

                com1.ReadTimeout = origReadTimeout;
            }
        }
        #endregion
    }
}
