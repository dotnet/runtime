// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.PortsTests;
using System.Text.RegularExpressions;
using Xunit;

public class OpenDevices : PortsTest
{
    //Determines how long the randomly generated PortName is
    public static readonly int rndPortNameSize = 256;

    [Fact]
    public bool OpenDevices01()
    {
        DosDevices dosDevices = new DosDevices();
        Regex comPortNameRegex = new Regex(@"com\d{1,3}", RegexOptions.IgnoreCase);
        bool retValue = true;

        foreach (KeyValuePair<string, string> keyValuePair in dosDevices)
        {
            SerialPort com1;

            if (!string.IsNullOrEmpty(keyValuePair.Key) && !comPortNameRegex.IsMatch(keyValuePair.Key))
            {
                com1 = new SerialPort(keyValuePair.Key);
                try
                {
                    com1.Open();
                    Debug.WriteLine("Error <KEY> no exception thrown with {0}", keyValuePair.Key);
                    retValue = false;
                    com1.Close();
                }
                catch (Exception) { }
            }

            if (!string.IsNullOrEmpty(keyValuePair.Value) && !comPortNameRegex.IsMatch(keyValuePair.Key))
            {
                com1 = new SerialPort(keyValuePair.Value);
                try
                {
                    com1.Open();
                    Debug.WriteLine("Error <VALUE> no exception thrown with {0}", keyValuePair.Value);
                    retValue = false;
                    com1.Close();
                }
                catch (Exception) { }
            }
        }

        return retValue;
    }
}
