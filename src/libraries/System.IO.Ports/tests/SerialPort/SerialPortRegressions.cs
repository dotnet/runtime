// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.PortsTests;
using System.Text;
using Legacy.Support;
using Xunit;

namespace System.IO.Ports.Tests
{
    public class SerialPortRegressions : PortsTest
    {
        [ConditionalFact(nameof(HasLoopbackOrNullModem))]
        public void UTF8Encoding()
        {
            VerifyReadExisting(new UTF8Encoding());
        }

        private void VerifyReadExisting(Encoding encoding)
        {
            string text = "Za\u017C\u00F3\u0142\u0107 g\u0119\u015Bl\u0105 ja\u017A\u0144";

            using (SerialPort com1 = TCSupport.InitFirstSerialPort())
            using (SerialPort com2 = TCSupport.InitSecondSerialPort(com1))
            {
                com1.ReadTimeout = 500;
                com1.Encoding = encoding;
                com2.Encoding = encoding;

                TCSupport.SetHighSpeed(com1, com2);

                com1.Open();

                if (!com2.IsOpen)
                    //This is necessary since com1 and com2 might be the same port if we are using a loopback
                    com2.Open();

                string receivedstr = "";
                com2.DataReceived += (sender, args) =>
                {
                    receivedstr += com2.ReadExisting();
                };

                com1.Write(text);

                //3 seconds is more than enough time to write a few bytes to the other port
                TCSupport.WaitForPredicate(() => receivedstr.Length >= text.Length, 3000,
                    "Data was not returned in a timely fashion.  Timeout");

                Assert.Equal(text, receivedstr);
            }
        }
    }
}
