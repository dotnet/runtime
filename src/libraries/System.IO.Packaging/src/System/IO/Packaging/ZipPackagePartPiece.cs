// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace System.IO.Packaging
{
    /// <summary>
    /// A part piece descriptor, made up of a ZipArchiveEntry and a PieceNameInfo.
    /// </summary>
    internal sealed class ZipPackagePartPiece : IComparable<ZipPackagePartPiece>
    {
        private const string PartPieceFileExtension = ".piece";
        private const string PartPieceLastExtension = "].last";

        #region Internal Methods
        /// <summary>
        /// Return true and create a ZipPackagePartPiece if the name in the input ZipArchiveEntry parses
        /// as a piece name.
        /// </summary>
        /// <remarks>
        /// No Uri validation is carried out at this level. All that is checked is valid piece syntax.
        /// This means that the PrefixName returned as part of the ZipPackagePartPiece will not necessarily
        /// be a part name. For example, it could be the name of the content type stream.
        /// </remarks>
        internal static bool TryParse(ZipArchiveEntry zipArchiveEntry, [NotNullWhen(true)] out ZipPackagePartPiece? partPiece)
        {
            if (zipArchiveEntry == null)
                throw new ArgumentNullException(nameof(zipArchiveEntry));

            partPiece = null;

            bool success = TryParseName(zipArchiveEntry.FullName, out PackUriHelper.ValidatedPartUri? partUri, out string? prefixName, out int pieceNumber, out bool isLastPiece);

            if (success)
            {
                partPiece = new ZipPackagePartPiece(zipArchiveEntry, partUri!, prefixName!, pieceNumber, isLastPiece);
            }

            return success;
        }

        /// <summary>
        /// Return true and populate the output parameters if the path parses as a piece name.
        /// </summary>
        /// <remarks>
        /// No Uri validation is carried out at this level. All that is checked is valid piece syntax.
        /// This means that the output prefix name will not necessarily be a part name. For example,
        /// it could be the name of the content type stream.
        /// </remarks>
        internal static bool TryParseName(string path, [NotNullWhen(true)] out PackUriHelper.ValidatedPartUri? partUri, [NotNullWhen(true)] out string? prefixName, out int pieceNumber, out bool isLastPiece)
        {
            bool success = true;
            int searchPosition = path.Length;
            // All piece names obey the syntax:
            //  prefix_name "/" "[" 1*digit "]" [".last"] ".piece"
            // Work backwards from the end of the full path, extracting and checking this metadata in stages.
            // Stage 1: extract the file extension of ".piece".
            int resultPosition = path.LastIndexOf(PartPieceFileExtension, StringComparison.OrdinalIgnoreCase);

            isLastPiece = false;
            pieceNumber = -1;
            prefixName = null;
            partUri = default;

            if (resultPosition < 1)
            {
                success = false;
            }
            else
            {
                // Stage 2: determine whether this piece name reflects the last piece in the part.
                // If this piece is the last piece in the part, the characters directly before the new
                // search position will be "].last"; if it's not, the character directly before the new
                // search position will be "]".
                searchPosition = resultPosition;

                if (path[searchPosition - 1] == ']')
                {
                    searchPosition--;
                    isLastPiece = false;
                }
                else if (path.Substring(0, searchPosition).EndsWith(PartPieceLastExtension, StringComparison.OrdinalIgnoreCase))
                {
                    searchPosition -= PartPieceLastExtension.Length;
                    isLastPiece = true;
                }
                else
                {
                    success = false;
                }
            }

            // Stage 3: extract the piece number. This is a number from before "].piece" or "].last.piece".
            // The OPC spec defines this as being a single digit, but some client applications (such as the XPS Document Writer)
            // write >10 part pieces. These should be parsed
            success = success
                && searchPosition > 1
                && char.IsDigit(path[searchPosition - 1]);

            if (success)
            {
                int digitStart;

                // Iterate backwards, character by character, until we find a non-digit character.
                for (digitStart = searchPosition; digitStart > 1 && char.IsDigit(path[digitStart - 1]); digitStart--)
                    ;

                success = int.TryParse(path.Substring(digitStart, searchPosition - digitStart), out pieceNumber);
                if (success)
                {
                    searchPosition = digitStart;
                }
            }

            // Stage 4: locate and remove the separator directly after the piece name
            success = success
                && searchPosition > 1
                && path[searchPosition - 1] == '['
                && path[searchPosition - 2] == '/';

            if (success)
            {
                searchPosition -= 2;

                // Stage 5: extract the piece name and validate it
                if (searchPosition > 0)
                {
                    int searchOffset = path[0] == '/' ? 1 : 0;

                    prefixName = path.Substring(searchOffset, searchPosition - searchOffset);

                    success = success
                        && Uri.TryCreate(ZipPackage.GetOpcNameFromZipItemName(prefixName), UriKind.Relative, out Uri? unvalidatedPartUri)
                        && PackUriHelper.TryValidatePartUri(unvalidatedPartUri, out partUri);
                }
                else
                {
                    success = false;
                }
            }

            return success;
        }

        internal static ZipPackagePartPiece Create(ZipArchive zipArchive, PackUriHelper.ValidatedPartUri? partUri, string prefixName, int pieceNumber, bool isLastPiece)
        {
            string newPieceFileName = FormattableString.Invariant($"{prefixName}/[{pieceNumber:D}]{(isLastPiece ? ".last" : string.Empty)}.piece");
            ZipArchiveEntry newPieceEntry = zipArchive.CreateEntry(newPieceFileName);

            return new ZipPackagePartPiece(newPieceEntry, partUri, prefixName, pieceNumber, isLastPiece);
        }
        #endregion

        #region Internal Constructors
        internal ZipPackagePartPiece(ZipArchiveEntry zipArchiveEntry, PackUriHelper.ValidatedPartUri? partUri, string prefixName, int pieceNumber, bool isLastPiece)
        {
            if (zipArchiveEntry == null)
                throw new ArgumentNullException(nameof(zipArchiveEntry));
            if (prefixName == null)
                throw new ArgumentNullException(nameof(prefixName));
            if (pieceNumber < 0)
                throw new ArgumentOutOfRangeException(nameof(pieceNumber));

            ZipArchiveEntry = zipArchiveEntry;

            // partUri is null if the prefix name is not a valid part name.
            PartUri = partUri;
            PrefixName = prefixName;
            PieceNumber = pieceNumber;
            IsLastPiece = isLastPiece;
        }
        #endregion

        #region Internal Properties

        internal string PrefixName { get; }

        internal string NormalizedPrefixName => PrefixName.ToUpperInvariant();

        internal int PieceNumber { get; }

        internal bool IsLastPiece { get; }

        internal PackUriHelper.ValidatedPartUri? PartUri { get; }

        internal ZipArchiveEntry ZipArchiveEntry { get; }
        #endregion

        #region Private Methods
        int IComparable<ZipPackagePartPiece>.CompareTo(ZipPackagePartPiece? other)
        {
            if (other == null)
                return 1;

            // When comparing part piece names, we only consider the prefix name and the piece numbers.
            // Pieces which are terminal and non-terminal (i.e. as represented by IsLastPiece) with the same
            // prefix name and piece number will be treated as equivalent.
            // This means that /partA/[1].piece and /partA/[1].last.piece will be considered equivalent,
            // since in a well-formed package only one of these can be present.

            int result = string.Compare(PrefixName, other.PrefixName, StringComparison.OrdinalIgnoreCase);

            if (result == 0)
            {
                result = PieceNumber.CompareTo(other.PieceNumber);
            }

            return result;
        }
        #endregion
    }
}
