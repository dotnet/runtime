// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.Diagnostics.NETCore.Client
{
    /**
     * ==ProcessInfo==
     * The response payload to issuing the GetProcessInfo command.
     * 
     * 8 bytes  - PID (little-endian)
     * 16 bytes - CLR Runtime Instance Cookie (little-endian)
     * # bytes  - Command line string length and data
     * # bytes  - Operating system string length and data
     * # bytes  - Process architecture string length and data
     * 
     * ==ProcessInfo2==
     * The response payload to issuing the GetProcessInfo2 command.
     * 
     * 8 bytes  - PID (little-endian)
     * 16 bytes - CLR Runtime Instance Cookie (little-endian)
     * # bytes  - Command line string length and data
     * # bytes  - Operating system string length and data
     * # bytes  - Process architecture string length and data
     * # bytes  - Managed entrypoint assembly name
     * # bytes  - CLR product version string (may include prerelease labels)
     * 
     * 
     * The "string length and data" fields are variable length:
     * 4 bytes            - Length of string data in UTF-16 characters
     * (2 * length) bytes - The data of the string encoded using Unicode
     *                      (includes null terminating character)
     */

    internal class ProcessInfo
    {
        private static readonly int GuidSizeInBytes = 16;

        /// <summary>
        /// Parses a ProcessInfo payload.
        /// </summary>
        internal static ProcessInfo ParseV1(byte[] payload)
        {
            int index = 0;
            return ParseCommon(payload, ref index);
        }

        /// <summary>
        /// Parses a ProcessInfo2 payload.
        /// </summary>
        internal static ProcessInfo ParseV2(byte[] payload)
        {
            int index = 0;
            ProcessInfo processInfo = ParseCommon(payload, ref index);

            processInfo.ManagedEntrypointAssemblyName = IpcHelpers.ReadString(payload, ref index);
            processInfo.ClrProductVersionString = IpcHelpers.ReadString(payload, ref index);

            return processInfo;
        }

        private static ProcessInfo ParseCommon(byte[] payload, ref int index)
        {
            ProcessInfo processInfo = new ProcessInfo();

            processInfo.ProcessId = BitConverter.ToUInt64(payload, index);
            index += sizeof(UInt64);

            byte[] cookieBuffer = new byte[GuidSizeInBytes];
            Array.Copy(payload, index, cookieBuffer, 0, GuidSizeInBytes);
            processInfo.RuntimeInstanceCookie = new Guid(cookieBuffer);
            index += GuidSizeInBytes;

            processInfo.CommandLine = IpcHelpers.ReadString(payload, ref index);
            processInfo.OperatingSystem = IpcHelpers.ReadString(payload, ref index);
            processInfo.ProcessArchitecture = IpcHelpers.ReadString(payload, ref index);

            return processInfo;
        }

        public UInt64 ProcessId { get; private set; }
        public Guid RuntimeInstanceCookie { get; private set; }
        public string CommandLine { get; private set; }
        public string OperatingSystem { get; private set; }
        public string ProcessArchitecture { get; private set; }
        public string ManagedEntrypointAssemblyName { get; private set; }
        public string ClrProductVersionString { get; private set; }
    }
}