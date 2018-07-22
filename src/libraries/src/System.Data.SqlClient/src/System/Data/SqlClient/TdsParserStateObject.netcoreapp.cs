// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.SqlClient
{
    internal abstract partial class TdsParserStateObject
    {
        public bool TryReadByteArray(Span<byte> buff, int len)
        {
            int ignored;
            return TryReadByteArray(buff, len, out ignored);
        }

        public bool TryReadByteArray(Span<byte> buff, int len, out int totalRead)
        {
            totalRead = 0;

#if DEBUG
            if (_snapshot != null && _snapshot.DoPend())
            {
                _networkPacketTaskSource = new TaskCompletionSource<object>();
                Interlocked.MemoryBarrier();

                if (_forcePendingReadsToWaitForUser)
                {
                    _realNetworkPacketTaskSource = new TaskCompletionSource<object>();
                    _realNetworkPacketTaskSource.SetResult(null);
                }
                else
                {
                    _networkPacketTaskSource.TrySetResult(null);
                }
                return false;
            }
#endif

            Debug.Assert(buff.Length == 0 || buff.Length >= len, "Invalid length sent to ReadByteArray()!");

            // loop through and read up to array length
            while (len > 0)
            {
                if ((_inBytesPacket == 0) || (_inBytesUsed == _inBytesRead))
                {
                    if (!TryPrepareBuffer())
                    {
                        return false;
                    }
                }

                int bytesToRead = Math.Min(len, Math.Min(_inBytesPacket, _inBytesRead - _inBytesUsed));
                Debug.Assert(bytesToRead > 0, "0 byte read in TryReadByteArray");
                if (buff.Length > 0)
                {
                    ReadOnlySpan<byte> inBuffSpan = new ReadOnlySpan<byte>(_inBuff, _inBytesUsed, bytesToRead);
                    inBuffSpan.CopyTo(buff);
                }

                totalRead += bytesToRead;
                _inBytesUsed += bytesToRead;
                _inBytesPacket -= bytesToRead;
                len -= bytesToRead;

                AssertValidState();
            }

            if ((_messageStatus != TdsEnums.ST_EOM) && ((_inBytesPacket == 0) || (_inBytesUsed == _inBytesRead)))
            {
                if (!TryPrepareBuffer())
                {
                    return false;
                }
            }

            AssertValidState();
            return true;
        }
    }
}
