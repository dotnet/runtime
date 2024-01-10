// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
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
            Assert.Fail(string.Format(format, args));
        }

#pragma warning disable SYSLIB0001 // Encoding.UTF7 property is obsolete
        protected static Encoding LegacyUTF7Encoding => Encoding.UTF7;
#pragma warning restore SYSLIB0001

        /// <summary>
        /// Returns a value stating whether <paramref name="encoding"/> is UTF-7.
        /// </summary>
        /// <remarks>
        /// This method checks only for the code page 65000.
        /// </remarks>
        internal static bool IsUTF7Encoding(Encoding encoding)
        {
            return (encoding.CodePage == LegacyUTF7Encoding.CodePage);
        }
    }
}
