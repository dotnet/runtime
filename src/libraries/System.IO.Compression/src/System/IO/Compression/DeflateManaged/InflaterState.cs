// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    // Do not rearrange the enum values.
    internal enum InflaterState
    {
        ReadingHeader = 0,           // Only applies to GZIP

        ReadingBFinal = 2,               // About to read bfinal bit
        ReadingBType = 3,                // About to read blockType bits

        ReadingNumLitCodes = 4,          // About to read # literal codes
        ReadingNumDistCodes = 5,         // About to read # dist codes
        ReadingNumCodeLengthCodes = 6,   // About to read # code length codes
        ReadingCodeLengthCodes = 7,      // In the middle of reading the code length codes
        ReadingTreeCodesBefore = 8,      // In the middle of reading tree codes (loop top)
        ReadingTreeCodesAfter = 9,       // In the middle of reading tree codes (extension; code > 15)

        DecodeTop = 10,                  // About to decode a literal (char/match) in a compressed block
        HaveInitialLength = 11,          // Decoding a match, have the literal code (base length)
        HaveFullLength = 12,             // Ditto, now have the full match length (incl. extra length bits)
        HaveDistCode = 13,               // Ditto, now have the distance code also, need extra dist bits

        /* uncompressed blocks */
        UncompressedAligning = 15,
        UncompressedByte1 = 16,
        UncompressedByte2 = 17,
        UncompressedByte3 = 18,
        UncompressedByte4 = 19,
        DecodingUncompressed = 20,

        // These three apply only to GZIP
        StartReadingFooter = 21,     // (Initialisation for reading footer)
        ReadingFooter = 22,
        VerifyingFooter = 23,

        Done = 24 // Finished
    }
}
