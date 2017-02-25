// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Threading;
using System.Threading.Tasks;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class ReadTimeout_Property : PortsTest
    {
        //The default number of chars to write with when testing timeout with Read(char[], int, int)
        private const int DEFAULT_READ_CHAR_ARRAY_SIZE = 8;

        //The default number of bytes to write with when testing timeout with Read(byte[], int, int)
        private const int DEFAULT_READ_BYTE_ARRAY_SIZE = 8;

        //The ammount of time to wait when expecting an infinite timeout
        private const int DEFAULT_WAIT_INFINITE_TIMEOUT = 250;

        //The maximum acceptable time allowed when a read method should timeout immediately
        private const int MAX_ACCEPTABLE_ZERO_TIMEOUT = 100;

        //The maximum acceptable time allowed when a read method should timeout immediately when it is called for the first time
        private const int MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT = 1000;
        private const int NUM_TRYS = 5;

        private enum ThrowAt { Set, Open };

        #region Test Cases

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Default_Read_byte_int_int()
        {
            Debug.WriteLine("Verifying default ReadTimeout with Read(byte[] buffer, int offset, int count)");
            VerifyInfiniteTimeout(Read_byte_int_int, false);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Default_Read_char_int_int()
        {
            Debug.WriteLine("Verifying default ReadTimeout with Read(char[] buffer, int offset, int count)");
            VerifyInfiniteTimeout(Read_char_int_int, false);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Default_ReadByte()
        {
            Debug.WriteLine("Verifying default ReadTimeout with ReadByte()");
            VerifyInfiniteTimeout(ReadByte, false);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Default_ReadLine()
        {
            Debug.WriteLine("Verifying default ReadTimeout with ReadLine()");
            VerifyInfiniteTimeout(ReadLine, false);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Default_ReadTo()
        {
            Debug.WriteLine("Verifying default ReadTimeout with ReadTo()");
            VerifyInfiniteTimeout(ReadTo, false);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Infinite_Read_byte_int_int()
        {
            Debug.WriteLine("Verifying infinite ReadTimeout with Read(byte[] buffer, int offset, int count)");
            VerifyInfiniteTimeout(Read_byte_int_int, true);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Infinite_Read_char_int_int()
        {
            Debug.WriteLine("Verifying infinite ReadTimeout with Read(char[] buffer, int offset, int count)");
            VerifyInfiniteTimeout(Read_char_int_int, true);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Infinite_ReadByte()
        {
            Debug.WriteLine("Verifying infinite ReadTimeout with ReadByte()");
            VerifyInfiniteTimeout(ReadByte, true);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Infinite_ReadLine()
        {
            Debug.WriteLine("Verifying infinite ReadTimeout with ReadLine()");
            VerifyInfiniteTimeout(ReadLine, true);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void ReadTimeout_Infinite_ReadTo()
        {
            Debug.WriteLine("Verifying infinite ReadTimeout with ReadTo()");
            VerifyInfiniteTimeout(ReadTo, true);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_Read_byte_int_int_BeforeOpen()
        {
            Debug.WriteLine("Verifying setting ReadTimeout=0 before Open() with Read(byte[] buffer, int offset, int count)");
            VerifyZeroTimeoutBeforeOpen(Read_byte_int_int);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_Read_char_int_int_BeforeOpen()
        {
            Debug.WriteLine("Verifying setting ReadTimeout=0 before Open() with Read(char[] buffer, int offset, int count)");
            VerifyZeroTimeoutBeforeOpen(Read_char_int_int);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_ReadByte_BeforeOpen()
        {
            Debug.WriteLine("Verifying zero ReadTimeout before Open with ReadByte()");
            VerifyZeroTimeoutBeforeOpen(ReadByte);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_ReadLine_BeforeOpen()
        {
            Debug.WriteLine("Verifying zero ReadTimeout before Open with ReadLine()");
            VerifyZeroTimeoutBeforeOpen(ReadLine);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_ReadTo_BeforeOpen()
        {
            Debug.WriteLine("Verifying zero ReadTimeout before Open with ReadTo()");
            VerifyZeroTimeoutBeforeOpen(ReadTo);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_Read_byte_int_int_AfterOpen()
        {
            Debug.WriteLine("Verifying setting ReadTimeout=0 after Open() with Read(byte[] buffer, int offset, int count)");
            VerifyZeroTimeoutAfterOpen(Read_byte_int_int);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_Read_char_int_int_AfterOpen()
        {
            Debug.WriteLine("Verifying setting ReadTimeout=0 after Open() with Read(char[] buffer, int offset, int count)");
            VerifyZeroTimeoutAfterOpen(Read_char_int_int);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_ReadByte_AfterOpen()
        {
            Debug.WriteLine("Verifying zero ReadTimeout after Open with ReadByte()");
            VerifyZeroTimeoutAfterOpen(ReadByte);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_ReadLine_AfterOpen()
        {
            Debug.WriteLine("Verifying zero ReadTimeout after Open with ReadLine()");
            VerifyZeroTimeoutAfterOpen(ReadLine);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_0_ReadTo_AfterOpen()
        {
            Debug.WriteLine("Verifying zero ReadTimeout after Open with ReadTo()");
            VerifyZeroTimeoutAfterOpen(ReadTo);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_Int32MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue ReadTimeout");

            VerifyException(int.MinValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void ReadTimeout_NEG2()
        {
            Debug.WriteLine("Verifying -2 ReadTimeout");

            VerifyException(int.MinValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }
        #endregion

        #region Verification for Test Cases
        public void VerifyInfiniteTimeout(Action<SerialPort> readMethod, bool setInfiniteTimeout)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
                serPortProp.SetProperty("WriteTimeout", 10);

                com1.WriteTimeout = 10;
                com1.Open();

                serPortProp.VerifyPropertiesAndPrint(com1);

                if (!com2.IsOpen)
                    com2.Open();

                if (setInfiniteTimeout)
                {
                    com1.ReadTimeout = 500;
                    com1.ReadTimeout = SerialPort.InfiniteTimeout;
                }

                Task task = Task.Run(() => readMethod(com1));

                Thread.Sleep(DEFAULT_WAIT_INFINITE_TIMEOUT);

                Assert.True(!task.IsCompleted);

                serPortProp.VerifyPropertiesAndPrint(com1);

                com2.WriteLine(string.Empty);

                TCSupport.WaitForTaskCompletion(task);
            }
        }

        private void VerifyZeroTimeoutBeforeOpen(Action<SerialPort> readMethod)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.ReadTimeout = 0;
                com.Open();

                VerifyZeroTimeout(com, readMethod);
            }
        }

        private void VerifyZeroTimeoutAfterOpen(Action<SerialPort> readMethod)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                com.ReadTimeout = 0;

                VerifyZeroTimeout(com, readMethod);
            }
        }

        private void VerifyZeroTimeout(SerialPort com, Action<SerialPort> readMethod)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();
            Stopwatch sw = new Stopwatch();

            int actualTime = 0;

            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            serPortProp.SetProperty("ReadTimeout", 0);
            serPortProp.SetProperty("WriteTimeout", 1000);

            com.WriteTimeout = 1000;

            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            sw.Start();
            readMethod(com);
            sw.Stop();

            if (MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT < sw.ElapsedMilliseconds)
            {
                Fail("Err_2570ajdlkj!!! Read Method {0} timed out in {1}ms expected something less then {2}ms", readMethod.Method.Name, sw.ElapsedMilliseconds, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT);
            }

            sw.Reset();

            for (int i = 0; i < NUM_TRYS; i++)
            {
                sw.Start();
                readMethod(com);
                sw.Stop();

                actualTime += (int)sw.ElapsedMilliseconds;
                sw.Reset();
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            actualTime /= NUM_TRYS;

            if (MAX_ACCEPTABLE_ZERO_TIMEOUT < actualTime)
            {
                Fail("ERROR!!! Read Method {0} timed out in {1}ms expected something less then {2}ms", readMethod.Method.Name, actualTime, MAX_ACCEPTABLE_ZERO_TIMEOUT);
            }

            serPortProp.VerifyPropertiesAndPrint(com);
            com.ReadTimeout = 0;
        }


        private void VerifyException(int readTimeout, ThrowAt throwAt, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, readTimeout, throwAt, expectedException);

                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, readTimeout, expectedException);
            }
        }


        private void VerifyExceptionAtOpen(SerialPort com, int readTimeout, ThrowAt throwAt, Type expectedException)
        {
            int origReadTimeout = com.ReadTimeout;

            SerialPortProperties serPortProp = new SerialPortProperties();

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            if (ThrowAt.Open == throwAt)
                serPortProp.SetProperty("ReadTimeout", readTimeout);

            try
            {
                com.ReadTimeout = readTimeout;

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
            com.ReadTimeout = origReadTimeout;
        }


        private void VerifyExceptionAfterOpen(SerialPort com, int readTimeout, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.ReadTimeout = readTimeout;
                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the ReadTimeout after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the ReadTimeout after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the ReadTimeout after Open() throw {0} and {1} was thrown",
                        expectedException, e.GetType());
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);
        }

        private void Read_byte_int_int(SerialPort com)
        {
            try
            {
                com.Read(new byte[DEFAULT_READ_BYTE_ARRAY_SIZE], 0, DEFAULT_READ_BYTE_ARRAY_SIZE);
            }
            catch (TimeoutException) { }
        }


        private void Read_char_int_int(SerialPort com)
        {
            try
            {
                com.Read(new char[DEFAULT_READ_CHAR_ARRAY_SIZE], 0, DEFAULT_READ_CHAR_ARRAY_SIZE);
            }
            catch (TimeoutException) { }
        }


        private void ReadByte(SerialPort com)
        {
            try
            {
                com.ReadByte();
            }
            catch (TimeoutException) { }
        }


        private void ReadChar(SerialPort com)
        {
            try
            {
                com.ReadChar();
            }
            catch (TimeoutException) { }
        }


        private void ReadLine(SerialPort com)
        {
            try
            {
                com.ReadLine();
            }
            catch (TimeoutException) { }
        }


        private void ReadTo(SerialPort com)
        {
            try
            {
                com.ReadTo(com.NewLine);
            }
            catch (TimeoutException) { }
        }

        #endregion
    }
}
