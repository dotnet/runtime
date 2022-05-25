// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Drawing.Internal
{
    internal sealed partial class GPStream : Interop.Ole32.IStream
    {
        private readonly Stream _dataStream;

        // to support seeking ahead of the stream length...
        private long _virtualPosition = -1;

        internal GPStream(Stream stream, bool makeSeekable = true)
        {
            if (makeSeekable && !stream.CanSeek)
            {
                // Copy to a memory stream so we can seek
                MemoryStream memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                _dataStream = memoryStream;
            }
            else
            {
                _dataStream = stream;
            }
        }

        private void ActualizeVirtualPosition()
        {
            if (_virtualPosition == -1)
                return;

            if (_virtualPosition > _dataStream.Length)
                _dataStream.SetLength(_virtualPosition);

            _dataStream.Position = _virtualPosition;

            _virtualPosition = -1;
        }

        public void Commit(uint grfCommitFlags)
        {
            _dataStream.Flush();

            // Extend the length of the file if needed.
            ActualizeVirtualPosition();
        }

        public unsafe void Read(byte* pv, uint cb, uint* pcbRead)
        {
            ActualizeVirtualPosition();

            // Stream Span API isn't available in 2.0
            Span<byte> buffer = new Span<byte>(pv, checked((int)cb));
            int read = _dataStream.Read(buffer);

            if (pcbRead != null)
                *pcbRead = (uint)read;
        }

        public void Revert()
        {
            // We never report ourselves as Transacted, so we can just ignore this.
        }

        public unsafe void Seek(long dlibMove, SeekOrigin dwOrigin, ulong* plibNewPosition)
        {
            long position = _virtualPosition;
            if (_virtualPosition == -1)
            {
                position = _dataStream.Position;
            }

            long length = _dataStream.Length;
            switch (dwOrigin)
            {
                case SeekOrigin.Begin:
                    if (dlibMove <= length)
                    {
                        _dataStream.Position = dlibMove;
                        _virtualPosition = -1;
                    }
                    else
                    {
                        _virtualPosition = dlibMove;
                    }
                    break;
                case SeekOrigin.End:
                    if (dlibMove <= 0)
                    {
                        _dataStream.Position = length + dlibMove;
                        _virtualPosition = -1;
                    }
                    else
                    {
                        _virtualPosition = length + dlibMove;
                    }
                    break;
                case SeekOrigin.Current:
                    if (dlibMove + position <= length)
                    {
                        _dataStream.Position = position + dlibMove;
                        _virtualPosition = -1;
                    }
                    else
                    {
                        _virtualPosition = dlibMove + position;
                    }
                    break;
            }

            if (plibNewPosition == null)
                return;

            if (_virtualPosition != -1)
            {
                *plibNewPosition = (ulong)_virtualPosition;
            }
            else
            {
                *plibNewPosition = (ulong)_dataStream.Position;
            }
        }

        public void SetSize(ulong value)
        {
            _dataStream.SetLength(checked((long)value));
        }

        public unsafe void Stat(Interop.Ole32.STATSTG* pstatstg, Interop.Ole32.STATFLAG grfStatFlag)
        {
            if (pstatstg == null)
            {
                throw new ArgumentNullException(nameof(pstatstg));
            }

            *pstatstg = new Interop.Ole32.STATSTG
            {
                cbSize = (ulong)_dataStream.Length,
                type = Interop.Ole32.STGTY.STGTY_STREAM,

                // Default read/write access is STGM_READ, which == 0
                grfMode = _dataStream.CanWrite
                    ? _dataStream.CanRead
                        ? Interop.Ole32.STGM.STGM_READWRITE
                        : Interop.Ole32.STGM.STGM_WRITE
                    : Interop.Ole32.STGM.Default
            };

            if (grfStatFlag == Interop.Ole32.STATFLAG.STATFLAG_DEFAULT)
            {
                // Caller wants a name
                pstatstg->AllocName(_dataStream is FileStream fs ? fs.Name : _dataStream.ToString());
            }
        }

        public unsafe void Write(byte* pv, uint cb, uint* pcbWritten)
        {
            ActualizeVirtualPosition();

            var buffer = new ReadOnlySpan<byte>(pv, checked((int)cb));
            _dataStream.Write(buffer);

            if (pcbWritten != null)
                *pcbWritten = cb;
        }

        public Interop.HRESULT LockRegion(ulong libOffset, ulong cb, uint dwLockType)
        {
            // Documented way to say we don't support locking
            return Interop.HRESULT.STG_E_INVALIDFUNCTION;
        }

        public Interop.HRESULT UnlockRegion(ulong libOffset, ulong cb, uint dwLockType)
        {
            // Documented way to say we don't support locking
            return Interop.HRESULT.STG_E_INVALIDFUNCTION;
        }
    }
}
