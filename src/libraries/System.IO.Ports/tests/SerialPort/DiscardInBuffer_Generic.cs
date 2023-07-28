// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class DiscardInBuffer_Generic : PortsTest
    {
        #region Test Cases

        [Fact]
        public void DiscardWithoutOpen()
        {
            using (SerialPort com = new SerialPort())
            {
                Debug.WriteLine("Verifying Discard method throws exception without a call to Open()");
                VerifyDiscardException(com, typeof(InvalidOperationException));
            }
        }


        [Fact]
        public void DiscardAfterFailedOpen()
        {
            using (SerialPort com = new SerialPort("BAD_PORT_NAME"))
            {
                Debug.WriteLine("Verifying read Discard throws exception with a failed call to Open()");

                //Since the PortName is set to a bad port name Open will thrown an exception
                //however we don't care what it is since we are verifying a read method
                Assert.ThrowsAny<Exception>(() => com.Open());
                VerifyDiscardException(com, typeof(InvalidOperationException));
            }
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DiscardAfterClose()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying Discard method throws exception after a call to Close()");

                com.Open();
                com.Close();

                VerifyDiscardException(com, typeof(InvalidOperationException));
            }
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void DiscardAfterOpen()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying Discard method does not throw an exception after a call to Open()");

                com.Open();
                VerifyDiscardException(com, null);

                Assert.Equal(0, com.BytesToRead);
            }
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyDiscardException(SerialPort com, Type expectedException)
        {
            try
            {
                com.DiscardInBuffer();
                if (null != expectedException)
                {
                    Fail("ERROR!!!: No Exception was thrown");
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!!: No Exception was expected and {0} was thrown", e.GetType());
                }

                if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!!: {0} exception was thrown expected {1}", e.GetType(), expectedException);
                }
            }
        }
        #endregion
    }
}
