// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_Position : PortsTest
    {
        private const int DEFAULT_VALUE = 0;
        private const int BAD_VALUE = -1;

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Position_Open_Close()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.Close();

                Debug.WriteLine("Verifying Position property throws exception After Open() then Close()");

                VerifyPositionException(serialStream, DEFAULT_VALUE, typeof(NotSupportedException));
            }
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Position_Open_BaseStreamClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.BaseStream.Close();

                Debug.WriteLine("Verifying Position property throws exception After Open() then BaseStream.Close()");

                VerifyPositionException(serialStream, DEFAULT_VALUE, typeof(NotSupportedException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Position_AfterOpen()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Debug.WriteLine("Verifying Position property throws exception after a call to Open()");

                VerifyPositionException(com.BaseStream, DEFAULT_VALUE, typeof(NotSupportedException));
            }
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Position_BadValue()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Debug.WriteLine("Verifying Position property throws exception with a bad value after a call to Open()");

                VerifyPositionException(com.BaseStream, BAD_VALUE, typeof(NotSupportedException));
            }
        }

        #endregion

        #region Verification for Test Cases
        private void VerifyPositionException(Stream serialStream, long value, Type expectedException)
        {
            Assert.Throws(expectedException, () => serialStream.Position = value);
            Assert.Throws(expectedException, () =>
            {
                long positionTest = serialStream.Position;
            });
        }
        #endregion
    }
}