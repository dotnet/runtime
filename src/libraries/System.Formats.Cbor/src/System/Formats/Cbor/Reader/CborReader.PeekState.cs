// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        /// <summary>Reads the next CBOR token, without advancing the reader.</summary>
        /// <returns>An object that represents the current CBOR reader state.</returns>
        /// <exception cref="CborContentException">The underlying data is not a well-formed CBOR encoding.</exception>
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
    }
}
