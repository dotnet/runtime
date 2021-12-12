// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Legacy.Support
{
    public partial class PortHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int GetLastError();

        [DllImport("kernel32.dll", EntryPoint = "QueryDosDeviceW", CharSet = CharSet.Unicode)]
        private static extern int QueryDosDevice(string lpDeviceName, IntPtr lpTargetPath, int ucchMax);

        [RegexGenerator(@"com\d{1,3}", RegexOptions.IgnoreCase)]
        public static partial Regex ComPortRegex();

        public static string[] GetPorts()
        {
            if (!PlatformDetection.IsWindows)
            {
                return SerialPort.GetPortNames();
            }

            if (PlatformDetection.IsInAppContainer)
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
                    string[] valueNames = serialKey.GetValueNames();
                    return valueNames.Select((Func<string, object>)serialKey.GetValue).Cast<string>().ToArray();
                }
            }

            return Array.Empty<string>();
        }

        private static string[] GetCommPortsViaQueryDosDevice()
        {
            int returnSize = 0;
            int maxSize = 1000000;
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
                            string[] allDevices = Marshal.PtrToStringUni(mem, returnSize).Split('\0');
                            string[] ports = allDevices.Where((Func<String, bool>)ComPortRegex().IsMatch).ToArray();
                            Array.ForEach(ports, p => Debug.WriteLine($"Installed serial ports :{p}"));
                            return ports;
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

            return Array.Empty<string>();
        }
    }

    public static class XOnOff
    {
        public const byte XOFF = 19;
        public const byte XON = 17;
    }
}
