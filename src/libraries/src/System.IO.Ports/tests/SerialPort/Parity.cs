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
    public class Parity_Property : PortsTest
    {
        //The default number of bytes to read/write to verify the speed of the port
        //and that the bytes were transfered successfully
        private const int DEFAULT_BYTE_SIZE = 512;

        //If the percentage difference between the expected time to transfer with the specified parity
        //and the actual time found through Stopwatch is greater then 10% then the Parity value was not correctly
        //set and the testcase fails.
        private const double MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE = .10;

        //The default number of databits to use when testing Parity
        private const int DEFUALT_DATABITS = 8;
        private const int NUM_TRYS = 3;

        private enum ThrowAt { Set, Open };

        #region Test Cases
        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Default()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default Parity");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyParity(com1, DEFAULT_BYTE_SIZE);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_None_BeforeOpen()
        {
            Debug.WriteLine("Verifying None Parity before open");
            VerifyParityBeforeOpen((int)Parity.None, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Even_BeforeOpen()
        {
            Debug.WriteLine("Verifying Even Parity before open");
            VerifyParityBeforeOpen((int)Parity.Even, DEFAULT_BYTE_SIZE);
        }


        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Odd_BeforeOpen()
        {
            Debug.WriteLine("Verifying Odd Parity before open");
            VerifyParityBeforeOpen((int)Parity.Odd, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Mark_BeforeOpen()
        {
            Debug.WriteLine("Verifying Mark Parity before open");
            VerifyParityBeforeOpen((int)Parity.Mark, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Space_BeforeOpen()
        {
            Debug.WriteLine("Verifying Space before open");
            VerifyParityBeforeOpen((int)Parity.Space, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_None_AfterOpen()
        {
            Debug.WriteLine("Verifying None Parity after open");
            VerifyParityAfterOpen((int)Parity.None, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Even_AfterOpen()
        {
            Debug.WriteLine("Verifying Even Parity after open");
            VerifyParityAfterOpen((int)Parity.Even, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Odd_AfterOpen()
        {
            Debug.WriteLine("Verifying Odd Parity after open");
            VerifyParityAfterOpen((int)Parity.Odd, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Mark_AfterOpen()
        {
            Debug.WriteLine("Verifying Mark Parity after open");
            VerifyParityAfterOpen((int)Parity.Mark, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Space_AfterOpen()
        {
            Debug.WriteLine("Verifying Space Parity after open");
            VerifyParityAfterOpen((int)Parity.Space, DEFAULT_BYTE_SIZE);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Parity_Int32MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue Parity");
            VerifyException(int.MinValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Parity_Neg1()
        {
            Debug.WriteLine("Verifying -1 Parity");
            VerifyException(-1, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Parity_Int32MaxValue()
        {
            Debug.WriteLine("Verifying Int32.MaxValue Parity");
            VerifyException(int.MaxValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Even_Odd()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying Parity Even and then Odd");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                com1.Parity = Parity.Even;
                com1.Parity = Parity.Odd;
                serPortProp.SetProperty("Parity", Parity.Odd);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyParity(com1, DEFAULT_BYTE_SIZE);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Odd_Even()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying Parity Odd and then Even");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                com1.Parity = Parity.Odd;
                com1.Parity = Parity.Even;
                serPortProp.SetProperty("Parity", Parity.Even);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyParity(com1, DEFAULT_BYTE_SIZE);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void Parity_Odd_Mark()
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying Parity Odd and then Mark");

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                com1.Open();
                com1.Parity = Parity.Odd;
                com1.Parity = Parity.Mark;
                serPortProp.SetProperty("Parity", Parity.Mark);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyParity(com1, DEFAULT_BYTE_SIZE);

                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyException(int parity, ThrowAt throwAt, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, parity, throwAt, expectedException);
                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, parity, expectedException);
            }
        }


        private void VerifyExceptionAtOpen(SerialPort com, int parity, ThrowAt throwAt, Type expectedException)
        {
            int origParity = (int)com.Parity;
            SerialPortProperties serPortProp = new SerialPortProperties();

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            if (ThrowAt.Open == throwAt)
                serPortProp.SetProperty("Parity", (Parity)parity);

            try
            {
                com.Parity = (Parity)parity;

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
            com.Parity = (Parity)origParity;
        }

        private void VerifyExceptionAfterOpen(SerialPort com, int parity, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.Parity = (Parity)parity;
                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the Parity after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the Parity after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the Parity after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);
        }


        private void VerifyParityBeforeOpen(int parity, int numBytesToSend)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Parity = (Parity)parity;
                com1.Open();
                serPortProp.SetProperty("Parity", (Parity)parity);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyParity(com1, numBytesToSend);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }


        private void VerifyParityAfterOpen(int parity, int numBytesToSend)
        {
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Open();
                com1.Parity = (Parity)parity;
                serPortProp.SetProperty("Parity", (Parity)parity);

                serPortProp.VerifyPropertiesAndPrint(com1);
                VerifyParity(com1, numBytesToSend);
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void VerifyParity(SerialPort com1, int numBytesToSend)
        {
            VerifyParity(com1, numBytesToSend, DEFUALT_DATABITS);
        }

        private void VerifyParity(SerialPort com1, int numBytesToSend, int dataBits)
        {
            byte[] xmitBytes = new byte[numBytesToSend];
            byte[] expectedBytes = new byte[numBytesToSend];
            Random rndGen = new Random();
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                byte shiftMask = 0xFF;

                //Create a mask that when logicaly and'd with the transmitted byte will 
                //will result in the byte recievied due to the leading bits being chopped
                //off due to Parity less then 8
                if (8 > dataBits)
                    shiftMask >>= 8 - com1.DataBits;

                //Generate some random bytes to read/write for this Parity setting
                for (int i = 0; i < xmitBytes.Length; i++)
                {
                    xmitBytes[i] = (byte)rndGen.Next(0, 256);
                    expectedBytes[i] = (byte)(xmitBytes[i] & shiftMask);
                }

                com2.DataBits = dataBits;
                com2.Parity = com1.Parity;
                com1.DataBits = dataBits;
                com2.Open();

                PerformWriteRead(com1, com2, xmitBytes, expectedBytes);
            }
        }


        private void VerifyReadParity(int parity, int dataBits, int numBytesToSend)
        {
            byte[] xmitBytes = new byte[numBytesToSend];
            byte[] expectedBytes = new byte[numBytesToSend];
            Random rndGen = new Random();
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                byte shiftMask = 0xFF;
                bool parityErrorOnLastByte = false, isParityError = false;
                int parityIndex;
                byte parityErrorByte;

                if (8 > dataBits)
                    com1.DataBits = dataBits + 1;
                else
                    com1.Parity = (Parity)parity;

                com2.Parity = (Parity)parity;
                com2.DataBits = dataBits;
                com1.StopBits = StopBits.One;
                com2.StopBits = StopBits.One;

                //Create a mask that when logicaly and'd with the transmitted byte will 
                //will result in the byte recievied due to the leading bits being chopped
                //off due to Parity less then 8
                shiftMask >>= 8 - dataBits;

                //Generate some random bytes to read/write for this Parity setting
                for (int i = 0; i < xmitBytes.Length; i++)
                {
                    do
                    {
                        xmitBytes[i] = (byte)rndGen.Next(0, 256);
                        isParityError = !VerifyParityByte(xmitBytes[i], com1.DataBits, (Parity)parity);
                    } while (isParityError); //Prevent adacent parity errors see VSWhidbey 103979

                    expectedBytes[i] = (byte)(xmitBytes[i] & shiftMask);
                    parityErrorOnLastByte = isParityError;
                }

                do
                {
                    parityErrorByte = (byte)rndGen.Next(0, 256);
                    isParityError = !VerifyParityByte(parityErrorByte, com1.DataBits, (Parity)parity);
                } while (!isParityError);

                parityIndex = rndGen.Next(xmitBytes.Length / 4, xmitBytes.Length / 2);
                xmitBytes[parityIndex] = parityErrorByte;
                expectedBytes[parityIndex] = com2.ParityReplace;

                Debug.WriteLine("parityIndex={0}", parityIndex);

                parityIndex = rndGen.Next((3 * xmitBytes.Length) / 4, xmitBytes.Length - 1);
                xmitBytes[parityIndex] = parityErrorByte;
                expectedBytes[parityIndex] = com2.ParityReplace;

                Debug.WriteLine("parityIndex={0}", parityIndex);

                /*
                for(int i=0; i<xmitBytes.Length; i++) {
                  do {
                    xmitBytes[i] = (byte)rndGen.Next(0, 256);
                    isParityError = !VerifyParityByte(xmitBytes[i], com1.DataBits, (Parity)parity);
                  }while(parityErrorOnLastByte && isParityError); //Prevent adacent parity errors see VSWhidbey 103979
    
                  expectedBytes[i] =  isParityError ? com2.ParityReplace :(byte)(xmitBytes[i] & shiftMask);
                  parityErrorOnLastByte = isParityError;
                }
            */
                com1.Open();
                com2.Open();
                PerformWriteRead(com1, com2, xmitBytes, expectedBytes);
            }
        }

        private void VerifyWriteParity(int parity, int dataBits, int numBytesToSend)
        {
            byte[] xmitBytes = new byte[numBytesToSend];
            byte[] expectedBytes = new byte[numBytesToSend];
            Random rndGen = new Random();
            using (SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            using (SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                //Generate some random bytes to read/write for this Parity setting
                for (int i = 0; i < xmitBytes.Length; i++)
                {
                    xmitBytes[i] = (byte)rndGen.Next(0, 256);
                    expectedBytes[i] = SetParityBit((byte)(xmitBytes[i]), dataBits, (Parity)parity);
                }

                if (8 > dataBits)
                    com2.DataBits = dataBits + 1;
                else
                    com2.Parity = (Parity)parity;

                com1.Parity = (Parity)parity;
                com1.DataBits = dataBits;
                com1.Open();
                com2.Open();

                PerformWriteRead(com1, com2, xmitBytes, expectedBytes);
            }
        }


        private void PerformWriteRead(SerialPort com1, SerialPort com2, byte[] xmitBytes, byte[] expectedBytes)
        {
            byte[] rcvBytes = new byte[expectedBytes.Length * 4];
            Stopwatch sw = new Stopwatch();
            double expectedTime, actualTime, percentageDifference;
            int numParityBits = (Parity)com1.Parity == Parity.None ? 0 : 1;
            double numStopBits = GetNumberOfStopBits(com1);
            int length = xmitBytes.Length;
            int rcvLength;

            // TODO: Consider removing all of the code to check the time it takes to transfer the bytes. 
            // This was likely just a copy and paste from another test case
            actualTime = 0;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            for (int i = 0; i < NUM_TRYS; i++)
            {
                com2.DiscardInBuffer();

                IAsyncResult beginWriteResult = com1.BaseStream.BeginWrite(xmitBytes, 0, length, null, null);

                while (0 == com2.BytesToRead) ;

                sw.Start();

                // Wait for all of the bytes to reach the input buffer of com2
                TCSupport.WaitForReadBufferToLoad(com2, length);

                sw.Stop();
                actualTime += sw.ElapsedMilliseconds;
                beginWriteResult.AsyncWaitHandle.WaitOne();
                sw.Reset();
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            actualTime /= NUM_TRYS;
            expectedTime = ((xmitBytes.Length * (1 + numStopBits + com1.DataBits + numParityBits)) / com1.BaudRate) * 1000;
            percentageDifference = Math.Abs((expectedTime - actualTime) / expectedTime);

            //If the percentageDifference between the expected time and the actual time is to high
            //then the expected baud rate must not have been used and we should report an error
            if (MAX_ACCEPTABEL_PERCENTAGE_DIFFERENCE < percentageDifference)
            {
                Fail("ERROR!!! Parity not used Expected time:{0}, actual time:{1} percentageDifference:{2}", expectedTime, actualTime, percentageDifference);
            }

            rcvLength = com2.Read(rcvBytes, 0, rcvBytes.Length);
            if (0 != com2.BytesToRead)
            {
                Fail("ERROR!!! BytesToRead={0} expected 0", com2.BytesToRead);
            }

            //Verify that the bytes we sent were the same ones we received
            int expectedIndex = 0, actualIndex = 0;

            for (; expectedIndex < expectedBytes.Length && actualIndex < rcvBytes.Length; ++expectedIndex, ++actualIndex)
            {
                if (expectedBytes[expectedIndex] != rcvBytes[actualIndex])
                {
                    if (actualIndex != rcvBytes.Length - 1 && expectedBytes[expectedIndex] == rcvBytes[actualIndex + 1])
                    {
                        //Sometimes if there is a parity error an extra byte gets added to the input stream so 
                        //look ahead at the next byte
                        actualIndex++;
                    }
                    else
                    {
                        Debug.WriteLine("Bytes Sent:");
                        TCSupport.PrintBytes(xmitBytes);

                        Debug.WriteLine("Bytes Recieved:");
                        TCSupport.PrintBytes(rcvBytes);

                        Debug.WriteLine("Expected Bytes:");
                        TCSupport.PrintBytes(expectedBytes);

                        Fail(
                            "ERROR: Expected to read {0,2:X} at {1,3} actually read {2,2:X} sent {3,2:X}",
                            expectedBytes[expectedIndex],
                            expectedIndex,
                            rcvBytes[actualIndex],
                            xmitBytes[expectedIndex]);
                    }
                }
            }

            if (expectedIndex < expectedBytes.Length)
            {
                Fail("ERRROR: Did not enumerate all of the expected bytes index={0} length={1}", expectedIndex, expectedBytes.Length);
            }
        }

        private double GetNumberOfStopBits(SerialPort com)
        {
            double stopBits = -1;

            switch (com.StopBits)
            {
                case StopBits.One:
                    stopBits = 1.0;
                    break;

                case StopBits.OnePointFive:
                    stopBits = 1.5;
                    break;

                case StopBits.Two:
                    stopBits = 2.0;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return stopBits;
        }


        private byte SetParityBit(byte parityByte, int dataBitSize, Parity parity)
        {
            byte result;

            if (8 == dataBitSize)
            {
                return parityByte;
            }

            switch (parity)
            {
                case Parity.Even:
                    if (VerifyEvenParity(parityByte, dataBitSize))
                        result = (byte)(parityByte & (0xFF >> 8 - dataBitSize));
                    else
                        result = (byte)(parityByte | (0x80 >> 8 - (dataBitSize + 1)));

                    break;

                case Parity.Odd:
                    if (VerifyOddParity(parityByte, dataBitSize))
                        result = (byte)(parityByte & (0xFF >> 8 - dataBitSize));
                    else
                        result = (byte)(parityByte | (0x80 >> 8 - (dataBitSize + 1)));

                    break;

                case Parity.Mark:
                    result = (byte)(parityByte | (0x80 >> 8 - (dataBitSize + 1)));
                    break;

                case Parity.Space:
                    result = (byte)(parityByte & (0xFF >> 8 - dataBitSize));
                    break;

                default:
                    result = 0;
                    break;
            }
            if (7 > dataBitSize)
                result &= (byte)(0xFF >> 8 - (dataBitSize + 1));

            return result;
        }


        private bool VerifyParityByte(byte parityByte, int parityWordSize, Parity parity)
        {
            switch (parity)
            {
                case Parity.Even:
                    return VerifyEvenParity(parityByte, parityWordSize);

                case Parity.Odd:
                    return VerifyOddParity(parityByte, parityWordSize);

                case Parity.Mark:
                    return VerifyMarkParity(parityByte, parityWordSize);

                case Parity.Space:
                    return VerifySpaceParity(parityByte, parityWordSize);

                default:
                    return false;
            }
        }

        private bool VerifyEvenParity(byte parityByte, int parityWordSize)
        {
            return (0 == CalcNumberOfTrueBits(parityByte, parityWordSize) % 2);
        }

        private bool VerifyOddParity(byte parityByte, int parityWordSize)
        {
            return (1 == CalcNumberOfTrueBits(parityByte, parityWordSize) % 2);
        }

        private bool VerifyMarkParity(byte parityByte, int parityWordSize)
        {
            byte parityMask = 0x80;

            parityByte <<= 8 - parityWordSize;
            return (0 != (parityByte & parityMask));
        }

        private bool VerifySpaceParity(byte parityByte, int parityWordSize)
        {
            byte parityMask = 0x80;

            parityByte <<= 8 - parityWordSize;
            return (0 == (parityByte & parityMask));
        }

        private int CalcNumberOfTrueBits(byte parityByte, int parityWordSize)
        {
            byte parityMask = 0x80;
            int numTrueBits = 0;

            //Debug.WriteLine("parityByte={0}", System.Convert.ToString(parityByte, 16));
            parityByte <<= 8 - parityWordSize;

            for (int i = 0; i < parityWordSize; i++)
            {
                if (0 != (parityByte & parityMask))
                    numTrueBits++;

                parityByte <<= 1;
            }

            //Debug.WriteLine("Number of true bits: {0}", numTrueBits);
            return numTrueBits;
        }
        #endregion
    }
}
