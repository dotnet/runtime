// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    [KnownFailure]
    public class WriteBufferSize_Property : PortsTest
    {
        private const int MAX_RANDMOM_BUFFER_SIZE = 1024 * 16;
        private const int LARGE_BUFFER_SIZE = 1024 * 128;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteBufferSize_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default WriteBufferSize before Open");
                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);

                Debug.WriteLine("Verifying default WriteBufferSize after Open");
                com1.Open();
                serPortProp = new SerialPortProperties();
                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);

                Debug.WriteLine("Verifying default WriteBufferSize after Close");
                com1.Close();
                serPortProp = new SerialPortProperties();
                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void WriteBufferSize_AfterOpen()
        {
            VerifyException(1024, null, typeof(InvalidOperationException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteBufferSize_NEG1()
        {
            VerifyException(-1, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteBufferSize_Int32MinValue()
        {
            VerifyException(int.MinValue, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteBufferSize_0()
        {
            VerifyException(0, typeof(ArgumentOutOfRangeException), typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteBufferSize_1()
        {
            Debug.WriteLine("Verifying setting WriteBufferSize=1");
            VerifyException(1, typeof(IOException), typeof(InvalidOperationException), true);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void WriteBufferSize_Smaller()
        {
            uint newWriteBufferSize = (uint)(new SerialPort()).WriteBufferSize;

            newWriteBufferSize /= 2; //Make the new buffer size half the original size
            newWriteBufferSize &= 0xFFFFFFFE; //Make sure the new buffer size is even by clearing the lowest order bit

            VerifyWriteBufferSize((int)newWriteBufferSize);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void WriteBufferSize_Larger()
        {
            using (var com = new SerialPort())
            {
                VerifyWriteBufferSize(com.WriteBufferSize * 2);
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteBufferSize_Odd()
        {
            Debug.WriteLine("Verifying setting WriteBufferSize=Odd");
            using (var com = new SerialPort())
            {
                int bufferSize = com.WriteBufferSize * 2 + 1;
                VerifyException(bufferSize, typeof(IOException), typeof(InvalidOperationException), true);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void WriteBufferSize_Even()
        {
            Debug.WriteLine("Verifying setting WriteBufferSize=Even");
            using (var com = new SerialPort())
            {
                VerifyWriteBufferSize(com.WriteBufferSize * 2);
            }
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void WriteBufferSize_Rnd()
        {
            Random rndGen = new Random(-55);
            uint newWriteBufferSize = (uint)rndGen.Next(MAX_RANDMOM_BUFFER_SIZE);

            newWriteBufferSize &= 0xFFFFFFFE; //Make sure the new buffer size is even by clearing the lowest order bit

            VerifyWriteBufferSize((int)newWriteBufferSize);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void WriteBufferSize_Large()
        {
            VerifyWriteBufferSize(LARGE_BUFFER_SIZE);
        }

        #endregion

        #region Verification for Test Cases
        private void VerifyException(int newWriteBufferSize, Type expectedExceptionBeforeOpen, Type expectedExceptionAfterOpen)
        {
            VerifyException(newWriteBufferSize, expectedExceptionBeforeOpen, expectedExceptionAfterOpen, false);
        }

        private void VerifyException(int newWriteBufferSize, Type expectedExceptionBeforeOpen, Type expectedExceptionAfterOpen, bool throwAtOpen)
        {
            VerifyExceptionBeforeOpen(newWriteBufferSize, expectedExceptionBeforeOpen, throwAtOpen);
            VerifyExceptionAfterOpen(newWriteBufferSize, expectedExceptionAfterOpen);
        }

        private void VerifyExceptionBeforeOpen(int newWriteBufferSize, Type expectedException, bool throwAtOpen)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                try
                {
                    com.WriteBufferSize = newWriteBufferSize;
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
                        Fail("Err_545498ahpba!!! expected exception {0} and {1} was thrown: {2}", expectedException, e.GetType(), e);
                    }
                }
            }
        }

        private void VerifyExceptionAfterOpen(int newWriteBufferSize, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                int originalWriteBufferSize = com.WriteBufferSize;

                com.Open();
                try
                {
                    com.WriteBufferSize = newWriteBufferSize;
                    Fail("Err_561567anhbp!!! expected exception {0} and nothing was thrown", expectedException);
                }
                catch (Exception e)
                {
                    if (e.GetType() != expectedException)
                    {
                        Fail("Err_21288ajpbam!!! expected exception {0} and {1} was thrown", expectedException, e.GetType());
                    }
                    else if (originalWriteBufferSize != com.WriteBufferSize)
                    {
                        Fail("Err_454987ahbopa!!! expected WriteBufferSize={0} and actual={1}", originalWriteBufferSize, com.WriteBufferSize);
                    }
                    else if (TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.NullModem))
                    {
                        VerifyWriteBufferSize(com, originalWriteBufferSize);
                    }
                }
            }
        }

        private void VerifyWriteBufferSize(int newWriteBufferSize)
        {
            Debug.WriteLine("Verifying setting WriteBufferSize={0} BEFORE a call to Open() has been made", newWriteBufferSize);
            VerifyWriteBufferSizeBeforeOpen(newWriteBufferSize);
        }

        private void VerifyWriteBufferSizeBeforeOpen(int newWriteBufferSize)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying setting WriteBufferSize to {0}", newWriteBufferSize);

                com1.WriteBufferSize = newWriteBufferSize;
                com1.Open();

                VerifyWriteBufferSize(com1, newWriteBufferSize);
            }
        }

        private void VerifyWriteBufferSize(SerialPort com1, int expectedWriteBufferSize)
        {
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                byte[] xmitBytes = new byte[Math.Max(expectedWriteBufferSize, com1.WriteBufferSize)];
                byte[] rcvBytes = new byte[xmitBytes.Length];
                SerialPortProperties serPortProp = new SerialPortProperties();
                Random rndGen = new Random(-55);


                for (int i = 0; i < xmitBytes.Length; i++)
                {
                    xmitBytes[i] = (byte)rndGen.Next(0, 256);
                }

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com2.ReadBufferSize = expectedWriteBufferSize;
                serPortProp.SetProperty("WriteBufferSize", expectedWriteBufferSize);

                com2.Open();

                int origBaudRate = com1.BaudRate;

                com2.BaudRate = 115200;
                com1.BaudRate = 115200;
                serPortProp.SetProperty("BaudRate", 115200);

                com1.Write(xmitBytes, 0, xmitBytes.Length);

                TCSupport.WaitForReadBufferToLoad(com2, xmitBytes.Length);

                Debug.WriteLine("Verifying properties after changing WriteBufferSize");
                serPortProp.VerifyPropertiesAndPrint(com1);

                com2.Read(rcvBytes, 0, rcvBytes.Length);

                Assert.Equal(xmitBytes, rcvBytes);

                com2.BaudRate = origBaudRate;
                com1.BaudRate = origBaudRate;
            }
        }
        #endregion
    }
}
