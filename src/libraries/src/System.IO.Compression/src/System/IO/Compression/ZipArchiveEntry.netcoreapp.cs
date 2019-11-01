// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO.Compression
{
    public partial class ZipArchiveEntry
    {
        private sealed partial class DirectToArchiveWriterStream : Stream
        {
            public override void Write(ReadOnlySpan<byte> source)
            {
                ThrowIfDisposed();
                Debug.Assert(CanWrite);

                // if we're not actually writing anything, we don't want to trigger the header
                if (source.Length == 0)
                    return;

                if (!_everWritten)
                {
                    _everWritten = true;
                    // write local header, we are good to go
                    _usedZip64inLH = _entry.WriteLocalFileHeader(isEmptyFile: false);
                }

                _crcSizeStream.Write(source);
                _position += source.Length;
            }
        }
    }
}
