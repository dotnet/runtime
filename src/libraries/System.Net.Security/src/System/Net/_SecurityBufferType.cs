// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Security
{
    internal enum SecurityBufferType
    {
        Empty = 0x00,
        Data = 0x01,
        Token = 0x02,
        Parameters = 0x03,
        Missing = 0x04,
        Extra = 0x05,
        Trailer = 0x06,
        Header = 0x07,
        Padding = 0x09,    // non-data padding
        Stream = 0x0A,
        ChannelBindings = 0x0E,
        TargetHost = 0x10,
        ReadOnlyFlag = unchecked((int)0x80000000),
        ReadOnlyWithChecksum = 0x10000000
    }
}
