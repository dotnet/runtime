// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_Seek : PortsTest
    {
        private const int DEFAULT_OFFSET = 0;
        private const int DEFAULT_ORIGIN = (int)SeekOrigin.Begin;
        private const int BAD_OFFSET = -1;
        private const int BAD_ORIGIN = -1;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Seek_Open_Close()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.Close();

                Debug.WriteLine("Verifying Seek property throws exception After Open() then Close()");

                VerifySeekException(serialStream, DEFAULT_OFFSET, DEFAULT_ORIGIN, typeof(NotSupportedException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Seek_Open_BaseStreamClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.BaseStream.Close();

                Debug.WriteLine("Verifying Seek property throws exception After Open() then BaseStream.Close()");

                VerifySeekException(serialStream, DEFAULT_OFFSET, DEFAULT_ORIGIN, typeof(NotSupportedException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Seek_AfterOpen()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();

                Debug.WriteLine("Verifying seek method throws exception after a call to Open()");

                VerifySeekException(com.BaseStream, DEFAULT_OFFSET, DEFAULT_ORIGIN, typeof(NotSupportedException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Seek_BadOffset()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();

                Debug.WriteLine("Verifying seek method throws exception with a bad offset after a call to Open()");

                VerifySeekException(com.BaseStream, BAD_OFFSET, DEFAULT_ORIGIN, typeof(NotSupportedException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Seek_BadOrigin()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();

                Debug.WriteLine("Verifying seek method throws exception with a bad origin after a call to Open()");

                VerifySeekException(com.BaseStream, DEFAULT_OFFSET, BAD_ORIGIN, typeof(NotSupportedException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Seek_BadOffset_BadOrigin()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();

                Debug.WriteLine(
                    "Verifying seek method throws exception with a bad offset/origin after a call to Open()");

                VerifySeekException(com.BaseStream, BAD_OFFSET, BAD_ORIGIN, typeof(NotSupportedException));
            }
        }

        #endregion

        #region Verification for Test Cases
        private void VerifySeekException(Stream serialStream, long offset, int origin, Type expectedException)
        {
            Assert.Throws(expectedException, () => serialStream.Seek(offset, (SeekOrigin)origin));
        }
        #endregion
    }
}