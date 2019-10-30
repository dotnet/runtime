// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Legacy.Support;
using Xunit;

namespace System.IO.PortsTests
{
    public class PortsTest : FileCleanupTestBase
    {
        public static bool HasOneSerialPort => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.OneSerialPort);

        public static bool HasTwoSerialPorts => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.TwoSerialPorts);

        public static bool HasLoopback => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.Loopback);

        public static bool HasNullModem => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.NullModem);

        public static bool HasLoopbackOrNullModem => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.LoopbackOrNullModem);

        /// <summary>
        /// Shows that we can retain a single byte in the transmit queue if flow control doesn't permit transmission
        /// This is true for traditional PC ports, but will be false if there is additional driver/hardware buffering in the system
        /// </summary>
        public static bool HasSingleByteTransmitBlocking => TCSupport.HardwareTransmitBufferSize == 0;

        /// <summary>
        /// Shows that we can inhibit transmission using hardware flow control
        /// Some kinds of virtual port or RS485 adapter can't do this
        /// </summary>
        public static bool HasHardwareFlowControl => TCSupport.HardwareWriteBlockingAvailable;

        public static void Fail(string format, params object[] args)
        {
            Assert.True(false, string.Format(format, args));
        }
    }
}
