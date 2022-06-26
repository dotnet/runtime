// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Compression;

namespace System.IO.Packaging
{
    /// <summary>
    /// A piece descriptor, made up of a ZipFileInfo and a PieceNameInfo.
    /// </summary>
    /// <remarks>
    /// PieceNameHelper implements IComparable in such a way as to enforce
    /// case-insensitive lexicographical order on &lt;name, number, isLast> triples.
    /// </remarks>
    internal sealed class PieceInfo
    {
        internal PieceInfo(ZipArchiveEntry zipArchiveEntry, PackUriHelper.ValidatedPartUri partUri, string prefixName, int pieceNumber, bool isLastPiece)
        {
            Debug.Assert(zipArchiveEntry != null);
            Debug.Assert(prefixName != null && prefixName != string.Empty);
            Debug.Assert(pieceNumber >= 0);

            ZipArchiveEntry = zipArchiveEntry;

            // partUri can be null to indicate that the prefixname is not a valid part name
            PartUri = partUri;
            PrefixName = prefixName;
            PieceNumber = pieceNumber;
            IsLastPiece = isLastPiece;

            // Currently as per the book, the prefix names/ logical names should be
            // compared in a case-insensitive manner.
            NormalizedPrefixName = PrefixName.ToUpperInvariant();
        }

        internal string NormalizedPrefixName { get; }

        internal string PrefixName { get; }

        internal int PieceNumber { get; }

        internal bool IsLastPiece { get; }

        internal PackUriHelper.ValidatedPartUri PartUri { get; }

        internal ZipArchiveEntry ZipArchiveEntry { get; }

    }
}
