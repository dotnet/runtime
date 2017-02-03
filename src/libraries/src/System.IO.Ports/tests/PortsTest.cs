// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Legacy.Support;

namespace System.IO.PortsTests
{
    public class PortsTest
    {
        public static bool HasOneSerialPort => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.OneSerialPort);

        public static bool HasTwoSerialPorts => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.TwoSerialPorts);

        public static bool HasLoopback => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.Loopback);

        public static bool HasNullModem => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.NullModem);

        public static bool HasLoopbackOrNullModem => TCSupport.SufficientHardwareRequirements(TCSupport.SerialPortRequirements.LoopbackOrNullModem);
    }
}
