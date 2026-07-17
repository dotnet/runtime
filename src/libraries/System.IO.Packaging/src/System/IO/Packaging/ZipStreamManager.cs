// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace System.IO.Packaging
{
    internal sealed class ZipStreamManager
    {
        private readonly ZipArchive _zipArchive;
        private readonly FileAccess _packageFileAccess;
        private readonly FileMode _packageFileMode;

        public ZipStreamManager(ZipArchive zipArchive, FileMode packageFileMode, FileAccess packageFileAccess)
        {
            _zipArchive = zipArchive;
            _packageFileMode = packageFileMode;
            _packageFileAccess = packageFileAccess;
        }

        public Stream Open(ZipArchiveEntry zipArchiveEntry, FileAccess streamFileAccess, bool discardExistingContent = false, bool requireSeekableStream = false)
        {
            bool canRead = true;
            bool canWrite = true;
            switch (_packageFileAccess)
            {
                case FileAccess.Read:
                    switch (streamFileAccess)
                    {
                        case FileAccess.Read:
                            canRead = true;
                            canWrite = false;
                            break;
                        case FileAccess.Write:
                            canRead = false;
                            canWrite = false;
                            break;
                        case FileAccess.ReadWrite:
                            canRead = true;
                            canWrite = false;
                            break;
                    }
                    break;
                case FileAccess.Write:
                    switch (streamFileAccess)
                    {
                        case FileAccess.Read:
                            canRead = false;
                            canWrite = false;
                            break;
                        case FileAccess.Write:
                            canRead = false;
                            canWrite = true;
                            break;
                        case FileAccess.ReadWrite:
                            canRead = false;
                            canWrite = true;
                            break;
                    }
                    break;
                case FileAccess.ReadWrite:
                    switch (streamFileAccess)
                    {
                        case FileAccess.Read:
                            canRead = true;
                            canWrite = false;
                            break;
                        case FileAccess.Write:
                            canRead = false;
                            canWrite = true;
                            break;
                        case FileAccess.ReadWrite:
                            canRead = true;
                            canWrite = true;
                            break;
                    }
                    break;
            }

            // Choose the most efficient way to open the entry based on how the returned stream will be used.
            // In Update mode the parameterless Open() decompresses and loads the whole entry into memory:
            //  - When the entry will be overwritten (discardExistingContent, e.g. FileMode.Create), open with
            //    FileAccess.Write to discard the existing content without loading it just to truncate it.
            //  - When the caller only reads (canRead && !canWrite), open with FileAccess.Read to stream directly
            //    from the archive instead of buffering the whole entry in memory. NOTE: for compressed entries
            //    the resulting stream is forward-only (not seekable) and reflects the on-disk content only, so
            //    callers that need to seek (e.g. the interleaved part reader) opt out via requireSeekableStream.
            Stream ns;
#if NET11_0_OR_GREATER
            ns = (_zipArchive.Mode, discardExistingContent, canRead, canWrite, requireSeekableStream) switch
            {
                (ZipArchiveMode.Update, true, _, _, _) => zipArchiveEntry.Open(FileAccess.Write),
                (ZipArchiveMode.Update, false, true, false, false) => zipArchiveEntry.Open(FileAccess.Read),
                _ => zipArchiveEntry.Open(),
            };
#else
            ns = zipArchiveEntry.Open();
#endif
            return new ZipWrappingStream(zipArchiveEntry, ns, _packageFileMode, _packageFileAccess, canRead, canWrite);
        }
    }
}
