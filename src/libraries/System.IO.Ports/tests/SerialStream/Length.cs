// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialStream_Length : PortsTest
    {
        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Length_Open_Close()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.Close();

                Debug.WriteLine("Verifying Length property throws exception After Open() then Close()");

                VerifyLengthException(serialStream, typeof(NotSupportedException));
            }
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Length_Open_BaseStreamClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();
                Stream serialStream = com.BaseStream;
                com.BaseStream.Close();

                Debug.WriteLine("Verifying Length property throws exception After Open() then BaseStream.Close()");

                VerifyLengthException(serialStream, typeof(NotSupportedException));
            }
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void Length_AfterOpen()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();

                Debug.WriteLine("Verifying Length property throws exception after a call to Open()");

                VerifyLengthException(com.BaseStream, typeof(NotSupportedException));
            }
        }

        #endregion

        #region Verification for Test Cases
        private void VerifyLengthException(Stream serialStream, Type expectedException)
        {
            Assert.Throws(expectedException, () =>
            {
                _ = serialStream.Length;
            });
        }
        #endregion
    }
}
