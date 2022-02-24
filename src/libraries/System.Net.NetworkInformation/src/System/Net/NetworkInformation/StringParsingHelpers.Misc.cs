// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Net.NetworkInformation
{
    internal static partial class StringParsingHelpers
    {
        // in some environments (restricted docker container, shared hosting etc.),
        // procfs is not accessible and we get UnauthorizedAccessException while the
        // inner exception is set to IOException.

        internal static string[] ReadAllLines(string filePath)
        {
            try
            {
                return File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                throw CreateNetworkInformationException(e);
            }
        }

        internal static string ReadAllText(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception e)
            {
                throw CreateNetworkInformationException(e);
            }
        }


        internal static StreamReader OpenStreamReader(string filePath)
        {
            try
            {
                return new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0x1000, useAsync: false));
            }
            catch (Exception e)
            {
                throw CreateNetworkInformationException(e);
            }
        }

        internal static NetworkInformationException CreateNetworkInformationException(Exception inner)
        {
            // Overload accepting message and inner exception is internal and thus inaccessible in
            // the unit test project
#if NETWORKINFORMATION_TEST
            return new NetworkInformationException();
#else
            return new NetworkInformationException(SR.net_PInvokeError, inner);
#endif
        }

        internal static int ParseNumRoutesFromRouteFile(string filePath)
        {
            string routeFile = ReadAllText(filePath);
            return CountOccurrences(Environment.NewLine, routeFile) - 1; // File includes one-line header
        }

        internal static int ParseNumIPInterfaces(string folderPath)
        {
            // Just count the number of files under /proc/sys/net/<ipv4/ipv6>/conf,
            // because GetAllNetworkInterfaces() is relatively expensive just for the count.
            int interfacesCount = 0;
            var files = new DirectoryInfo(folderPath).GetFiles();
            foreach (var file in files)
            {
                if (file.Name != NetworkFiles.AllNetworkInterfaceFileName && file.Name != NetworkFiles.DefaultNetworkInterfaceFileName)
                {
                    interfacesCount++;
                }
            }

            return interfacesCount;
        }

        internal static int ParseDefaultTtlFromFile(string filePath)
        {
            // snmp6 does not include Default TTL info. Read it from snmp.
            string snmp4FileContents = ReadAllText(filePath);
            int firstIpHeader = snmp4FileContents.IndexOf("Ip:", StringComparison.Ordinal);
            int secondIpHeader = snmp4FileContents.IndexOf("Ip:", firstIpHeader + 1, StringComparison.Ordinal);
            int endOfSecondLine = snmp4FileContents.IndexOf(Environment.NewLine, secondIpHeader, StringComparison.Ordinal);
            string ipData = snmp4FileContents.Substring(secondIpHeader, endOfSecondLine - secondIpHeader);
            StringParser parser = new StringParser(ipData, ' ');
            parser.MoveNextOrFail(); // Skip Ip:
            // According to RFC 1213, "1" indicates "acting as a gateway". "2" indicates "NOT acting as a gateway".
            parser.MoveNextOrFail(); // Skip forwarding
            return parser.ParseNextInt32();
        }

        internal static int ParseRawIntFile(string filePath)
        {
            int ret;
            if (!int.TryParse(ReadAllText(filePath).Trim(), out ret))
            {
                throw ExceptionHelper.CreateForParseFailure();
            }

            return ret;
        }

        internal static long ParseRawLongFile(string filePath)
        {
            long ret;
            if (!long.TryParse(ReadAllText(filePath).Trim(), out ret))
            {
                throw ExceptionHelper.CreateForParseFailure();
            }

            return ret;
        }

        internal static int ParseRawHexFileAsInt(string filePath)
        {
            return Convert.ToInt32(ReadAllText(filePath).Trim(), 16);
        }

        private static int CountOccurrences(string value, string candidate)
        {
            Debug.Assert(candidate != null, "CountOccurrences: Candidate string was null.");
            int index = 0;
            int occurrences = 0;
            while (index != -1)
            {
                index = candidate.IndexOf(value, index + 1, StringComparison.Ordinal);
                if (index != -1)
                {
                    occurrences++;
                }
            }

            return occurrences;
        }
    }
}
