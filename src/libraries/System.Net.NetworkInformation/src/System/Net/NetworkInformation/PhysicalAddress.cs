// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Net.NetworkInformation
{
    public class PhysicalAddress
    {
        private readonly byte[] _address = null;
        private bool _hashNotComputed = true;
        private int _hash = 0;

        public static readonly PhysicalAddress None = new PhysicalAddress(Array.Empty<byte>());

        public PhysicalAddress(byte[] address)
        {
            _address = address;
        }

        public override int GetHashCode()
        {
            if (_hashNotComputed)
            {
                _hashNotComputed = false;
                _hash = 0;

                int i;
                int size = _address.Length & ~3;

                for (i = 0; i < size; i += 4)
                {
                    _hash ^= (int)_address[i]
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

                    _hash ^= remnant;
                }
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

        public static PhysicalAddress Parse(string address)
        {
            int validSegmentLength;
            char? delimiter = null;
            byte[] buffer;

            if (address == null)
            {
                return None;
            }

            if (address.Contains('-'))
            {
                if ((address.Length + 1) % 3 != 0)
                {
                    ThrowBadAddressException(address);
                }

                delimiter = '-';
                buffer = new byte[(address.Length + 1) / 3]; // allow any length that's a multiple of 3
                validSegmentLength = 2;
            }
            else if (address.Contains(':'))
            {
                delimiter = ':';
                validSegmentLength = GetValidSegmentLength(address, ':');
                if (validSegmentLength != 2 && validSegmentLength != 4)
                {
                    ThrowBadAddressException(address);
                }
                buffer = new byte[6];
            }
            else if (address.Contains('.'))
            {
                delimiter = '.';
                validSegmentLength = GetValidSegmentLength(address, '.');
                if (validSegmentLength != 4)
                {
                    ThrowBadAddressException(address);
                }
                buffer = new byte[6];
            }
            else
            {
                if (address.Length % 2 > 0)
                {
                    ThrowBadAddressException(address);
                }

                validSegmentLength = address.Length;
                buffer = new byte[address.Length / 2];
            }

            int validCount = 0;
            int j = 0;
            for (int i = 0; i < address.Length; i++)
            {
                int value = address[i];

                if (value >= '0' && value <= '9')
                {
                    value -= '0';
                }
                else if (value >= 'A' && value <= 'F')
                {
                    value -= ('A' - 10);
                }
                else if (value >= 'a' && value <= 'f')
                {
                    value -= ('a' - 10);
                }
                else
                {
                    if (delimiter == value && validCount == validSegmentLength)
                    {
                        validCount = 0;
                        continue;
                    }

                    ThrowBadAddressException(address);
                }

                // we had too many characters after the last delimiter
                if (validCount >= validSegmentLength)
                {
                    ThrowBadAddressException(address);
                }

                if (validCount % 2 == 0)
                {
                    buffer[j] = (byte)(value << 4);
                }
                else
                {
                    buffer[j++] |= (byte)value;
                }

                validCount++;
            }

            // we had too few characters after the last delimiter
            if (validCount < validSegmentLength)
            {
                ThrowBadAddressException(address);
            }

            return new PhysicalAddress(buffer);

            static void ThrowBadAddressException(string address) =>
                throw new FormatException(SR.Format(SR.net_bad_mac_address, address));
        }

        private static int GetValidSegmentLength(string address, char delimiter)
        {
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
                        // segments - 1 = num of delimeters. Throw if new segment isn't the validSegmentLength
                        throw new FormatException(SR.Format(SR.net_bad_mac_address, address));
                    }

                    segments++;
                }
            }

            if (segments * validSegmentLength != 12)
            {
                throw new FormatException(SR.Format(SR.net_bad_mac_address, address));
            }

            return validSegmentLength;
        }
    }
}
