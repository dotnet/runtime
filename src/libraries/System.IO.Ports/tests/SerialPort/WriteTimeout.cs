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
    public class WriteTimeout_Property : PortsTest
    {
        //The default number of chars to write with when testing timeout with Write(char[], int, int)
        private static readonly int s_DEFAULT_WRITE_CHAR_ARRAY_SIZE = TCSupport.MinimumBlockingByteCount;

        //The default number of bytes to write with when testing timeout with Write(byte[], int, int)
        private static readonly int s_DEFAULT_WRITE_BYTE_ARRAY_SIZE = TCSupport.MinimumBlockingByteCount;

        //The ammount of time to wait when expecting an infinite timeout
        private const int DEFAULT_WAIT_INFINITE_TIMEOUT = 250;

        //The maximum acceptable time allowed when a write method should timeout immediately
        private const int MAX_ACCEPTABLE_ZERO_TIMEOUT = 100;

        //The maximum acceptable time allowed when a write method should timeout immediately when it is called for the first time
        private const int MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT = 5000;

        //The default string to write with when testing timeout with Write(str)
        private static readonly string s_DEFAULT_STRING_TO_WRITE = new string('H', TCSupport.MinimumBlockingByteCount);
        private const int NUM_TRYS = 5;

        private delegate void WriteMethodDelegate(SerialPort com);

        private enum ThrowAt { Set, Open };

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Default_Write_byte_int_int()
        {
            Debug.WriteLine("Verifying default WriteTimeout with Write(byte[] buffer, int offset, int count)");
            VerifyInfiniteTimeout(Write_byte_int_int, false);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Default_Write_char_int_int()
        {
            Debug.WriteLine("Verifying default WriteTimeout with Write(char[] buffer, int offset, int count)");
            VerifyInfiniteTimeout(Write_char_int_int, false);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Default_Write_str()
        {
            Debug.WriteLine("Verifying default WriteTimeout with Write(string)");
            VerifyInfiniteTimeout(Write_str, false);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Default_WriteLine()
        {
            Debug.WriteLine("Verifying default WriteTimeout with WriteLine()");
            VerifyInfiniteTimeout(WriteLine, false);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Infinite_Write_byte_int_int()
        {
            Debug.WriteLine("Verifying infinite WriteTimeout with Write(byte[] buffer, int offset, int count)");
            VerifyInfiniteTimeout(Write_byte_int_int, true);
        }

        [ActiveIssue(15961)]
        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Infinite_Write_char_int_int()
        {
            Debug.WriteLine("Verifying infinite WriteTimeout with Write(char[] buffer, int offset, int count)");
            VerifyInfiniteTimeout(Write_char_int_int, true);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Infinite_Write_str()
        {
            Debug.WriteLine("Verifying infinite WriteTimeout with Write(string)");
            VerifyInfiniteTimeout(Write_str, true);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_Infinite_WriteLine()
        {
            Debug.WriteLine("Verifying infinite WriteTimeout with WriteLine()");
            VerifyInfiniteTimeout(WriteLine, true);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_1_Write_byte_int_int_BeforeOpen()
        {
            Debug.WriteLine("Verifying setting WriteTimeout=1 before Open() with Write(byte[] buffer, int offset, int count)");
            Verify1TimeoutBeforeOpen(Write_byte_int_int);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_1_Write_char_int_int_BeforeOpen()
        {
            Debug.WriteLine("Verifying setting WriteTimeout=1 before Open() with Write(char[] buffer, int offset, int count)");
            Verify1TimeoutBeforeOpen(Write_char_int_int);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_1_Write_str_BeforeOpen()
        {
            Debug.WriteLine("Verifying 1 WriteTimeout before Open with Write(string)");
            Verify1TimeoutBeforeOpen(Write_str);
        }


        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_1_WriteLine_BeforeOpen()
        {
            Debug.WriteLine("Verifying 1 WriteTimeout before Open with WriteLine()");
            Verify1TimeoutBeforeOpen(WriteLine);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_1_Write_byte_int_int_AfterOpen()
        {
            Debug.WriteLine("Verifying setting WriteTimeout=1 after Open() with Write(byte[] buffer, int offset, int count)");
            Verify1TimeoutAfterOpen(Write_byte_int_int);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_1_Write_char_int_int_AfterOpen()
        {
            Debug.WriteLine("Verifying setting WriteTimeout=1 after Open() with Write(char[] buffer, int offset, int count)");
            Verify1TimeoutAfterOpen(Write_char_int_int);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_1_Write_str_AfterOpen()
        {
            Debug.WriteLine("Verifying 1 WriteTimeout after Open with Write(string)");
            Verify1TimeoutAfterOpen(Write_str);
        }

        [ConditionalFact(nameof(HasOneSerialPort), nameof(HasHardwareFlowControl))]
        public void WriteTimeout_1_WriteLine_AfterOpen()
        {
            Debug.WriteLine("Verifying 1 WriteTimeout after Open with WriteLine()");
            Verify1TimeoutAfterOpen(WriteLine);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteTimeout_Int32MinValue()
        {
            Debug.WriteLine("Verifying Int32.MinValue WriteTimeout");
            VerifyException(int.MinValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void WriteTimeout_NEG2()
        {
            Debug.WriteLine("Verifying -2 WriteTimeout");
            VerifyException(int.MinValue, ThrowAt.Set, typeof(ArgumentOutOfRangeException));
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyInfiniteTimeout(WriteMethodDelegate writeMethod, bool setInfiniteTimeout)
        {
            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                serPortProp.SetAllPropertiesToOpenDefaults();
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

                com1.Handshake = Handshake.RequestToSend;

                serPortProp.SetProperty("ReadTimeout", 10);
                com1.ReadTimeout = 10;

                com1.Open();

                if (setInfiniteTimeout)
                {
                    com1.WriteTimeout = 500;
                    com1.WriteTimeout = SerialPort.InfiniteTimeout;
                }

                Task task = Task.Run(() => writeMethod(com1));
                Thread.Sleep(DEFAULT_WAIT_INFINITE_TIMEOUT);

                Assert.False(task.IsCompleted, "Task should not have completed while tx is blocked by flow-control");

                com1.Handshake = Handshake.None;

                TCSupport.WaitForTaskCompletion(task);

                com1.DiscardOutBuffer();
                // If we're looped-back, then there will be data queued on the receive side which we need to discard
                com1.DiscardInBuffer();
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        private void Verify1TimeoutBeforeOpen(WriteMethodDelegate writeMethod)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.WriteTimeout = 1;
                com.Open();

                Verify1Timeout(com, writeMethod);
            }
        }

        private void Verify1TimeoutAfterOpen(WriteMethodDelegate writeMethod)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                com.WriteTimeout = 1;

                Verify1Timeout(com, writeMethod);
            }
        }

        private void Verify1Timeout(SerialPort com, WriteMethodDelegate writeMethod)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();
            Stopwatch sw = new Stopwatch();

            int actualTime = 0;

            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
            serPortProp.SetProperty("WriteTimeout", 1);

            serPortProp.SetProperty("Handshake", Handshake.RequestToSend);
            com.Handshake = Handshake.RequestToSend;

            serPortProp.SetProperty("ReadTimeout", 1000);
            com.ReadTimeout = 1000;

            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            sw.Start();
            writeMethod(com);
            sw.Stop();

            if (MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT < sw.ElapsedMilliseconds)
            {
                Fail("Err_2570ajdlkj!!! Write Method {0} timed out in {1}ms expected something less then {2}ms", writeMethod.Method.Name, sw.ElapsedMilliseconds, MAX_ACCEPTABLE_WARMUP_ZERO_TIMEOUT);
            }
            sw.Reset();

            for (int i = 0; i < NUM_TRYS; i++)
            {
                sw.Start();
                writeMethod(com);
                sw.Stop();

                actualTime += (int)sw.ElapsedMilliseconds;
                sw.Reset();
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;
            actualTime /= NUM_TRYS;

            if (MAX_ACCEPTABLE_ZERO_TIMEOUT < actualTime)
            {
                Fail("ERROR!!! Write Method {0} timed out in {1}ms expected something less then {2}ms", writeMethod.Method.Name, actualTime, MAX_ACCEPTABLE_ZERO_TIMEOUT);
            }

            serPortProp.VerifyPropertiesAndPrint(com);
        }


        private void VerifyException(int writeTimeout, ThrowAt throwAt, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, writeTimeout, throwAt, expectedException);

                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, writeTimeout, expectedException);
            }
        }

        private void VerifyExceptionAtOpen(SerialPort com, int writeTimeout, ThrowAt throwAt, Type expectedException)
        {
            int origWriteTimeout = com.WriteTimeout;

            SerialPortProperties serPortProp = new SerialPortProperties();

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            if (ThrowAt.Open == throwAt)
                serPortProp.SetProperty("WriteTimeout", writeTimeout);

            try
            {
                com.WriteTimeout = writeTimeout;

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
            com.WriteTimeout = origWriteTimeout;
        }


        private void VerifyExceptionAfterOpen(SerialPort com, int writeTimeout, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.WriteTimeout = writeTimeout;

                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the WriteTimeout after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the WriteTimeout after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the WriteTimeout after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }
            serPortProp.VerifyPropertiesAndPrint(com);
        }


        private void Write_byte_int_int(SerialPort com)
        {
            try
            {
                com.Write(new byte[s_DEFAULT_WRITE_BYTE_ARRAY_SIZE], 0, s_DEFAULT_WRITE_BYTE_ARRAY_SIZE);
            }
            catch (TimeoutException)
            {
            }
        }


        private void Write_char_int_int(SerialPort com)
        {
            try
            {
                com.Write(new char[s_DEFAULT_WRITE_CHAR_ARRAY_SIZE], 0, s_DEFAULT_WRITE_CHAR_ARRAY_SIZE);
            }
            catch (TimeoutException)
            {
            }
        }


        private void Write_str(SerialPort com)
        {
            try
            {
                com.Write(s_DEFAULT_STRING_TO_WRITE);
            }
            catch (TimeoutException)
            {
            }
        }


        private void WriteLine(SerialPort com)
        {
            try
            {
                com.WriteLine(s_DEFAULT_STRING_TO_WRITE);
            }
            catch (TimeoutException)
            {
            }
        }

        #endregion
    }
}
