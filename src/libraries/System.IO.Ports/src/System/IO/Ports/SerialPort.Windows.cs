// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace System.IO.Ports
{
    public partial class SerialPort : Component
    {
        public static string[] GetPortNames()
        {
            // Hitting the registry for this isn't the only way to get the ports.
            //
            // WMI: https://msdn.microsoft.com/en-us/library/aa394413.aspx
            // QueryDosDevice: https://msdn.microsoft.com/en-us/library/windows/desktop/aa365461.aspx
            //
            // QueryDosDevice involves finding any ports that map to \Device\Serialx (call with null to get all, then iterate to get the actual device name)

            using (RegistryKey serialKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\SERIALCOMM"))
            {
                if (serialKey != null)
                {
                    string[] result = serialKey.GetValueNames();
                    for (int i = 0; i < result.Length; i++)
                    {
                        // Replace the name in the array with its value.
                        result[i] = (string)serialKey.GetValue(result[i]);
                    }
                    return result;
                }
            }

            return Array.Empty<string>();
        }
    }
}
