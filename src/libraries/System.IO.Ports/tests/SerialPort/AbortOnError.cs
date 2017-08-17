// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO.PortsTests;
using System.Reflection;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class AbortOnError : PortsTest
    {
        /// <summary>
        /// This is a test for GH issue 17396, Connect issue 584116
        /// 
        /// The situation is as follows:
        /// * Some user of the port causes DCB.fAbortOnError to be set at some point
        /// * The port is later opened by SerialPort - which doesn't clear fAbortOnError
        ///   when it's initialising the port
        /// * Subsequent errors on the port are mishandled, because fAbortOnError causes
        ///   the error behaviour to be different, and SerialPort isn't prepared for this
        /// 
        /// To test this, we need to do the following
        /// 1. Open the port, then use a private SerialStream method to set fAbortOnError to TRUE and then close the port
        /// 2. Reopen the port
        /// 3. Verify that the fAbortOnError flag is clear
        /// 
        /// </summary>
        // This test requires access, via reflection, to internal type SerialStream and respective methods GetDcbFlag and
        // SetDcbFlag, however, that requires either changes to the public type (increasing its size) or to the test itself.
        [ActiveIssue("https://github.com/dotnet/corefx/issues/23234", TargetFrameworkMonikers.Uap)]
        [ConditionalFact(nameof(HasOneSerialPort))]
        public void AbortOnErrorShouldBeClearedOnOpen()
        {
            // Open the port, set the fAbortOnError flag and then close the port
            SetAbortOnError(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

            using (SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName))
            {
                com.Open();

                Assert.False(ReadAbortOnErrorFlag(com), "fAbortOnError should be clear when port is opened");
            }
        }

        private static bool ReadAbortOnErrorFlag(SerialPort com)
        {
            Stream baseStream = com.BaseStream;
            int flagValue = (int)baseStream.GetType()
                .GetMethod("GetDcbFlag", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(baseStream, new object[] {14});
            return flagValue != 0;
        }

        private void SetAbortOnError(string port)
        {
            using (SerialPort com = new SerialPort(port))
            {
                com.Open();
                Stream baseStream = com.BaseStream;

                // Invoke the private method SetDcbFlag to set bit 14 in the DCB flags
                // This just updates the _dcb.Flags member of SerialStream
                baseStream.GetType().GetMethod("SetDcbFlag", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(baseStream, new object[] { 14, 1 });

                // Force the DCB to get written to the driver
                com.DataBits = 7;
                com.DataBits = 8;
            }
        }
    }
}
