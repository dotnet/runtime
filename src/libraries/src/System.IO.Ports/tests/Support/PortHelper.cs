// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

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
            if (PlatformDetection.IsUap)
            {
                return new [] { "COM3", "COM4", "COM5", "COM6", "COM7" }; // we are waiting for a Win32 new QueryDosDevice API since the current doesn't work for Uap https://github.com/dotnet/corefx/issues/21156
            }

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
