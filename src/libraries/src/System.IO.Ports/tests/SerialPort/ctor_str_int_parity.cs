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
    public class ctor_str_int_parity : PortsTest
    {
        private enum ThrowAt { Set, Open };

        [Fact]
        public void COM1_9600_Odd()
        {
            string portName = "COM1";
            int baudRate = 9600;
            int parity = (int)Parity.Odd;

            VerifyCtor(portName, baudRate, parity);
        }


        [Fact]
        public void COM2_14400_None()
        {
            string portName = "COM2";
            int baudRate = 14400;
            int parity = (int)Parity.None;

            VerifyCtor(portName, baudRate, parity);
        }


        [Fact]
        public void COM3_28800_Mark()
        {
            string portName = "COM3";
            int baudRate = 28800;
            int parity = (int)Parity.Mark;

            VerifyCtor(portName, baudRate, parity);
        }

        [Fact]
        public void COM4_57600_Space()
        {
            string portName = "COM4";
            int baudRate = 57600;
            int parity = (int)Parity.Space;

            VerifyCtor(portName, baudRate, parity);
        }

        [Fact]
        public void COM256_115200_Even()
        {
            string portName = "COM256";
            int baudRate = 115200;
            int parity = (int)Parity.Even;

            VerifyCtor(portName, baudRate, parity);
        }


        //[] Error checking for PortName
        [Fact]
        public void Empty_9600_None()
        {
            string portName = string.Empty;
            int baudRate = 9600;
            int parity = (int)Parity.None;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentException), ThrowAt.Set);
        }


        [Fact]
        public void Null_14400_Even()
        {
            string portName = null;
            int baudRate = 14400;
            int parity = (int)Parity.Even;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentNullException), ThrowAt.Set);
        }

        [Fact]
        public void COM257_57600_Mark()
        {
            string portName = "COM257";
            int baudRate = 57600;
            int parity = (int)Parity.Mark;

            VerifyCtor(portName, baudRate, parity);
        }

        [Fact]
        public void Filename_9600_Space()
        {
            string portName;
            int baudRate = 9600;
            int parity = (int)Parity.Space;
            string fileName = portName = GetTestFilePath();
            FileStream testFile = File.Open(fileName, FileMode.Create);
            ASCIIEncoding asciiEncd = new ASCIIEncoding();
            string testStr = "Hello World";

            testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));
            testFile.Close();
            try
            {
                VerifyCtor(portName, baudRate, parity, typeof(ArgumentException), ThrowAt.Open);
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
        public void PHYSICALDRIVE0_14400_Even()
        {
            string portName = "PHYSICALDRIVE0";
            int baudRate = 14400;
            int parity = (int)Parity.Even;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentException), ThrowAt.Open);
        }

        //[] Error checking for BaudRate
        [Fact]
        public void COM1_Int32MinValue_None()
        {
            string portName = "Com1";
            int baudRate = int.MinValue;
            int parity = (int)Parity.None;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM2_Neg1_Even()
        {
            string portName = "Com2";
            int baudRate = -1;
            int parity = (int)Parity.Even;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM3_0_Odd()
        {
            string portName = "Com3";
            int baudRate = 0;
            int parity = (int)Parity.Odd;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM4_Int32MaxValue_Mark()
        {
            string portName = "Com4";
            int baudRate = int.MaxValue;
            int parity = (int)Parity.Mark;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentOutOfRangeException), ThrowAt.Open);
        }

        [Fact]
        //[] Error checking for Parity
        public void COM1_9600_Int32MinValue()
        {
            string portName = "Com1";
            int baudRate = 9600;
            int parity = int.MinValue;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM2_14400_Neg1()
        {
            string portName = "Com2";
            int baudRate = 14400;
            int parity = -1;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM3_28800_5()
        {
            string portName = "Com3";
            int baudRate = 28800;
            int parity = 5;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM4_57600_Int32MaxValue()
        {
            string portName = "Com4";
            int baudRate = 57600;
            int parity = int.MaxValue;

            VerifyCtor(portName, baudRate, parity, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        private void VerifyCtor(string portName, int baudRate, int parity)
        {
            VerifyCtor(portName, baudRate, parity, null, ThrowAt.Set);
        }

        private void VerifyCtor(string portName, int baudRate, int parity, Type expectedException, ThrowAt throwAt)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            Debug.WriteLine("Verifying properties where PortName={0},BaudRate={1},Parity={2}", portName, baudRate, parity);
            try
            {
                using (SerialPort com = new SerialPort(portName, baudRate, (Parity)parity))
                {
                    if (null != expectedException && throwAt == ThrowAt.Set)
                    {
                        Fail("Err_7212ahsdj Expected Ctor to throw {0}", expectedException);
                    }

                    serPortProp.SetAllPropertiesToDefaults();

                    serPortProp.SetProperty("PortName", portName);
                    serPortProp.SetProperty("BaudRate", baudRate);
                    serPortProp.SetProperty("Parity", (Parity)parity);

                    serPortProp.VerifyPropertiesAndPrint(com);
                }
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

