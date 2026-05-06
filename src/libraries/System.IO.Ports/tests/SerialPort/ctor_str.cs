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
    public class ctor_str : PortsTest
    {
        private enum ThrowAt { Set, Open };

        [Fact]
        public void COM1()
        {
            string portName = "COM1";
            VerifyCtor(portName);
        }

        [Fact]
        public void COM2()
        {
            string portName = "COM2";
            VerifyCtor(portName);
        }

        [Fact]
        public void COM3()
        {
            string portName = "COM3";
            VerifyCtor(portName);
        }

        [Fact]
        public void COM4()
        {
            string portName = "COM4";
            VerifyCtor(portName);
        }

        [Fact]
        public void COM256()
        {
            string portName = "COM256";
            VerifyCtor(portName);
        }

        [Fact]
        public void Empty()
        {
            //[] Error checking for PortName
            string portName = string.Empty;
            VerifyCtor(portName, typeof(ArgumentException), ThrowAt.Set);
        }

        [Fact]
        public void Null()
        {
            string portName = null;
            VerifyCtor(portName, typeof(ArgumentNullException), ThrowAt.Set);
        }

        [Fact]
        public void COM257()
        {
            string portName = "COM257";
            VerifyCtor(portName);
        }

        [Fact]
        public void Filename()
        {
            string portName;
            string fileName = portName = GetTestFilePath();
            FileStream testFile = File.Open(fileName, FileMode.Create);
            ASCIIEncoding asciiEncd = new ASCIIEncoding();
            string testStr = "Hello World";

            testFile.Write(asciiEncd.GetBytes(testStr), 0, asciiEncd.GetByteCount(testStr));
            testFile.Close();
            try
            {
                VerifyCtor(portName, typeof(ArgumentException), ThrowAt.Open);
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
        public void PHYSICALDRIVE0()
        {
            string portName = "PHYSICALDRIVE0";
            VerifyCtor(portName, typeof(ArgumentException), ThrowAt.Open);
        }

        private void VerifyCtor(string portName)
        {
            VerifyCtor(portName, null, ThrowAt.Set);
        }

        private void VerifyCtor(string portName, Type expectedException, ThrowAt throwAt)
        {
            SerialPortProperties serPortProp = new SerialPortProperties();

            Debug.WriteLine($"Verifying properties where PortName={portName}");
            try
            {
                using (SerialPort com = new SerialPort(portName))
                {
                    if (null != expectedException && throwAt == ThrowAt.Set)
                    {
                        Assert.Fail($"Err_7212ahsdj Expected Ctor to throw {expectedException}");
                    }

                    serPortProp.SetAllPropertiesToDefaults();
                    serPortProp.SetProperty("PortName", portName);

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
                    Assert.Fail($"Err_07081hadnh Did not expect exception to be thrown and the following was thrown: \n{e}");
                }
                else if (throwAt == ThrowAt.Open)
                {
                    Assert.Fail($"Err_88916adfa Expected {expectedException} to be thrown at Open and the following was thrown at Set: \n{e}");
                }
                else if (e.GetType() != expectedException)
                {
                    Assert.Fail($"Err_90282ahwhp Expected {expectedException} to be thrown and the following was thrown: \n{e}");
                }
            }
        }
    }
}
