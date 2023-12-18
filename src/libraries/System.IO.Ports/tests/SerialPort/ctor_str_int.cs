// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.PortsTests;
using System.Text;
using Legacy.Support;
using Xunit;
using Xunit.Sdk;

namespace System.IO.Ports.Tests
{
    public class ctor_str_int : PortsTest
    {
        private enum ThrowAt { Set, Open };

        [Fact]
        public void COM1_9600()
        {
            string portName = "COM1";
            int baudRate = 9600;

            VerifyCtor(portName, baudRate);
        }


        [Fact]
        public void COM2_14400()
        {
            string portName = "COM2";
            int baudRate = 14400;

            VerifyCtor(portName, baudRate);
        }


        [Fact]
        public void COM3_28800()
        {
            string portName = "COM3";
            int baudRate = 28800;

            VerifyCtor(portName, baudRate);
        }


        [Fact]
        public void COM4_57600()
        {
            string portName = "COM4";
            int baudRate = 57600;

            VerifyCtor(portName, baudRate);
        }


        [Fact]
        public void COM256_115200()
        {
            string portName = "COM256";
            int baudRate = 115200;

            VerifyCtor(portName, baudRate);
        }


        //[] Error checking for PortName
        [Fact]
        public void Empty_9600()
        {
            string portName = string.Empty;
            int baudRate = 9600;

            VerifyCtor(portName, baudRate, typeof(ArgumentException), ThrowAt.Set);
        }

        [Fact]
        public void Null_14400()
        {
            string portName = null;
            int baudRate = 14400;

            VerifyCtor(portName, baudRate, typeof(ArgumentNullException), ThrowAt.Set);
        }

        [Fact]
        public void COM257_57600()
        {
            string portName = "COM257";
            int baudRate = 57600;

            VerifyCtor(portName, baudRate);
        }

        [Fact]
        public void Filename_9600()
        {
            string portName;
            int baudRate = 9600;
            string fileName = portName = GetTestFilePath();
            FileStream testFile = File.Open(fileName, FileMode.Create);
            ASCIIEncoding asciiEncd = new ASCIIEncoding();
            string testStr = "Hello World";

            testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));
            testFile.Close();
            try
            {
                VerifyCtor(portName, baudRate, typeof(ArgumentException), ThrowAt.Open);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                File.Delete(fileName);
            }
        }


        [Fact]
        public void PHYSICALDRIVE0_14400()
        {
            string portName = "PHYSICALDRIVE0";
            int baudRate = 14400;

            VerifyCtor(portName, baudRate, typeof(ArgumentException), ThrowAt.Open);
        }


        //[] Error checking for BaudRate
        [Fact]
        public void COM1_Int32MinValue()
        {
            string portName = "Com1";
            int baudRate = int.MinValue;

            VerifyCtor(portName, baudRate, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM2_Neg1()
        {
            string portName = "Com2";
            int baudRate = -1;

            VerifyCtor(portName, baudRate, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM3_0()
        {
            string portName = "Com3";
            int baudRate = 0;

            VerifyCtor(portName, baudRate, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM4_Int32MaxValue()
        {
            string portName = "Com4";
            int baudRate = int.MaxValue;

            VerifyCtor(portName, baudRate, typeof(ArgumentOutOfRangeException), ThrowAt.Open);
        }


        private void VerifyCtor(string portName, int baudRate)
        {
            VerifyCtor(portName, baudRate, null, ThrowAt.Set);
        }


        private void VerifyCtor(string portName, int baudRate, Type expectedException, ThrowAt throwAt)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            Debug.WriteLine("Verifying properties where PortName={0},BaudRate={1}", portName, baudRate);
            try
            {
                using (SerialPort com = new SerialPort(portName, baudRate))
                {
                    if (null != expectedException && throwAt == ThrowAt.Set)
                    {
                        Fail("Err_7212ahsdj Expected Ctor to throw {0}", expectedException);
                    }

                    serPortProp.SetAllPropertiesToDefaults();

                    serPortProp.SetProperty("PortName", portName);
                    serPortProp.SetProperty("BaudRate", baudRate);

                    serPortProp.VerifyPropertiesAndPrint(com);
                }
            }
            catch (FailException)
            {
                // This is an inner failure
                throw;
            }
            catch (Exception e)
            {
                if (null == expectedException)
                {
                    Fail("Err_07081hadnh Did not expect exception to be thrown and the following was thrown: \n{0}", e);
                }
                else if (throwAt == ThrowAt.Open)
                {
                    Fail("Err_88916adfa Expected {0} to be thrown at Open and the following was thrown at Set: \n{1}", expectedException, e);
                }
                else if (e.GetType() != expectedException)
                {
                    Fail("Err_90282ahwhp Expected {0} to be thrown and the following was thrown: \n{1}", expectedException, e);
                }
            }
        }
    }
}
