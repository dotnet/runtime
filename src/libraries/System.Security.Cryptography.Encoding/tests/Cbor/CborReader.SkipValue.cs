// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        public void SkipValue()
        {
            CborReaderState state = Peek();

            switch (state)
            {
                case CborReaderState.UnsignedInteger: ReadUInt64(); break;
                case CborReaderState.NegativeInteger: ReadCborNegativeIntegerEncoding(); break;

                // TOCONSIDER: avoid allocating strings when skipping string values
                case CborReaderState.ByteString: ReadByteString(); break;
                case CborReaderState.TextString: ReadTextString(); break;

                case CborReaderState.StartByteString:
                    ReadStartByteStringIndefiniteLength();
                    while (Peek() != CborReaderState.EndByteString)
                    {
                        ReadByteString();
                    }
                    ReadEndByteStringIndefiniteLength();
                    break;

                case CborReaderState.StartTextString:
                    ReadStartTextStringIndefiniteLength();
                    while (Peek() != CborReaderState.EndTextString)
                    {
                        ReadTextString();
                    }
                    ReadEndTextStringIndefiniteLength();
                    break;

                case CborReaderState.StartArray:
                    ulong? arrayLength = ReadStartArray();
                    if (arrayLength != null)
                    {
                        SkipDefiniteLengthCollectionElements(arrayLength.Value);
                    }
                    else
                    {
                        SkipIndefiniteLengthCollectionElements(breakState: CborReaderState.EndArray);
                    }
                    ReadEndArray();
                    break;

                case CborReaderState.StartMap:
                    ulong? mapLength = ReadStartMap();
                    if (mapLength != null)
                    {
                        SkipDefiniteLengthCollectionElements(checked(2 * mapLength.Value));
                    }
                    else
                    {
                        SkipIndefiniteLengthCollectionElements(breakState: CborReaderState.EndMap);
                    }
                    ReadEndMap();
                    break;

                case CborReaderState.Tag: ReadTag(); SkipValue(); break;

                case CborReaderState.HalfPrecisionFloat:
                case CborReaderState.SinglePrecisionFloat:
                case CborReaderState.DoublePrecisionFloat:
                    ReadDouble();
                    break;

                case CborReaderState.Null: ReadNull(); break;
                case CborReaderState.Boolean: ReadBoolean(); break;
                case CborReaderState.SpecialValue: ReadSpecialValue(); break;

                case CborReaderState.FormatError_IndefiniteStringWithInvalidDataItems:
                case CborReaderState.FormatError_NoValueAfterTag:
                case CborReaderState.FormatError_IncompleteCborMap:
                case CborReaderState.FormatError_EndOfData:
                case CborReaderState.FormatError_InvalidBreakByte:
                    throw new FormatException($"CBOR format error {state}.");

                default:
                    throw new InvalidOperationException($"CBOR reader state {state} is not a value.");
            }

            void SkipDefiniteLengthCollectionElements(ulong length)
            {
                for (ulong i = 0; i < length; i++)
                {
                    SkipValue();
                }
            }

            void SkipIndefiniteLengthCollectionElements(CborReaderState breakState)
            {
                while(Peek() != breakState)
                {
                    SkipValue();
                }
            }
        }

    }
}
