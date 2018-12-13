// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Legacy.Support
{
    public class PortHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetLastError();

        [DllImport("kernel32.dll", EntryPoint = "QueryDosDeviceW", CharSet = CharSet.Unicode)]
        private static extern int QueryDosDevice(string lpDeviceName, IntPtr lpTargetPath, int ucchMax);

        public static string[] GetPorts()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return SerialPort.GetPortNames();
            }

            if (PlatformDetection.IsUap)
            {
                // On UAP it is not possible to call QueryDosDevice, so use HARDWARE\DEVICEMAP\SERIALCOMM on the registry
                // to get this information. The UAP code uses the GetCommPorts API to retrieve the same information.
                return GetCommPortsFromRegistry();
            }

            return GetCommPortsViaQueryDosDevice();
        }

        private static string[] GetCommPortsFromRegistry()
        {
            // See https://msdn.microsoft.com/en-us/library/windows/hardware/ff546502.aspx for more information.
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

        private static string[] GetCommPortsViaQueryDosDevice()
        {
            List<string> ports = new List<string>();
            int returnSize = 0;
            int maxSize = 1000000;
            string[] retval = null;
            const int ERROR_INSUFFICIENT_BUFFER = 122;
            while (returnSize == 0)
            {
                IntPtr mem = Marshal.AllocHGlobal(maxSize);
                if (mem != IntPtr.Zero)
                {
                    // mem points to memory that needs freeing
                    try
                    {
                        returnSize = QueryDosDevice(null, mem, maxSize);
                        if (returnSize != 0)
                        {
                            string allDevices = Marshal.PtrToStringUni(mem, returnSize);
                            retval = allDevices.Split('\0');
                            break;    // not really needed, but makes it more clear...
                        }
                        else if (GetLastError() == ERROR_INSUFFICIENT_BUFFER)
                        {
                            maxSize *= 10;
                        }
                        else
                        {
                            Marshal.ThrowExceptionForHR(GetLastError());
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(mem);
                    }
                }
                else
                {
                    throw new OutOfMemoryException();
                }
            }

            if (retval != null)
            {
                var serialRegex = new Regex(@"^COM\d{1,3}$");
                foreach (string str in retval)
                {
                    if (serialRegex.IsMatch(str))
                    {
                        ports.Add(str);
                        Debug.WriteLine("Installed serial ports :" + str);
                    }
                }
            }

            return ports.ToArray();
        }
    }

    public static class XOnOff
    {
        public const byte XOFF = 19;
        public const byte XON = 17;
    }
}
