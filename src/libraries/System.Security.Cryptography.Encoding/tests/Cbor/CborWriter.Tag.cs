// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using Microsoft.Extensions.Logging;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
    {
        public void WriteTag(CborTag tag)
        {
            WriteUnsignedInteger(CborMajorType.Tag, (ulong)tag);
            _isTagContext = true;
        }

        // Additional tagged type support

        public void WriteDateTimeOffset(DateTimeOffset value)
        {
            WriteTag(CborTag.DateTimeString);
            WriteTextString(value.ToString("O"));
        }

        public void WriteUnixTimeSeconds(long unixTimeSeconds)
        {
            WriteTag(CborTag.DateTimeUnixSeconds);
            WriteInt64(unixTimeSeconds);
        }
    }
}
