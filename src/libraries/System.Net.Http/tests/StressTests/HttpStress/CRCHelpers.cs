// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;

namespace HttpStress
{
    public static class CRCHelpers
    {
        public const ulong InitialCrc = 0xffffffffL;

        public static ulong UpdateCrC(ulong crc, string text, Encoding? encoding = null)
        {
            encoding = encoding ?? Encoding.ASCII;
            byte[] bytes = encoding.GetBytes(text);
            return CRC.UpdateCRC(crc, bytes);
        }

        public static ulong CalculateCRC(string text, Encoding? encoding = null) => UpdateCrC(InitialCrc, text, encoding) ^ InitialCrc;

        public static ulong CalculateHeaderCrc<T>(IEnumerable<(string name, T)> headers, Encoding? encoding = null) where T : IEnumerable<string>
        {
            ulong checksum = InitialCrc;

            foreach ((string name, IEnumerable<string> values) in headers)
            {
                checksum = UpdateCrC(checksum, name);
                foreach (string value in values) 
                {
                    checksum = UpdateCrC(checksum, value);
                }
            }

            return checksum ^ InitialCrc;
        }
    }
}
