// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Compression;

namespace System.IO.Packaging
{
    /// <summary>
    /// The static class PieceNameHelper contains utilities to parse and create piece names
    /// in an adaptor-independent way.
    /// </summary>
    internal static class PieceNameHelper
    {

        internal static PieceNameComparer PieceNameComparer => _pieceNameComparer;

        /// <summary>
        /// Build a piece name from its constituents: part name, piece number
        /// and terminal status.
        /// The linearized result obeys the piece name syntax:
        ///   piece_name = prefix_name "/" "[" 1*digit "]" [".last"] ".piece"
        /// </summary>
        /// <param name="partName">A part name or the zip item name corresponding to a part name.</param>
        /// <param name="pieceNumber">The 0-based order number of the piece.</param>
        /// <param name="isLastPiece">Whether the piece is last in the part.</param>
        /// <returns>A Metro piece name.</returns>
        /// <exception cref="ArgumentException">If partName is a piece uri.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If pieceNumber is negative.</exception>
        internal static string CreatePieceName(string partName, int pieceNumber, bool isLastPiece)
        {
            Debug.Assert(pieceNumber >= 0, "Negative piece number.");

            return string.Format(CultureInfo.InvariantCulture, "{0}/[{1:D}]{2}.piece",
                partName,
                pieceNumber,
                isLastPiece ? ".last" : "");
        }

        /// <summary>
        /// Return true and create a PieceInfo if the name in the input ZipArchiveEntry parses
        /// as a piece name.
        /// </summary>
        /// <remarks>
        /// No Uri validation is carried out at this level. All that is checked is valid piece
        /// syntax. So the _prefixName returned as part of the PieceInfo will not necessarily
        /// a part name. For example, it could be the name of the content type stream.
        /// </remarks>
        internal static bool TryCreatePieceInfo(ZipArchiveEntry zipArchiveEntry, [NotNullWhen(returnValue: true)] out PieceInfo? pieceInfo)
        {
            Debug.Assert(zipArchiveEntry != null);

            pieceInfo = null;

            // Try to parse as a piece name.
            bool result = TryParseAsPieceName(zipArchiveEntry.FullName,
                out PieceNameInfo pieceNameConstituents);

            // Return the result and the output parameter.
            if (result)
                pieceInfo = new PieceInfo(zipArchiveEntry,
                    pieceNameConstituents.PartUri!,
                    pieceNameConstituents.PrefixName,
                    pieceNameConstituents.PieceNumber,
                    pieceNameConstituents.IsLastPiece);

            return result;
        }

        #region Scan Steps

        // The functions in this region conform to the delegate type ScanStepDelegate
        // and implement the following automaton for scanning a piece name from right to left:

        //   state                      transition      new state
        //   -----                      ----------      ---------
        //   FindPieceExtension         ".piece"        FindIsLast
        //   FindIsLast                 "].last"        FindPieceNumber
        //   FindIsLast                 "]"             FindPieceNumber
        //   FindPieceNumber            "/[" 1*digit    FindPartName (terminal state)

        // On entering the step, position is at the beginning of the last portion that was recognized.
        // So left-to-right scanning starts at position - 1 in each step.
        private delegate bool ScanStepDelegate(
            string path, ref int position, ref ScanStepDelegate nextStep, ref PieceNameInfo parseResults);

        // Look for ".piece".
        private static bool FindPieceExtension(string path, ref int position, ref ScanStepDelegate nextStep,
            ref PieceNameInfo parseResults)
        {
            if (!FindString(path, ref position, ".piece"))
                return false;

            nextStep = FindIsLast;
            return true;
        }

        // Look for "]" or "].last".
        private static bool FindIsLast(string path, ref int position, ref ScanStepDelegate nextStep,
            ref PieceNameInfo parseResults)
        {
            // Case of no ".last" member:
            if (path[position - 1] == ']')
            {
                parseResults.IsLastPiece = false;
                --position;
                nextStep = FindPieceNumber;
                return true;
            }

            // There has to be "].last".
            if (!FindString(path, ref position, "].last"))
                return false;

            parseResults.IsLastPiece = true;
            nextStep = FindPieceNumber;
            return true;
        }

        // Look for "/[" followed by decimal digits.
        private static bool FindPieceNumber(string path, ref int position, ref ScanStepDelegate nextStep,
            ref PieceNameInfo parseResults)
        {
            if (!char.IsDigit(path[position - 1]))
                return false;

            int pieceNumber = 0;
            int multiplier = 1; // rightmost digit is for units
            --position;
            do
            {
                pieceNumber += multiplier * (int)char.GetNumericValue(path[position]);
                multiplier *= 10;
            } while (char.IsDigit(path[--position]));

            // Point to the last digit found.
            ++position;

            //If we have a leading 0, then its not correct piecename syntax
            if (multiplier > 10 && (int)char.GetNumericValue(path[position]) == 0)
                return false;

            if (!FindString(path, ref position, "/["))
                return false;

            parseResults.PieceNumber = pieceNumber;
            nextStep = FindPartName;
            return true;
        }

        // Retrieve part name. The position points to the slash past the part name.
        // So simply return the prefix up to that slash.
        private static bool FindPartName(string path, ref int position, ref ScanStepDelegate nextStep,
            ref PieceNameInfo parseResults)
        {
            parseResults.PrefixName = path.Substring(0, position);

            // Subtract the length of the part name from position.
            position = 0;

            if (parseResults.PrefixName.Length == 0)
                return false;

            Uri partUri = new Uri(ZipPackage.GetOpcNameFromZipItemName(parseResults.PrefixName), UriKind.Relative);
            PackUriHelper.TryValidatePartUri(partUri, out parseResults.PartUri);
            return true;
        }

        #endregion Scan Steps

        /// <summary>
        /// Attempts to parse a name as a piece name. Returns true and places the
        /// output in pieceNameConstituents. Otherwise, returns false and returns
        /// the default constituent values pieceName, 0, and false.
        /// </summary>
        /// <param name="path">The input string.</param>
        /// <param name="parseResults">An object containing the prefix name (i.e. generally the part name), the 0-based order number of the piece, and whether the piece is last in the part.</param>
        /// <returns>True for parse success.</returns>
        /// <remarks>
        /// Syntax of a piece name:
        ///   piece_name = part_name "/" "[" 1*digit "]" [".last"] ".piece"
        /// </remarks>
        private static bool TryParseAsPieceName(string path, out PieceNameInfo parseResults)
        {
            parseResults = default; // initialize to CLR default values

            // Start from the end and look for ".piece".
            int position = path.Length;
            ScanStepDelegate nextStep = new ScanStepDelegate(FindPieceExtension);

            // Scan backward until the whole path has been scanned.
            while (position > 0)
            {
                if (!nextStep.Invoke(path, ref position, ref nextStep, ref parseResults))
                {
                    // Scan step failed. Return false.
                    parseResults.IsLastPiece = false;
                    parseResults.PieceNumber = 0;
                    parseResults.PrefixName = path;
                    parseResults.PartUri = null;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Look for 'query' backward in 'input' starting at 'position'.
        /// </summary>
        private static bool FindString(string input, ref int position, string query)
        {
            int queryPosition = query.Length;

            //The input string should have length that is greater than or equal to the
            //length of the query string.
            if (position < queryPosition)
                return false;

            while (--queryPosition >= 0)
            {
                --position;
                if (char.ToUpperInvariant(input[position]) != char.ToUpperInvariant(query[queryPosition]))
                    return false;
            }
            return true;
        }

        private static readonly PieceNameComparer _pieceNameComparer = new PieceNameComparer();

        /// <summary>
        /// The result of parsing a piece name as returned by the parsing methods of PieceNameHelper.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The first member, _prefixName, will be a part name if the input to parse begins with
        /// a part name, and a zip item name if it starts with a zip item name.
        /// </para>
        /// <para>
        /// In other words, all that precedes the suffixes is returned unanalyzed as an "prefix name"
        /// by the parse functions of the PieceNameHelper.
        /// </para>
        /// </remarks>
        private struct PieceNameInfo
        {
            internal PackUriHelper.ValidatedPartUri? PartUri;
            internal string PrefixName;
            internal int PieceNumber;
            internal bool IsLastPiece;
        }

    }
}
