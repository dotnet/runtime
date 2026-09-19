// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        /// <summary>Reads the next CBOR token, without advancing the reader.</summary>
        /// <returns>An object that represents the current CBOR reader state.</returns>
        /// <exception cref="CborContentException">The underlying data is not a well-formed CBOR encoding.</exception>
        /// <remarks>
        /// <para>If the reader's current data is not the final block, <see cref="CborReaderState.NeedsMoreData" /> is returned
        /// when the next token is incomplete in the current buffer, instead of throwing <see cref="CborContentException" />.
        /// Any other returned token state guarantees that the method reading that single token will not fail
        /// due to an unexpected end of the data, unless the token declares a definite length that can never fit
        /// a single buffer, in which case the reading method throws <see cref="CborContentException" />.</para>
        /// <para>Methods consuming multiple tokens at once, such as <see cref="ReadByteString" /> on indefinite-length strings
        /// or <see cref="ReadEncodedValue" />, can still throw <see cref="CborContentException" /> on truncated data.</para>
        /// </remarks>
        public CborReaderState PeekState()
        {
            if (_cachedState == CborReaderState.Undefined)
            {
                _cachedState = PeekStateCore();
            }

            return _cachedState;
        }

        private CborReaderState PeekStateCore()
        {
            if (_definiteLength - _itemsRead == 0)
            {
                // is at the end of a definite-length context
                switch (_currentMajorType)
                {
                    case null:
                        // finished reading root-level document
                        Debug.Assert(!AllowMultipleRootLevelValues);
                        return CborReaderState.Finished;

                    case CborMajorType.Array: return CborReaderState.EndArray;
                    case CborMajorType.Map: return CborReaderState.EndMap;
                    default:
                        Debug.Fail("CborReader internal error. Invalid CBOR major type pushed to stack.");
                        throw new Exception();
                }
            }

            if (_offset == _data.Length)
            {
                // is at the end of the read buffer
                if (!_isFinalBlock)
                {
                    // more data may follow, even at the end of a sequence of root-level values
                    return CborReaderState.NeedsMoreData;
                }

                if (_currentMajorType is null && _definiteLength is null)
                {
                    // is at the end of a well-defined sequence of root-level values
                    return CborReaderState.Finished;
                }
                else
                {
                    // incomplete CBOR document(s)
                    throw new CborContentException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
                }
            }

            // peek the next initial byte
            var initialByte = new CborInitialByte(_data.Span[_offset]);

            if (initialByte.InitialByte == CborInitialByte.IndefiniteLengthBreakByte)
            {
                if (_isTagContext)
                {
                    throw new CborContentException(SR.Cbor_Reader_InvalidCbor_TagNotFollowedByValue);
                }

                if (_definiteLength is null)
                {
                    switch (_currentMajorType)
                    {
                        case null:
                            // found a break byte at the end of a root-level data item sequence
                            Debug.Assert(AllowMultipleRootLevelValues);
                            throw new CborContentException(SR.Cbor_Reader_InvalidCbor_UnexpectedBreakByte);

                        case CborMajorType.ByteString: return CborReaderState.EndIndefiniteLengthByteString;
                        case CborMajorType.TextString: return CborReaderState.EndIndefiniteLengthTextString;
                        case CborMajorType.Array: return CborReaderState.EndArray;
                        case CborMajorType.Map when _itemsRead % 2 == 0: return CborReaderState.EndMap;
                        case CborMajorType.Map:
                            throw new CborContentException(SR.Cbor_Reader_InvalidCbor_KeyMissingValue);
                        default:
                            Debug.Fail("CborReader internal error. Invalid CBOR major type pushed to stack.");
                            throw new Exception();
                    };
                }
                else
                {
                    throw new CborContentException(SR.Cbor_Reader_InvalidCbor_UnexpectedBreakByte);
                }
            }

            if (_definiteLength is null && _currentMajorType != null)
            {
                // is at indefinite-length nested data item
                switch (_currentMajorType.Value)
                {
                    case CborMajorType.ByteString:
                    case CborMajorType.TextString:
                        if (initialByte.MajorType != _currentMajorType.Value)
                        {
                            throw new CborContentException(SR.Cbor_Reader_InvalidCbor_IndefiniteLengthStringContainsInvalidDataItem);
                        }

                        break;
                }
            }

            if (!_isFinalBlock && !IsNextTokenFullyAvailable(GetRemainingBytes()))
            {
                // the next token is truncated, but more data may follow
                return CborReaderState.NeedsMoreData;
            }

            switch (initialByte.MajorType)
            {
                case CborMajorType.UnsignedInteger: return CborReaderState.UnsignedInteger;
                case CborMajorType.NegativeInteger: return CborReaderState.NegativeInteger;
                case CborMajorType.ByteString:
                    return (initialByte.AdditionalInfo == CborAdditionalInfo.IndefiniteLength) ?
                            CborReaderState.StartIndefiniteLengthByteString :
                            CborReaderState.ByteString;

                case CborMajorType.TextString:
                    return (initialByte.AdditionalInfo == CborAdditionalInfo.IndefiniteLength) ?
                            CborReaderState.StartIndefiniteLengthTextString :
                            CborReaderState.TextString;

                case CborMajorType.Array: return CborReaderState.StartArray;
                case CborMajorType.Map: return CborReaderState.StartMap;
                case CborMajorType.Tag: return CborReaderState.Tag;
                case CborMajorType.Simple: return MapSimpleValueDataToReaderState(initialByte.AdditionalInfo);
                default:
                    Debug.Fail("CborReader internal error. Invalid CBOR major type.");
                    throw new Exception();
            };

            static CborReaderState MapSimpleValueDataToReaderState(CborAdditionalInfo value)
            {
                // https://tools.ietf.org/html/rfc7049#section-2.3

                switch (value)
                {
                    case (CborAdditionalInfo)CborSimpleValue.Null:
                        return CborReaderState.Null;
                    case (CborAdditionalInfo)CborSimpleValue.True:
                    case (CborAdditionalInfo)CborSimpleValue.False:
                        return CborReaderState.Boolean;
                    case CborAdditionalInfo.Additional16BitData:
                        return CborReaderState.HalfPrecisionFloat;
                    case CborAdditionalInfo.Additional32BitData:
                        return CborReaderState.SinglePrecisionFloat;
                    case CborAdditionalInfo.Additional64BitData:
                        return CborReaderState.DoublePrecisionFloat;
                    default:
                        return CborReaderState.SimpleValue;
                }
            }
        }

        // Checks that the token starting at remaining[0] is fully available in the buffer:
        // its header, the header's argument, and, for definite-length strings, their contents.
        // Checks availability only; malformed encodings are reported as available,
        // deferring to the corresponding Read method to throw.
        private static bool IsNextTokenFullyAvailable(ReadOnlySpan<byte> remaining)
        {
            Debug.Assert(!remaining.IsEmpty);

            var initialByte = new CborInitialByte(remaining[0]);
            int argumentWidth;

            switch (initialByte.AdditionalInfo)
            {
                case CborAdditionalInfo x when (x < CborAdditionalInfo.Additional8BitData):
                    argumentWidth = 0;
                    break;
                case CborAdditionalInfo.Additional8BitData:
                    argumentWidth = sizeof(byte);
                    break;
                case CborAdditionalInfo.Additional16BitData:
                    argumentWidth = sizeof(ushort);
                    break;
                case CborAdditionalInfo.Additional32BitData:
                    argumentWidth = sizeof(uint);
                    break;
                case CborAdditionalInfo.Additional64BitData:
                    argumentWidth = sizeof(ulong);
                    break;
                default:
                    // Indefinite-length tokens are the initial byte only; the reserved values 28-30
                    // are malformed regardless of any additional data.
                    return true;
            }

            if (remaining.Length < 1 + argumentWidth)
            {
                return false;
            }

            switch (initialByte.MajorType)
            {
                case CborMajorType.ByteString:
                case CborMajorType.TextString:
                    // definite-length string tokens include their contents
                    ulong length = argumentWidth switch
                    {
                        0 => (ulong)initialByte.AdditionalInfo,
                        sizeof(byte) => remaining[1],
                        sizeof(ushort) => BinaryPrimitives.ReadUInt16BigEndian(remaining.Slice(1)),
                        sizeof(uint) => BinaryPrimitives.ReadUInt32BigEndian(remaining.Slice(1)),
                        _ => BinaryPrimitives.ReadUInt64BigEndian(remaining.Slice(1)),
                    };

                    if (length > (ulong)(int.MaxValue - (1 + argumentWidth)))
                    {
                        // can never fit a single buffer, so this is not a truncation condition
                        return true;
                    }

                    return (ulong)(remaining.Length - (1 + argumentWidth)) >= length;

                default:
                    // For all other tokens the argument concludes the token: integers and float or
                    // simple-value payloads are encoded in the argument itself, and the contents of
                    // tags, arrays and maps are separate tokens.
                    return true;
            }
        }
    }
}
