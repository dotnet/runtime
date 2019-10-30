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
    public class ctor_str_int_parity_int_stopbits : PortsTest
    {
        private enum ThrowAt { Set, Open };

        [Fact]
        public void COM1_9600_Odd_5_1()
        {
            string portName = "COM1";
            int baudRate = 9600;
            int parity = (int)Parity.Odd;
            int dataBits = 5;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM2_14400_None_5_1()
        {
            string portName = "COM2";
            int baudRate = 14400;
            int parity = (int)Parity.None;
            int dataBits = 5;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM3_28800_None_5_15()
        {
            string portName = "COM3";
            int baudRate = 28800;
            int parity = (int)Parity.None;
            int dataBits = 5;
            int stopBits = (int)StopBits.OnePointFive;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM4_57600_Even_5_15()
        {
            string portName = "COM2";
            int baudRate = 57600;
            int parity = (int)Parity.Even;
            int dataBits = 5;
            int stopBits = (int)StopBits.OnePointFive;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM256_115200_Mark_5_2()
        {
            string portName = "COM256";
            int baudRate = 115200;
            int parity = (int)Parity.Mark;
            int dataBits = 5;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM1_9600_None_5_2()
        {
            string portName = "COM1";
            int baudRate = 9600;
            int parity = (int)Parity.None;
            int dataBits = 5;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM2_14400_Even_6_1()
        {
            string portName = "COM2";
            int baudRate = 14400;
            int parity = (int)Parity.Even;
            int dataBits = 6;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM3_28800_Odd_7_2()
        {
            string portName = "COM3";
            int baudRate = 28800;
            int parity = (int)Parity.Odd;
            int dataBits = 7;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM1_9600_Odd_8_1()
        {
            string portName = "COM1";
            int baudRate = 9600;
            int parity = (int)Parity.Odd;
            int dataBits = 8;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM2_14400_None_8_1()
        {
            string portName = "COM2";
            int baudRate = 14400;
            int parity = (int)Parity.None;
            int dataBits = 8;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM256_115200_Mark_8_2()
        {
            string portName = "COM256";
            int baudRate = 115200;
            int parity = (int)Parity.Mark;
            int dataBits = 8;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        [Fact]
        public void COM1_9600_None_8_2()
        {
            string portName = "COM1";
            int baudRate = 9600;
            int parity = (int)Parity.None;
            int dataBits = 8;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }


        //[] Error checking for PortName
        [Fact]
        public void Empty_9600_None_5_15()
        {
            string portName = string.Empty;
            int baudRate = 9600;
            int parity = (int)Parity.None;
            int dataBits = 5;
            int stopBits = (int)StopBits.OnePointFive;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentException), ThrowAt.Set);
        }


        [Fact]
        public void Null_14400_Even_6_1()
        {
            string portName = null;
            int baudRate = 14400;
            int parity = (int)Parity.Even;
            int dataBits = 6;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentNullException), ThrowAt.Set);
        }

        [Fact]
        public void COM257_57600_Mark_8_2()
        {
            string portName = "COM257";
            int baudRate = 57600;
            int parity = (int)Parity.Mark;
            int dataBits = 8;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits);
        }

        [Fact]
        public void Filename_9600_Space_8_1()
        {
            string portName;
            int baudRate = 9600;
            int parity = (int)Parity.Space;
            int dataBits = 8;
            int stopBits = (int)StopBits.One;
            string fileName = portName = GetTestFilePath();
            FileStream testFile = File.Open(fileName, FileMode.Create);
            ASCIIEncoding asciiEncd = new ASCIIEncoding();
            string testStr = "Hello World";

            testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));
            testFile.Close();
            try
            {
                VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentException), ThrowAt.Open);
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
        public void PHYSICALDRIVE0_14400_Even_5_15()
        {
            string portName = "PHYSICALDRIVE0";
            int baudRate = 14400;
            int parity = (int)Parity.Even;
            int dataBits = 5;
            int stopBits = (int)StopBits.OnePointFive;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentException), ThrowAt.Open);
        }

        //[] Error checking for BaudRate
        [Fact]
        public void COM1_Int32MinValue_None_5_15()
        {
            string portName = "Com1";
            int baudRate = int.MinValue;
            int parity = (int)Parity.None;
            int dataBits = 5;
            int stopBits = (int)StopBits.OnePointFive;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM2_Neg1_Even_6_1()
        {
            string portName = "Com2";
            int baudRate = -1;
            int parity = (int)Parity.Even;
            int dataBits = 6;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM3_0_Odd_7_2()
        {
            string portName = "Com3";
            int baudRate = 0;
            int parity = (int)Parity.Odd;
            int dataBits = 7;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM4_Int32MaxValue_Mark_8_2()
        {
            string portName = "Com4";
            int baudRate = int.MaxValue;
            int parity = (int)Parity.Mark;
            int dataBits = 8;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Open);
        }

        //[] Error checking for Parity
        [Fact]
        public void COM1_9600_Int32MinValue_5_15()
        {
            string portName = "Com1";
            int baudRate = 9600;
            int parity = int.MinValue;
            int dataBits = 5;
            int stopBits = (int)StopBits.OnePointFive;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM2_14400_Neg1_6_1()
        {
            string portName = "Com2";
            int baudRate = 14400;
            int parity = -1;
            int dataBits = 6;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM3_28800_5_7_2()
        {
            string portName = "Com3";
            int baudRate = 28800;
            int parity = 5;
            int dataBits = 7;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM4_57600_Int32MaxValue_8_2()
        {
            string portName = "Com4";
            int baudRate = 57600;
            int parity = int.MaxValue;
            int dataBits = 8;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        //[] Error checking for DataBits
        [Fact]
        public void COM1_9600_None_Int32MinValue_1()
        {
            string portName = "Com1";
            int baudRate = 9600;
            int parity = (int)Parity.None;
            int dataBits = int.MinValue;
            int stopBits = (int)StopBits.OnePointFive;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM2_14400_Even_Neg1_15()
        {
            string portName = "Com2";
            int baudRate = 14400;
            int parity = (int)Parity.Even;
            int dataBits = -1;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM3_28800_Odd_4_2()
        {
            string portName = "Com3";
            int baudRate = 28800;
            int parity = (int)Parity.Odd;
            int dataBits = 4;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM4_57600_Mark_9_1()
        {
            string portName = "Com4";
            int baudRate = 57600;
            int parity = (int)Parity.Mark;
            int dataBits = 9;
            int stopBits = (int)StopBits.Two;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM255_115200_Space_Int32MaxValue_15()
        {
            string portName = "Com255";
            int baudRate = 115200;
            int parity = (int)Parity.Space;
            int dataBits = int.MaxValue;
            int stopBits = (int)StopBits.One;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        //[] Error checking for StopBits
        [Fact]
        public void COM1_9600_None_5_Int32MinValue()
        {
            string portName = "Com1";
            int baudRate = 9600;
            int parity = (int)Parity.None;
            int dataBits = 5;
            int stopBits = int.MinValue;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM2_14400_Even_6_Neg1()
        {
            string portName = "Com2";
            int baudRate = 14400;
            int parity = (int)Parity.Even;
            int dataBits = 6;
            int stopBits = -1;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM3_28800_Odd_7_0()
        {
            string portName = "Com3";
            int baudRate = 28800;
            int parity = (int)Parity.Odd;
            int dataBits = 7;
            int stopBits = 0;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM4_57600_Mark_8_4()
        {
            string portName = "Com4";
            int baudRate = 57600;
            int parity = (int)Parity.Mark;
            int dataBits = 8;
            int stopBits = 4;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        [Fact]
        public void COM255_115200_Space_8_Int32MaxValue()
        {
            string portName = "Com255";
            int baudRate = 115200;
            int parity = (int)Parity.Space;
            int dataBits = 8;
            int stopBits = int.MaxValue;

            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, typeof(ArgumentOutOfRangeException), ThrowAt.Set);
        }

        private void VerifyCtor(string portName, int baudRate, int parity, int dataBits, int stopBits)
        {
            VerifyCtor(portName, baudRate, parity, dataBits, stopBits, null, ThrowAt.Set);
        }

        private void VerifyCtor(string portName, int baudRate, int parity, int dataBits, int stopBits, Type expectedException, ThrowAt throwAt)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            Debug.WriteLine("Verifying properties where PortName={0},BaudRate={1},Parity={2},DatBits={3},StopBits={4}", portName, baudRate, parity, dataBits, stopBits);
            try
            {
                using (SerialPort com = new SerialPort(portName, baudRate, (Parity)parity, dataBits, (StopBits)stopBits))
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
                    serPortProp.SetProperty("StopBits", (StopBits)stopBits);

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
