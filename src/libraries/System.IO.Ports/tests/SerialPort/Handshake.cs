// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class Handshake_Property : PortsTest
    {
        //The default number of bytes to read/write to verify the speed of the port
        //and that the bytes were transfered successfully
        private static readonly int s_DEFAULT_BYTE_SIZE = TCSupport.MinimumBlockingByteCount;

        //The number of bytes to send when send XOn or XOff, the actual XOn/XOff char will be inserted somewhere
        //in the array of bytes
        private static readonly int s_DEFAULT_BYTE_SIZE_XON_XOFF = TCSupport.MinimumBlockingByteCount * 2;

        //The default time to wait after writing some bytes
        private const int DEFAULT_WAIT_AFTER_READ_OR_WRITE = 500;

        private enum ThrowAt { Set, Open };

        #region Test Cases
        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default Handshake");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyHandshake(com1);

                com1.DiscardInBuffer();
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_None_BeforeOpen()
        {
            Debug.WriteLine("Verifying None Handshake before open");
            VerifyHandshakeBeforeOpen((int)Handshake.None);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_XOnXOff_BeforeOpen()
        {
            Debug.WriteLine("Verifying XOnXOff Handshake before open");
            VerifyHandshakeBeforeOpen((int)Handshake.XOnXOff);
        }


        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Handshake_RequestToSend_BeforeOpen()
        {
            Debug.WriteLine("Verifying RequestToSend Handshake before open");
            VerifyHandshakeBeforeOpen((int)Handshake.RequestToSend);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Handshake_RequestToSendXOnXOff_BeforeOpen()
        {
            Debug.WriteLine("Verifying RequestToSendXOnXOff Handshake before open");
            VerifyHandshakeBeforeOpen((int)Handshake.RequestToSendXOnXOff);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_None_AfterOpen()
        {
            Debug.WriteLine("Verifying None Handshake after open");
            VerifyHandshakeAfterOpen((int)Handshake.None);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem))]
        public void Handshake_XOnXOff_AfterOpen()
        {
            Debug.WriteLine("Verifying XOnXOff Handshake after open");
            VerifyHandshakeAfterOpen((int)Handshake.XOnXOff);
        }

        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Handshake_RequestToSend_AfterOpen()
        {
            Debug.WriteLine("Verifying RequestToSend Handshake after open");
            VerifyHandshakeAfterOpen((int)Handshake.RequestToSend);
        }

        [KnownFailure]
        [ConditionalFact(nameof(HasNullModem), nameof(HasHardwareFlowControl))]
        public void Handshake_RequestToSendXOnXOff_AfterOpen()
        {
            Debug.WriteLine("Verifying RequestToSendXOnXOff Handshake after open");
            VerifyHandshakeAfterOpen((int)Handshake.RequestToSendXOnXOff);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Handshake_Int32MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue Handshake");
            VerifyException(int.MinValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Handshake_Neg1()
        {
            Debug.WriteLine("Verifying -1 Handshake");
            VerifyException(-1, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Handshake_4()
        {
            Debug.WriteLine("Verifying 4 Handshake");
            VerifyException(4, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Handshake_Int32MaxValue()
        {
            Debug.WriteLine("Verifying Int32.MaxValue Handshake");
            VerifyException(int.MaxValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyException(int handshake, ThrowAt throwAt, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, handshake, throwAt, expectedException);

                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, handshake, expectedException);
            }
        }


        private void VerifyExceptionAtOpen(SerialPort com, int handshake, ThrowAt throwAt, Type expectedException)
        {
            int origHandshake = (int)com.Handshake;

            SerialPortProperties serPortProp = new SerialPortProperties();

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            if (ThrowAt.Open == throwAt)
                serPortProp.SetProperty("Handshake", (Handshake)handshake);

            try
            {
                com.Handshake = (Handshake)handshake;

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
            com.Handshake = (Handshake)origHandshake;
        }

        private void VerifyExceptionAfterOpen(SerialPort com, int handshake, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.Handshake = (Handshake)handshake;
                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the Handshake after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the Handshake after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the Handshake after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }
            serPortProp.VerifyPropertiesAndPrint(com);
        }

        private void VerifyHandshakeBeforeOpen(int handshake)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Handshake = (Handshake)handshake;
                com1.Open();

                serPortProp.SetProperty("Handshake", (Handshake)handshake);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyHandshake(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyHandshakeAfterOpen(int handshake)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com1.Handshake = (Handshake)handshake;

                serPortProp.SetProperty("Handshake", (Handshake)handshake);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyHandshake(com1);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyHandshake(SerialPort com1)
        {
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                int origWriteTimeout = com1.WriteTimeout;
                int origReadTimeout = com1.ReadTimeout;

                com2.Open();
                com1.WriteTimeout = 250;
                com1.ReadTimeout = 250;

                VerifyRTSHandshake(com1, com2);
                VerifyXOnXOffHandshake(com1, com2);
                VerifyRTSXOnXOffHandshake(com1, com2);
                VerirfyRTSBufferFull(com1, com2);
                VerirfyXOnXOffBufferFull(com1, com2);

                com1.WriteTimeout = origWriteTimeout;
                com1.ReadTimeout = origReadTimeout;
            }
        }

        private void VerifyRTSHandshake(SerialPort com1, SerialPort com2)
        {
            bool origRtsEnable = com2.RtsEnable;

            try
            {
                com2.RtsEnable = true;

                try
                {
                    com1.Write(new byte[s_DEFAULT_BYTE_SIZE], 0, s_DEFAULT_BYTE_SIZE);
                }
                catch (TimeoutException)
                {
                    Fail("Err_103948aooh!!! TimeoutException thrown when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                }

                com2.RtsEnable = false;

                try
                {
                    com1.Write(new byte[s_DEFAULT_BYTE_SIZE], 0, s_DEFAULT_BYTE_SIZE);
                    if (Handshake.RequestToSend == com1.Handshake || Handshake.RequestToSendXOnXOff == com1.Handshake)
                    {
                        Fail("Err_15397lkjh!!! TimeoutException NOT thrown when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    }
                }
                catch (TimeoutException)
                {
                    if (Handshake.RequestToSend != com1.Handshake && Handshake.RequestToSendXOnXOff != com1.Handshake)
                    {
                        Fail("Err_1341pawh!!! TimeoutException thrown when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    }
                }

                com2.RtsEnable = true;

                try
                {
                    com1.Write(new byte[s_DEFAULT_BYTE_SIZE], 0, s_DEFAULT_BYTE_SIZE);
                }
                catch (TimeoutException)
                {
                    Fail("Err_143987aqaih!!! TimeoutException thrown when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                }
            }
            finally
            {
                com2.RtsEnable = origRtsEnable;
                com2.DiscardInBuffer();
            }
        }


        private void VerifyRTSXOnXOffHandshake(SerialPort com1, SerialPort com2)
        {
            bool origRtsEnable = com2.RtsEnable;
            Random rndGen = new Random();
            byte[] xmitXOnBytes = new byte[s_DEFAULT_BYTE_SIZE_XON_XOFF];
            byte[] xmitXOffBytes = new byte[s_DEFAULT_BYTE_SIZE_XON_XOFF];

            try
            {
                for (int i = 0; i < xmitXOnBytes.Length; i++)
                {
                    byte rndByte;

                    do
                    {
                        rndByte = (byte)rndGen.Next(0, 256);
                    } while (rndByte == 17 || rndByte == 19);

                    xmitXOnBytes[i] = rndByte;
                }

                for (int i = 0; i < xmitXOffBytes.Length; i++)
                {
                    byte rndByte;

                    do
                    {
                        rndByte = (byte)rndGen.Next(0, 256);
                    } while (rndByte == 17 || rndByte == 19);

                    xmitXOffBytes[i] = rndByte;
                }

                xmitXOnBytes[rndGen.Next(0, xmitXOnBytes.Length)] = (byte)17;
                xmitXOffBytes[rndGen.Next(0, xmitXOffBytes.Length)] = (byte)19;

                com2.RtsEnable = false;

                com2.Write(xmitXOffBytes, 0, xmitXOffBytes.Length);
                com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
                while (true)
                {
                    try
                    {
                        com1.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }

                try
                {
                    com1.Write(new byte[s_DEFAULT_BYTE_SIZE], 0, s_DEFAULT_BYTE_SIZE);
                    if (Handshake.RequestToSend == com1.Handshake || Handshake.RequestToSendXOnXOff == com1.Handshake)
                    {
                        Fail("Err_1253aasyo!!! TimeoutException NOT thrown after XOff and XOn char sent when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    }
                }
                catch (TimeoutException)
                {
                    if (Handshake.RequestToSend != com1.Handshake && Handshake.RequestToSendXOnXOff != com1.Handshake)
                    {
                        Fail("Err_51390awi!!! TimeoutException thrown after XOff and XOn char sent when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    }
                }

                com2.Write(xmitXOffBytes, 0, xmitXOffBytes.Length);
                com2.RtsEnable = false;
                com2.RtsEnable = true;
                while (true)
                {
                    try
                    {
                        com1.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }

                try
                {
                    com1.Write(new byte[s_DEFAULT_BYTE_SIZE], 0, s_DEFAULT_BYTE_SIZE);
                    if (Handshake.XOnXOff == com1.Handshake || Handshake.RequestToSendXOnXOff == com1.Handshake)
                    {
                        Fail("Err_2457awez!!! TimeoutException NOT thrown after RTSEnable set to false then true when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    }
                }
                catch (TimeoutException)
                {
                    if (Handshake.XOnXOff != com1.Handshake && Handshake.RequestToSendXOnXOff != com1.Handshake)
                    {
                        Fail("Err_3240aw4er!!! TimeoutException thrown RTSEnable set to false then true when CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    }
                }

                com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
                while (true)
                {
                    try
                    {
                        com1.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                com2.RtsEnable = origRtsEnable;
                com2.DiscardInBuffer();
            }
        }


        private void VerifyXOnXOffHandshake(SerialPort com1, SerialPort com2)
        {
            bool origRtsEnable = com2.RtsEnable;
            Random rndGen = new Random();
            byte[] xmitXOnBytes = new byte[s_DEFAULT_BYTE_SIZE_XON_XOFF];
            byte[] xmitXOffBytes = new byte[s_DEFAULT_BYTE_SIZE_XON_XOFF];

            try
            {
                com2.RtsEnable = true;

                for (int i = 0; i < xmitXOnBytes.Length; i++)
                {
                    byte rndByte;

                    do
                    {
                        rndByte = (byte)rndGen.Next(0, 256);
                    } while (rndByte == 17 || rndByte == 19);

                    xmitXOnBytes[i] = rndByte;
                }

                for (int i = 0; i < xmitXOffBytes.Length; i++)
                {
                    byte rndByte;

                    do
                    {
                        rndByte = (byte)rndGen.Next(0, 256);
                    } while (rndByte == 17 || rndByte == 19);

                    xmitXOffBytes[i] = rndByte;
                }

                Assert.InRange(xmitXOnBytes.Length, 1, int.MaxValue);

                int XOnIndex = rndGen.Next(0, xmitXOnBytes.Length);
                int XOffIndex = rndGen.Next(0, xmitXOffBytes.Length);

                xmitXOnBytes[XOnIndex] = (byte)17;
                xmitXOffBytes[XOffIndex] = (byte)19;

                Debug.WriteLine("XOnIndex={0} XOffIndex={1}", XOnIndex, XOffIndex);

                com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
                com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
                while (true)
                {
                    try
                    {
                        com1.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }

                try
                {
                    com1.Write(new byte[s_DEFAULT_BYTE_SIZE], 0, s_DEFAULT_BYTE_SIZE);
                }
                catch (TimeoutException)
                {
                    Fail("Err_2357pquaz!!! TimeoutException thrown after XOn char sent and CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                }

                com2.Write(xmitXOffBytes, 0, xmitXOffBytes.Length);
                while (true)
                {
                    try
                    {
                        com1.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }

                try
                {
                    com1.Write(new byte[s_DEFAULT_BYTE_SIZE], 0, s_DEFAULT_BYTE_SIZE);

                    if (Handshake.XOnXOff == com1.Handshake || Handshake.RequestToSendXOnXOff == com1.Handshake)
                    {
                        Fail("Err_1349znpq!!! TimeoutException NOT thrown after XOff char sent and CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    }
                }
                catch (TimeoutException)
                {
                    if (Handshake.XOnXOff != com1.Handshake && Handshake.RequestToSendXOnXOff != com1.Handshake)
                    {
                        Fail("Err_2507pqzhn!!! TimeoutException thrown after XOff char sent and CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                    }
                }

                com2.Write(xmitXOnBytes, 0, xmitXOnBytes.Length);
                while (true)
                {
                    try
                    {
                        com1.ReadByte();
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }

                try
                {
                    com1.Write(new byte[s_DEFAULT_BYTE_SIZE], 0, s_DEFAULT_BYTE_SIZE);
                }
                catch (TimeoutException)
                {
                    Fail("Err_2570aqpa!!! TimeoutException thrown after XOn char sent and CtsHolding={0} with Handshake={1}", com1.CtsHolding, com1.Handshake);
                }
            }
            finally
            {
                com2.RtsEnable = origRtsEnable;
                com2.DiscardInBuffer();
            }
        }


        private void VerirfyRTSBufferFull(SerialPort com1, SerialPort com2)
        {
            int com1BaudRate = com1.BaudRate;
            int com2BaudRate = com2.BaudRate;
            int bufferSize = com1.ReadBufferSize;
            int upperLimit = (3 * bufferSize) / 4;
            int lowerLimit = bufferSize / 4;
            byte[] bytes = new byte[upperLimit];

            try
            {
                //Set the BaudRate to something faster so that it does not take so long to fill up the buffer
                com1.BaudRate = 115200;
                com2.BaudRate = 115200;

                //Write 1 less byte then when the RTS pin would be cleared
                com2.Write(bytes, 0, upperLimit - 1);
                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

                if (IsRequestToSend(com1))
                {
                    if (!com2.CtsHolding)
                    {
                        Fail("Err_548458ahiede Expected RTS to be set");
                    }
                }
                else
                {
                    if (com2.CtsHolding)
                    {
                        Fail("Err_028538aieoz Expected RTS to be cleared");
                    }
                }

                //Write the byte and verify the RTS pin is cleared appropriately
                com2.Write(bytes, 0, 1);
                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

                if (IsRequestToSend(com1))
                {
                    if (com2.CtsHolding)
                    {
                        Fail("Err_508845aueid Expected RTS to be cleared");
                    }
                }
                else
                {
                    if (com2.CtsHolding)
                    {
                        Fail("Err_48848ajeoid Expected RTS to be set");
                    }
                }

                //Read 1 less byte then when the RTS pin would be set
                com1.Read(bytes, 0, (upperLimit - lowerLimit) - 1);
                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

                if (IsRequestToSend(com1))
                {
                    if (com2.CtsHolding)
                    {
                        Fail("Err_952085aizpea Expected RTS to be cleared");
                    }
                }
                else
                {
                    if (com2.CtsHolding)
                    {
                        Fail("Err_725527ahjiuzp Expected RTS to be set");
                    }
                }

                //Read the byte and verify the RTS pin is set appropriately
                com1.Read(bytes, 0, 1);
                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

                if (IsRequestToSend(com1))
                {
                    if (!com2.CtsHolding)
                    {
                        Fail("Err_652820aopea Expected RTS to be set");
                    }
                }
                else
                {
                    if (com2.CtsHolding)
                    {
                        Fail("Err_35585ajuei Expected RTS to be cleared");
                    }
                }
            }
            finally
            {
                //Rollback any changed that were made to the SerialPort
                com1.BaudRate = com1BaudRate;
                com2.BaudRate = com2BaudRate;

                com1.DiscardInBuffer();//This can cuase the XOn character to be sent to com2.
                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);
                com2.DiscardInBuffer();
            }
        }

        private void VerirfyXOnXOffBufferFull(SerialPort com1, SerialPort com2)
        {
            int com1BaudRate = com1.BaudRate;
            int com2BaudRate = com2.BaudRate;
            bool com2RtsEnable = com2.RtsEnable;
            int bufferSize = com1.ReadBufferSize;
            int upperLimit = bufferSize - 1024;
            int lowerLimit = 1024;
            byte[] bytes = new byte[upperLimit];
            int byteRead;

            try
            {
                //Set the BaudRate to something faster so that it does not take so long to fill up the buffer
                com1.BaudRate = 115200;
                com2.BaudRate = 115200;

                if (com1.Handshake == Handshake.RequestToSendXOnXOff)
                {
                    com2.RtsEnable = true;
                    com1.Write("foo");
                    Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);
                    com2.DiscardInBuffer();
                }

                //Write 1 less byte then when the XOff character would be sent
                com2.Write(bytes, 0, upperLimit - 1);
                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

                if (IsXOnXOff(com1))
                {
                    if (com2.BytesToRead != 0)
                    {
                        Fail("Err_81919aniee Did not expect anything to be sent");
                    }
                }
                else
                {
                    if (com2.BytesToRead != 0)
                    {
                        Fail("Err_5258aieodpo Did not expect anything to be sent com2.BytesToRead={0}", com2.BytesToRead);
                    }
                }

                //Write the byte and verify the XOff character was sent as appropriately
                com2.Write(bytes, 0, 1);
                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

                if (IsXOnXOff(com1))
                {
                    if (com2.BytesToRead != 1)
                    {
                        Fail("Err_12558aoed Expected XOff to be sent and nothing was sent");
                    }
                    else if (XOnOff.XOFF != (byteRead = com2.ReadByte()))
                    {
                        Fail("Err_0188598aoepad Expected XOff to be sent actually sent={0}", byteRead);
                    }
                }
                else
                {
                    if (com2.BytesToRead != 0)
                    {
                        Fail("Err_2258ajoe Did not expect anything to be sent");
                    }
                }

                //Read 1 less byte then when the XOn char would be sent
                com1.Read(bytes, 0, (upperLimit - lowerLimit) - 1);
                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

                if (IsXOnXOff(com1))
                {
                    if (com2.BytesToRead != 0)
                    {
                        Fail("Err_22808aiuepa Did not expect anything to be sent");
                    }
                }
                else
                {
                    if (com2.BytesToRead != 0)
                    {
                        Fail("Err_12508aieap Did not expect anything to be sent");
                    }
                }

                //Read the byte and verify the XOn char is sent as appropriately
                com1.Read(bytes, 0, 1);

                Thread.Sleep(DEFAULT_WAIT_AFTER_READ_OR_WRITE);

                if (IsXOnXOff(com1))
                {
                    if (com2.BytesToRead != 1)
                    {
                        Fail("Err_6887518adizpa Expected XOn to be sent and nothing was sent");
                    }
                    else if (XOnOff.XON != (byteRead = com2.ReadByte()))
                    {
                        Fail("Err_58145auead Expected XOn to be sent actually sent={0}", byteRead);
                    }
                }
                else
                {
                    if (com2.BytesToRead != 0)
                    {
                        Fail("Err_256108aipeg Did not expect anything to be sent");
                    }
                }
            }
            finally
            {
                //Rollback any changed that were made to the SerialPort
                com1.BaudRate = com1BaudRate;
                com2.BaudRate = com2BaudRate;


                com1.DiscardInBuffer();
                com2.DiscardInBuffer();

                com2.RtsEnable = com2RtsEnable;
            }
        }

        private bool IsRequestToSend(SerialPort com)
        {
            return com.Handshake == Handshake.RequestToSend || com.Handshake == Handshake.RequestToSendXOnXOff;
        }

        private bool IsXOnXOff(SerialPort com)
        {
            return com.Handshake == Handshake.XOnXOff || com.Handshake == Handshake.RequestToSendXOnXOff;
        }
        #endregion
    }
}
