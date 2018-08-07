// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.IO.Ports
{
    public partial class SerialPort : Component
    {
        public static string[] GetPortNames()
        {
            Span<uint> portNumbers = stackalloc uint[16];
            uint portNumbersFound;
            int error;

            try
            {
                error = Interop.mincore.GetCommPorts(portNumbers, out portNumbersFound);
            }
            catch (Exception e) when (e is EntryPointNotFoundException || e is DllNotFoundException)
            {
                throw new PlatformNotSupportedException(System.SR.PlatformNotSupported_SerialPort_GetPortNames);
            }

            while (error == Interop.Errors.ERROR_MORE_DATA)
            {
                portNumbers = new uint[portNumbersFound];
                error = Interop.mincore.GetCommPorts(portNumbers, out portNumbersFound);
            }

            if (error != Interop.Errors.ERROR_SUCCESS)
            {
                // Try to match general behavior of Fx: return empty array for failures.
                return Array.Empty<string>();
            }

            var portNames = new string[portNumbersFound];
            for (int i = 0; i < portNumbersFound; ++i)
            {
                portNames[i] = "COM" + portNumbers[i].ToString();
            }

            return portNames;
        }
    }
}
