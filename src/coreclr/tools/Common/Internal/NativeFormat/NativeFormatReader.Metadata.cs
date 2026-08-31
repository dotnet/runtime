// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// Native Format Reader
//
// Metadata / NativeLayoutInfo reading methods
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Internal.NativeFormat
{
    internal partial struct NativeParser
    {
        public BagElementKind GetBagElementKind()
        {
            return (BagElementKind)GetUnsigned();
        }

        public FixupSignatureKind GetFixupSignatureKind()
        {
            return (FixupSignatureKind)GetUnsigned();
        }

        public TypeSignatureKind GetTypeSignatureKind(out uint data)
        {
            uint val = GetUnsigned();
            data = (val >> 4);
            return (TypeSignatureKind)(val & 0xF);
        }

        public NativeParser GetLookbackParser(uint lookback)
        {
            // Adjust the lookback by the size of the TypeSignatureKind element and the minimum lookback size
            uint adjustedLookback = lookback + NativePrimitiveDecoder.GetUnsignedEncodingSize(lookback << 4) + 2;
            return new NativeParser(_reader, _offset - adjustedLookback);
        }

        public uint? GetUnsignedForBagElementKind(BagElementKind kindToFind)
        {
            var parser = this;

            BagElementKind kind;
            while ((kind = parser.GetBagElementKind()) != BagElementKind.End)
            {
                if (kind == kindToFind)
                    return parser.GetUnsigned();

                parser.SkipInteger();
            }

            return null;
        }

        public NativeParser GetParserForBagElementKind(BagElementKind kindToFind)
        {
            var parser = this;

            BagElementKind kind;
            while ((kind = parser.GetBagElementKind()) != BagElementKind.End)
            {
                if (kind == kindToFind)
                    return parser.GetParserFromRelativeOffset();

                parser.SkipInteger();
            }

            return default(NativeParser);
        }
    }
}
