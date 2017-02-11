// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO.Ports;
using System.IO.PortsTests;
using Legacy.Support;
using Xunit;

public class SerialPortRegressions : PortsTest
{
    private static string s_receivedstr = "";
    private static SerialPort s_com1;
    private static SerialPort s_com2;
    private static bool s_readComplete;

    [ConditionalFact(nameof(HasLoopbackOrNullModem))]
    public void UTF8Encoding()
    {
        VerifyReadExisting(new System.Text.UTF8Encoding());
    }

    void VerifyReadExisting(System.Text.Encoding encoding)
    {
        // TODO - this test text did not come across from legacy properly.
        string text = "????????????4??????????????????,?11????????????????????????????????????????????????,????????????,??????????";
        using (s_com1 = TCSupport.InitFirstSerialPort())
        using (s_com2 = TCSupport.InitSecondSerialPort(s_com1))
        {
            s_com1.ReadTimeout = 500;
            s_com1.Encoding = encoding;
            s_com2.Encoding = encoding;

            s_com1.Open();

            if (!s_com2.IsOpen)
                //This is necessary since com1 and com2 might be the same port if we are using a loopback
                s_com2.Open();

            s_com2.DataReceived += com2_DataReceived;
            s_com1.Write(text);

            //3 seconds is more than enough time to write a few bytes to the other port	
            TCSupport.WaitForPredicate(() => s_readComplete, 3000,
                "ReadExisting did not complete in a timely fashion.  Timeout");

            Assert.Equal(text, s_receivedstr);
        }
    }

    private static void com2_DataReceived(object o, SerialDataReceivedEventArgs e)
    {
        s_receivedstr += s_com2.ReadExisting();
        s_readComplete = true;
    }
}
