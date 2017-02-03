// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;

namespace Legacy.Support
{
    public class SerialPortConnection
    {
        public static bool VerifyConnection(string portName1, string portName2)
        {
            SerialPort com1 = new SerialPort(portName1);
            SerialPort com2 = new SerialPort(portName2);
            bool retValue = true;

            try
            {
                com1.Open();
                com2.Open();
                retValue = VerifyReadWrite(com1, com2);
            }
            catch (Exception)
            {
                // One of the com ports does not exist on the machine that this is being run on
                // thus their can not be a connection between com1 and com2
                retValue = false;
            }
            finally
            {
                com1.Close();
                com2.Close();
            }

            return retValue;
        }

        public static bool VerifyLoopback(string portName)
        {
            bool retValue = true;

            SerialPort com = new SerialPort(portName);

            try
            {
                com.Open();
                retValue = VerifyReadWrite(com, com);
            }
            catch (Exception)
            {
                // The com ports does not exist on the machine that this is being run on
                // thus their can not be a loopback between the ports
                retValue = false;
            }
            finally
            {
                com.Close();
            }

            return retValue;
        }

        private static bool VerifyReadWrite(SerialPort com1, SerialPort com2)
        {
            try
            {
                com1.ReadTimeout = 1000;
                com2.ReadTimeout = 1000;

                com1.WriteLine("Ping");

                if ("Ping" != com2.ReadLine())
                {
                    return false;
                }

                com2.WriteLine("Response");

                if ("Response" != com1.ReadLine())
                {
                    return false;
                }

                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }
    }
}
