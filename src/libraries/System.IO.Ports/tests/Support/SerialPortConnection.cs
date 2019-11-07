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
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Legacy.Support
{
    public class SerialPortConnection
    {
        public static bool VerifyConnection(string portName1, string portName2)
        {
            using (SerialPort com1 = new SerialPort(portName1, 115200))
            using (SerialPort com2 = new SerialPort(portName2, 115200))
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
            using (SerialPort com = new SerialPort(portName, 115200))
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
                com1.WriteTimeout = 1000;
                com2.WriteTimeout = 1000;

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

        /// <summary>
        /// Not all types of serial ports block transmission completely when they're trying to transmit
        /// with flow-control set against them.  Some ports transmit a certain amount of data onward to
        /// the hardware before stopping, so if you want them to block, you need to send more than
        /// this minimum of data
        ///
        /// This routine performs a series of probes of the behaviour of a port, to establish how much
        /// data is needed - the length of the probe packet is doubled on each probe up to a maximum of 8192 bytes
        ///
        /// A traditional 16550 UART port will block on the first byte.  Some FTDI USB-Serial devices need more than 4096 bytes before
        /// they block
        /// </summary>
        /// <returns>The smallest probe </returns>
        public static FlowControlCapabilities MeasureFlowControlCapabilities(string portName)
        {
            for (int probeBase = 1; probeBase <= 65536; probeBase *= 2)
            {
                // We always probe one over the powers of two to make sure we just exceed common buffer sizes
                int probeLength;
                probeLength = probeBase + 1;
                int bufferSize = MeasureTransmitBufferSize(portName, probeLength);
                if (bufferSize < probeLength)
                {
                    Debug.WriteLine("{0}: Found blocking packet of length {1}, hardware buffer {2}", portName, probeLength, bufferSize);
                    return new FlowControlCapabilities(probeLength, bufferSize, true);
                }
            }

            Debug.WriteLine("Failed to achieve write blocking on serial port - no hardware flow-control available");
            return new FlowControlCapabilities(0, -1, false);
        }

        /// <summary>
        /// Measure the amount of data which can be written to a blocked serial port before it
        /// starts to queue-up
        /// </summary>
        private static int MeasureTransmitBufferSize(string portName, int probeLength)
        {
            int measuredHardwareCapacity = 0;
            using (var com = new SerialPort(portName, 115200))
            {
                com.Handshake = Handshake.RequestToSend;
                com.WriteTimeout = 2000;
                com.Open();
                com.DiscardOutBuffer();

                var testBlock = new byte[probeLength];

                Task t = Task.Run(() =>
                {
                    int lastHardwareCapacity = -1;
                    for (int i = 0; i < 10; i++)
                    {
                        int queuedLength = com.BytesToWrite;
                        int hardwareCapacity = testBlock.Length - queuedLength;

                        if (hardwareCapacity == lastHardwareCapacity)
                        {
                            // We've had two readings the same
                            measuredHardwareCapacity = hardwareCapacity;
                            break;
                        }
                        // We're still pushing stuff out - wait for two readings the same
                        lastHardwareCapacity = hardwareCapacity;
                        Thread.Sleep(10);
                    }
                    com.Handshake = Handshake.None;
                    com.DiscardOutBuffer();
                });

                try
                {
                    com.Write(testBlock, 0, testBlock.Length);
                }
                catch (TimeoutException)
                {
                }
                catch (IOException)
                {
                    // We may see hardware exceptions when the task calls Discard
                }

                t.Wait(2000);
            }
            return measuredHardwareCapacity;
        }
    }
}
