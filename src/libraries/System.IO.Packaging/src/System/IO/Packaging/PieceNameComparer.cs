// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.IO.Packaging
{
    internal sealed class PieceNameComparer : IComparer<PieceInfo>
    {
        //For comparing the piece names we consider the prefix name and piece numbers
        //Pieces that are terminal and non terminal with the same number and same prefix
        //number will be treated as equivalent.
        //For example - /partA/[number].piece and /partA[number].last.piece will be treated
        //to be equivalent, as in a well-formed package either one of them can be present,
        //not both.
        int IComparer<PieceInfo>.Compare(PieceInfo? pieceInfoA, PieceInfo? pieceInfoB)
        {
            //Even though most comparers allow for comparisons with null, we assert here, as
            //this is an internal class and we are sure that pieceInfoA and pieceInfoB passed
            //in here should be non-null, else it would be a logical error.
            Debug.Assert(pieceInfoA != null);
            Debug.Assert(pieceInfoB != null);

            int result = string.Compare(
                pieceInfoA.NormalizedPrefixName,
                pieceInfoB.NormalizedPrefixName,
                StringComparison.Ordinal);

            if (result != 0)
                return result;

            result = pieceInfoA.PieceNumber - pieceInfoB.PieceNumber;

            return result;
        }
    }
}
