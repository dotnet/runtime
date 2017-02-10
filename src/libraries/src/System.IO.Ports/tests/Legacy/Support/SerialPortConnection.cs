// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Notes on test capabilities:
// 
// A single port just means there's at least one port, which can be opened by the test application
// A loopback port is a port where the DATA is looped back, but the HANDSHAKE is not.   Several tests
// rely on enabling RTS/CTS handshake to block transmissions from the port, but if RTS/CTS is looped-back
// the port will never block and the tests will either hang or fail (we could perhaps detect this when probing for loopback ports)
// A null-modem connection is available when you have at least two openable ports, where some pair of ports is connected with
// a null modem connection - i.e. TX/RX and CTS/RTS are crossed between the ports.

using System;
using System.IO.Ports;

namespace Legacy.Support
{
    public class SerialPortConnection
    {
        public static bool VerifyConnection(string portName1, string portName2)
        {
            using (SerialPort com1 = new SerialPort(portName1))
            using (SerialPort com2 = new SerialPort(portName2))
            {
                bool connectionVerified;
                try
                {
                    com1.Open();
                    com2.Open();
                    connectionVerified = VerifyReadWrite(com1, com2);
                }
                catch (Exception)
                {
                    // One of the com ports does not exist on the machine that this is being run on
                    // thus their can not be a connection between com1 and com2
                    connectionVerified = false;
                }
                return connectionVerified;
            }
        }

        public static bool VerifyLoopback(string portName)
        {
            using (SerialPort com = new SerialPort(portName))
            {
                bool loopbackVerified;
                try
                {
                    com.Open();
                    loopbackVerified = VerifyReadWrite(com, com);
                }
                catch (Exception)
                {
                    // The com ports does not exist on the machine that this is being run on
                    // thus their can not be a loopback between the ports
                    loopbackVerified = false;
                }
                return loopbackVerified;
            }
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
