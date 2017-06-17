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
    public class ctor_str_int_parity_int : PortsTest
    {
        private enum ThrowAt { Set, Open };

        [Fact]
        public void COM1_9600_Odd_7()
        {
            string portName = "COM1";
            int baudRate = 9600;
            int parity = (int)Parity.Odd;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits);
        }


        [Fact]
        public void COM2_14400_None_7()
        {
            string portName = "COM2";
            int baudRate = 14400;
            int parity = (int)Parity.None;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits);
        }


        [Fact]
        public void COM3_28800_Mark_7()
        {
            string portName = "COM3";
            int baudRate = 28800;
            int parity = (int)Parity.Mark;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits);
        }


        [Fact]
        public void COM4_57600_Space_7()
        {
            string portName = "COM4";
            int baudRate = 57600;
            int parity = (int)Parity.Space;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits);
        }


        [Fact]
        public void COM256_115200_Even_8()
        {
            string portName = "COM256";
            int baudRate = 115200;
            int parity = (int)Parity.Even;
            int dataBits = 8;

            VerifyCtor(portName, baudRate, parity, dataBits);
        }


        [Fact]
        public void COM1_9600_None_8()
        {
            string portName = "COM1";
            int baudRate = 9600;
            int parity = (int)Parity.None;
            int dataBits = 8;

            VerifyCtor(portName, baudRate, parity, dataBits);
        }


        //[] Error checking for PortName
        [Fact]
        public void Empty_9600_None_7()
        {
            string portName = string.Empty;
            int baudRate = 9600;
            int parity = (int)Parity.None;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentException), ThrowAt.Set);
        }

        [Fact]
        public void Null_14400_Even_8()
        {
            string portName = null;
            int baudRate = 14400;
            int parity = (int)Parity.Even;
            int dataBits = 8;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentNullException), ThrowAt.Set);
        }

        [Fact]
        public void COM257_57600_Mark_8()
        {
            string portName = "COM257";
            int baudRate = 57600;
            int parity = (int)Parity.Mark;
            int dataBits = 8;

            VerifyCtor(portName, baudRate, parity, dataBits);
        }

        [Fact]
        public void Filename_9600_Space_8()
        {
            string portName;
            int baudRate = 9600;
            int parity = (int)Parity.Space;
            int dataBits = 8;
            string fileName = portName = GetTestFilePath();
            FileStream testFile = File.Open(fileName, FileMode.Create);
            ASCIIEncoding asciiEncd = new ASCIIEncoding();
            string testStr = "Hello World";

            testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));
            testFile.Close();
            try
            {
                VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentException), ThrowAt.Open);
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
        public void PHYSICALDRIVE0_14400_Even_7()
        {
            string portName = "PHYSICALDRIVE0";
            int baudRate = 14400;
            int parity = (int)Parity.Even;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentException), ThrowAt.Open);
        }


        //[] Error checking for BaudRate
        [Fact]
        public void COM1_Int32MinValue_None_8()
        {
            string portName = "Com1";
            int baudRate = int.MinValue;
            int parity = (int)Parity.None;
            int dataBits = 8;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM2_Neg1_Even_7()
        {
            string portName = "Com2";
            int baudRate = -1;
            int parity = (int)Parity.Even;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM3_0_Odd_7()
        {
            string portName = "Com3";
            int baudRate = 0;
            int parity = (int)Parity.Odd;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM4_Int32MaxValue_Mark_8()
        {
            string portName = "Com4";
            int baudRate = int.MaxValue;
            int parity = (int)Parity.Mark;
            int dataBits = 8;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Open);
        }


        //[] Error checking for Parity
        [Fact]
        public void COM1_9600_Int32MinValue_7()
        {
            string portName = "Com1";
            int baudRate = 9600;
            int parity = int.MinValue;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM2_14400_Neg1_8()
        {
            string portName = "Com2";
            int baudRate = 14400;
            int parity = -1;
            int dataBits = 8;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM3_28800_5_7()
        {
            string portName = "Com3";
            int baudRate = 28800;
            int parity = 5;
            int dataBits = 7;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM4_57600_Int32MaxValue_8()
        {
            string portName = "Com4";
            int baudRate = 57600;
            int parity = int.MaxValue;
            int dataBits = 8;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        //[] Error checking for DataBits
        [Fact]
        public void COM1_9600_None_Int32MinValue()
        {
            string portName = "Com1";
            int baudRate = 9600;
            int parity = (int)Parity.None;
            int dataBits = int.MinValue;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM2_14400_Even_Neg1()
        {
            string portName = "Com2";
            int baudRate = 14400;
            int parity = (int)Parity.Even;
            int dataBits = -1;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM3_28800_Odd_4()
        {
            string portName = "Com3";
            int baudRate = 28800;
            int parity = (int)Parity.Odd;
            int dataBits = 4;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM4_57600_Mark_9()
        {
            string portName = "Com4";
            int baudRate = 57600;
            int parity = (int)Parity.Mark;
            int dataBits = 9;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        [Fact]
        public void COM255_115200_Space_Int32MaxValue()
        {
            string portName = "Com255";
            int baudRate = 115200;
            int parity = (int)Parity.Space;
            int dataBits = int.MaxValue;

            VerifyCtor(portName, baudRate, parity, dataBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }


        private void VerifyCtor(string portName, int baudRate, int parity, int dataBits)
        {
            VerifyCtor(portName, baudRate, parity, dataBits, null, ThrowAt.Set);
        }


        private void VerifyCtor(string portName, int baudRate, int parity, int dataBits, Type expectedException, ThrowAt throwAt)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            Debug.WriteLine("Verifying properties where PortName={0},BaudRate={1},Parity={2},DatBits={3}", portName, baudRate, parity, dataBits);
            try
            {
                using (SerialPort com = new SerialPort(portName, baudRate, (Parity)parity, dataBits))
                {
                    if (null != expectedException && throwAt == ThrowAt.Set)
                    {
                        Fail("Err_7212ahsdj Expected Ctor to throw {0}", expectedException);
                    }

                    serPortProp.SetAllPropertiesToDefaults();

                    serPortProp.SetProperty("PortName", portName);
                    serPortProp.SetProperty("BaudRate", baudRate);
                    serPortProp.SetProperty("Parity", (Parity)parity);
                    serPortProp.SetProperty("DataBits", dataBits);

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

