// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class PortName_Property : PortsTest
    {
        //Determines how long the randomly generated PortName is
        private const int rndPortNameSize = 255;

        private enum ThrowAt { Set, Open };

        private readonly DosDevices _dosDevices;

        public PortName_Property()
        {
            _dosDevices = new DosDevices();
        }

        #region Test Cases

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_COM1_After_Open()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                Debug.WriteLine("Verifying setting PortName=COM1 after open has been called");

                VerifyExceptionAfterOpen(com, TCSupport.LocalMachineSerialInfo.FirstAvailablePortName, typeof(InvalidOperationException));
            }
        }


        [ConditionalFact(nameof(HasTwoSerialPorts))]
        public void PortName_COM2_After_Open()
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName))
            {
                Debug.WriteLine("Verifying setting PortName=COM2 after open has been called");

                VerifyExceptionAfterOpen(com, TCSupport.LocalMachineSerialInfo.SecondAvailablePortName, typeof(InvalidOperationException));
            }
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_Empty()
        {
            Debug.WriteLine("Verifying setting PortName=\"\"");
            VerifyException("", ThrowAt.Set, typeof(ArgumentException), typeof(ArgumentException));
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_null()
        {
            Debug.WriteLine("Verifying setting PortName=null");
            VerifyException(null, ThrowAt.Set, typeof(ArgumentNullException), typeof(ArgumentNullException));
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_RND()
        {
            Random rndGen = new Random();
            StringBuilder rndStrBuf = new StringBuilder();

            for (int i = 0; i < rndPortNameSize; i++)
            {
                rndStrBuf.Append((char)rndGen.Next(0, ushort.MaxValue));
            }

            Debug.WriteLine("Verifying setting PortName to a random string");
            VerifyException(rndStrBuf.ToString(), ThrowAt.Open, typeof(ArgumentException), typeof(InvalidOperationException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_FileName()
        {
            string fileName = "PortNameEqualToFileName.txt";
            FileStream testFile = File.Open(fileName, FileMode.Create);
            ASCIIEncoding asciiEncd = new ASCIIEncoding();
            string testStr = "Hello World";

            testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));

            testFile.Close();
            Debug.WriteLine("Verifying setting PortName={0}", fileName);

            VerifyException(fileName, ThrowAt.Open, typeof(ArgumentException), typeof(InvalidOperationException));

            Debug.WriteLine("Verifying setting PortName={0}", Environment.CurrentDirectory + fileName);

            VerifyException(Environment.CurrentDirectory + fileName, ThrowAt.Open, typeof(ArgumentException),
                typeof(InvalidOperationException));

            File.Delete(fileName);
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_COM257()
        {
            Debug.WriteLine("Verifying setting PortName=COM257");
            VerifyException("COM257", ThrowAt.Open, typeof(IOException), typeof(InvalidOperationException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_LPT()
        {
            Type expectedException = _dosDevices.CommonNameExists("LPT") ? typeof(ArgumentException) : typeof(ArgumentException);

            Debug.WriteLine("Verifying setting PortName=LPT");
            VerifyException("LPT", ThrowAt.Open, expectedException, typeof(InvalidOperationException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_LPT1()
        {
            Type expectedException = _dosDevices.CommonNameExists("LPT1") ? typeof(ArgumentException) : typeof(ArgumentException);

            Debug.WriteLine("Verifying setting PortName=LPT1");
            VerifyException("LPT1", ThrowAt.Open, expectedException, typeof(InvalidOperationException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_PHYSICALDRIVE0()
        {
            Type expectedException = _dosDevices.CommonNameExists("PHYSICALDRIVE0") ? typeof(ArgumentException) : typeof(ArgumentException);

            Debug.WriteLine("Verifying setting PortName=PHYSICALDRIVE0");
            VerifyException("PHYSICALDRIVE0", ThrowAt.Open, expectedException, typeof(InvalidOperationException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_A()
        {
            Type expectedException = _dosDevices.CommonNameExists("A:") ? typeof(ArgumentException) : typeof(ArgumentException);

            Debug.WriteLine("Verifying setting PortName=A:");
            VerifyException("A:", ThrowAt.Open, expectedException, typeof(InvalidOperationException));
        }


        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_C()
        {
            Debug.WriteLine("Verifying setting PortName=C:");

            VerifyException("C:", ThrowAt.Open, new[] { typeof(ArgumentException), typeof(ArgumentException) }, typeof(InvalidOperationException));
        }

        [ConditionalFact(nameof(HasOneSerialPort))]
        public void PortName_SystemDrive()
        {
            Debug.WriteLine("Verifying setting PortName=%SYSTEMDRIVE%");
            string portName = Environment.GetEnvironmentVariable("SystemDrive");

            if (!string.IsNullOrEmpty(portName))
            {
                VerifyException(portName, ThrowAt.Open, new[] { typeof(ArgumentException) }, typeof(InvalidOperationException));
            }
        }
        #endregion

        #region Verification for Test Cases

        private void VerifyException(string portName, ThrowAt throwAt, Type expectedExceptionAtOpen, Type expectedExceptionAfterOpen)
        {
            VerifyException(portName, throwAt, new[] { expectedExceptionAtOpen }, expectedExceptionAfterOpen);
        }

        private void VerifyException(string portName, ThrowAt throwAt, Type[] expectedExceptionAtOpen, Type expectedExceptionAfterOpen)
        {
            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                VerifyExceptionAtOpen(com, portName, throwAt, expectedExceptionAtOpen);

                if (com.IsOpen)
                    com.Close();

                VerifyExceptionAfterOpen(com, portName, expectedExceptionAfterOpen);
            }
        }


        private void VerifyExceptionAtOpen(SerialPort com, string portName, ThrowAt throwAt, Type expectedException)
        {
            VerifyExceptionAtOpen(com, portName, throwAt, new[] { expectedException });
        }

        private void VerifyExceptionAtOpen(SerialPort com, string portName, ThrowAt throwAt, Type[] expectedExceptions)
        {
            string origPortName = com.PortName;

            SerialPortProperties serPortProp = new SerialPortProperties();

            if (null != expectedExceptions && 0 < expectedExceptions.Length)
            {
                serPortProp.SetAllPropertiesToDefaults();
            }
            else
            {
                serPortProp.SetAllPropertiesToOpenDefaults();
            }

            if (ThrowAt.Open == throwAt)
                serPortProp.SetProperty("PortName", portName);
            else
                serPortProp.SetProperty("PortName", TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            try
            {
                com.PortName = portName;

                if (ThrowAt.Open == throwAt)
                    com.Open();

                if (null != expectedExceptions && 0 < expectedExceptions.Length)
                {
                    Fail("ERROR!!! Expected Open() to throw ");
                    for (int i = 0; i < expectedExceptions.Length; ++i) Console.Write(expectedExceptions[i] + " ");
                    Debug.WriteLine(" and nothing was thrown");
                }
            }
            catch (Exception e)
            {
                if (null == expectedExceptions || 0 == expectedExceptions.Length)
                {
                    Fail("ERROR!!! Expected Open() NOT to throw an exception and the following was thrown:\n{0}", e);
                }
                else
                {
                    bool exceptionFound = false;
                    Type actualExceptionType = e.GetType();

                    for (int i = 0; i < expectedExceptions.Length; ++i)
                    {
                        if (actualExceptionType == expectedExceptions[i])
                        {
                            exceptionFound = true;
                            break;
                        }
                    }

                    if (exceptionFound)
                    {
                        Debug.WriteLine("Caught expected exception:\n{0}", e.GetType());
                    }
                    else
                    {
                        Fail("ERROR!!! Expected Open() throw ");
                        for (int i = 0; i < expectedExceptions.Length; ++i) Console.Write(expectedExceptions[i] + " ");
                        Debug.WriteLine(" and  the following was thrown:\n{0}", e);
                    }
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);
            com.PortName = origPortName;
        }


        private void VerifyExceptionAfterOpen(SerialPort com, string portName, Type expectedException)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            com.Open();
            serPortProp.SetAllPropertiesToOpenDefaults();
            serPortProp.SetProperty("PortName", com.PortName);

            try
            {
                com.PortName = portName;
                if (null != expectedException)
                {
                    Fail("ERROR!!! Expected setting the PortName after Open() to throw {0} and nothing was thrown", expectedException);
                }
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("ERROR!!! Expected setting the PortName after Open() NOT to throw an exception and {0} was thrown", e.GetType());
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("ERROR!!! Expected setting the PortName after Open() throw {0} and {1} was thrown", expectedException, e.GetType());
                }
            }

            serPortProp.VerifyPropertiesAndPrint(com);
        }
        #endregion
    }
}
