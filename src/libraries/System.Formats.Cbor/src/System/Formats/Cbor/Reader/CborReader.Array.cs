// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        /// <summary>
        ///   Reads the next data item as the start of an array (major type 4).
        /// </summary>
        /// <returns>
        ///   The length of the definite-length array, or <see langword="null" /> if the array is indefinite-length.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   the next data item does not have the correct major type.
        /// </exception>
        /// <exception cref="CborContentException">
        ///   the next value has an invalid CBOR encoding. -or-
        ///   there was an unexpected end of CBOR encoding data. -or-
        ///   the next value uses a CBOR encoding that is not valid under the current conformance mode.
        /// </exception>
        public int? ReadStartArray()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Array);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceModeCheckEnabled && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
                {
                    throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
                }

                AdvanceBuffer(1);
                PushDataItem(CborMajorType.Array, null);
                return null;
            }
            else
            {
                ReadOnlySpan<byte> buffer = GetRemainingBytes();
                int arrayLength = DecodeDefiniteLength(header, buffer, out int bytesRead);

                AdvanceBuffer(bytesRead);
                PushDataItem(CborMajorType.Array, arrayLength);
                return arrayLength;
            }
        }

        /// <summary>
        ///   Reads the end of an array (major type 4).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   the current context is not an array. -or-
        ///   the reader is not at the end of the array.
        /// </exception>
        /// <exception cref="CborContentException">
        ///   the next value has an invalid CBOR encoding. -or-
        ///   there was an unexpected end of CBOR encoding data
        /// </exception>
        public void ReadEndArray()
        {
            if (_definiteLength is null)
            {
                ValidateNextByteIsBreakByte();
                PopDataItem(expectedType: CborMajorType.Array);
                AdvanceDataItemCounters();
                AdvanceBuffer(1);
            }
            else
            {
                PopDataItem(expectedType: CborMajorType.Array);
                AdvanceDataItemCounters();
            }
        }
    }
}
