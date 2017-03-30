// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class NewLine_Property : PortsTest
    {
        #region Test Cases

        [ConditionalFact(nameof(HasNullModem))]
        public void NewLine_Default()
        {
            using (SerialPort com1 = new SerialPort())
            {
                SerialPortProperties serPortProp = new SerialPortProperties();

                Debug.WriteLine("Verifying default NewLine");
                serPortProp.SetAllPropertiesToDefaults();
                serPortProp.VerifyPropertiesAndPrint(com1);
            }
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void NewLine_null()
        {
            Debug.WriteLine("Verifying null NewLine");
            VerifyException(null, typeof(ArgumentNullException));
        }

        [ConditionalFact(nameof(HasNullModem))]
        public void NewLine_empty_string()
        {
            Debug.WriteLine("Verifying empty string NewLine");
            VerifyException("", typeof(ArgumentException));
        }
        #endregion

        #region Verification for Test Cases
        private void VerifyException(string newLine, Type expectedException)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, newLine, expectedException);

                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, newLine, expectedException);
            }
        }

        private void VerifyExceptionAtOpen(SerialPort com, string newLine, Type expectedException)
        {
            string origNewLine = com.NewLine;
            SerialPortProperties serPortProp = new SerialPortProperties();

            serPortProp.SetAllPropertiesToDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.NewLine = newLine;

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
            com.NewLine = origNewLine;
        }


        private void VerifyExceptionAfterOpen(SerialPort com, string newLine, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.NewLine = newLine;

                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the NewLine after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the NewLine after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the NewLine after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);
        }
        #endregion
    }
}
