// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Net.NetworkInformation
{
    public class PhysicalAddress
    {
        private readonly byte[] _address = null;
        private int _hash = 0;

        public static readonly PhysicalAddress None = new PhysicalAddress(Array.Empty<byte>());

        public PhysicalAddress(byte[] address)
        {
            _address = address;
        }

        public override int GetHashCode()
        {
            if (_hash == 0)
            {
                int hash = 0;

                int i;
                int size = _address.Length & ~3;

                for (i = 0; i < size; i += 4)
                {
                    hash ^= (int)_address[i]
                            | ((int)_address[i + 1] << 8)
                            | ((int)_address[i + 2] << 16)
                            | ((int)_address[i + 3] << 24);
                }

                if ((_address.Length & 3) != 0)
                {
                    int remnant = 0;
                    int shift = 0;

                    for (; i < _address.Length; ++i)
                    {
                        remnant |= ((int)_address[i]) << shift;
                        shift += 8;
                    }

                    hash ^= remnant;
                }

                if (hash == 0)
                {
                    hash = 1;
                }

                _hash = hash;
            }

            return _hash;
        }

        public override bool Equals(object comparand)
        {
            PhysicalAddress address = comparand as PhysicalAddress;
            if (address == null)
            {
                return false;
            }

            if (_address.Length != address._address.Length)
            {
                return false;
            }

            if (GetHashCode() != address.GetHashCode())
            {
                return false;
            }

            for (int i = 0; i < address._address.Length; i++)
            {
                if (_address[i] != address._address[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            return string.Create(_address.Length * 2, _address, (span, addr) =>
            {
                int p = 0;
                foreach (byte value in addr)
                {
                    byte upper = (byte)(value >> 4), lower = (byte)(value & 0xF);
                    span[p++] = (char)(upper + (upper < 10 ? '0' : 'A' - 10));
                    span[p++] = (char)(lower + (lower < 10 ? '0' : 'A' - 10));
                }
            });
        }

        public byte[] GetAddressBytes()
        {
            return (byte[])_address.Clone();
        }

        public static PhysicalAddress Parse(string address) => address != null ? Parse(address.AsSpan()) : None;

        public static PhysicalAddress Parse(ReadOnlySpan<char> address)
        {
            if (!TryParse(address, out PhysicalAddress value))
                throw new FormatException(SR.Format(SR.net_bad_mac_address, new string(address)));

            return value;
        }

        public static bool TryParse(string address, out PhysicalAddress value)
        {
            if (address == null)
            {
                value = None;
                return true;
            }

            return TryParse(address.AsSpan(), out value);
        }

        public static bool TryParse(ReadOnlySpan<char> address, out PhysicalAddress value)
        {
            int validSegmentLength;
            char? delimiter = null;
            byte[] buffer;
            value = null;

            if (address.Contains('-'))
            {
                if ((address.Length + 1) % 3 != 0)
                {
                    return false;
                }

                delimiter = '-';
                buffer = new byte[(address.Length + 1) / 3]; // allow any length that's a multiple of 3
                validSegmentLength = 2;
            }
            else if (address.Contains(':'))
            {
                delimiter = ':';

                if (!TryGetValidSegmentLength(address, ':', out validSegmentLength))
                {
                    return false;
                }

                if (validSegmentLength != 2 && validSegmentLength != 4)
                {
                    return false;
                }
                buffer = new byte[6];
            }
            else if (address.Contains('.'))
            {
                delimiter = '.';

                if (!TryGetValidSegmentLength(address, '.', out validSegmentLength))
                {
                    return false;
                }

                if (validSegmentLength != 4)
                {
                    return false;
                }
                buffer = new byte[6];
            }
            else
            {
                if (address.Length % 2 > 0)
                {
                    return false;
                }

                validSegmentLength = address.Length;
                buffer = new byte[address.Length / 2];
            }

            int validCount = 0;
            int j = 0;
            for (int i = 0; i < address.Length; i++)
            {
                int character = address[i];

                if (character >= '0' && character <= '9')
                {
                    character -= '0';
                }
                else if (character >= 'A' && character <= 'F')
                {
                    character -= ('A' - 10);
                }
                else if (character >= 'a' && character <= 'f')
                {
                    character -= ('a' - 10);
                }
                else
                {
                    if (delimiter == character && validCount == validSegmentLength)
                    {
                        validCount = 0;
                        continue;
                    }

                    return false;
                }

                // we had too many characters after the last delimiter
                if (validCount >= validSegmentLength)
                {
                    return false;
                }

                if (validCount % 2 == 0)
                {
                    buffer[j] = (byte)(character << 4);
                }
                else
                {
                    buffer[j++] |= (byte)character;
                }

                validCount++;
            }

            // we had too few characters after the last delimiter
            if (validCount < validSegmentLength)
            {
                return false;
            }

            value = new PhysicalAddress(buffer);
            return true;
        }

        private static bool TryGetValidSegmentLength(ReadOnlySpan<char> address, char delimiter, out int value)
        {
            value = -1;
            int segments = 1;
            int validSegmentLength = 0;
            for (int i = 0; i < address.Length; i++)
            {
                if (address[i] == delimiter)
                {
                    if (validSegmentLength == 0)
                    {
                        validSegmentLength = i;
                    }
                    else if ((i - (segments - 1)) % validSegmentLength != 0)
                    {
                        // segments - 1 = num of delimeters. Return false if new segment isn't the validSegmentLength
                        return false;
                    }

                    segments++;
                }
            }

            if (segments * validSegmentLength != 12)
            {
                return false;
            }

            value = validSegmentLength;
            return true;
        }
    }
}
