// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        public void SkipValue(bool validateConformance = false) => SkipToAncestor(0, validateConformance);
        public void SkipToParent(bool validateConformance = false)
        {
            if (_currentMajorType is null)
            {
                throw new InvalidOperationException("CBOR reader is at the root context.");
            }

            SkipToAncestor(1, validateConformance);
        }

        private void SkipToAncestor(int depth, bool validateConformance)
        {
            Debug.Assert(0 <= depth && depth <= Depth);
            Checkpoint checkpoint = CreateCheckpoint();
            _isConformanceLevelCheckEnabled = validateConformance;

            try
            {
                do
                {
                    SkipNextNode(ref depth);
                } while (depth > 0);
            }
            catch
            {
                RestoreCheckpoint(in checkpoint);
                throw;
            }
            finally
            {
                _isConformanceLevelCheckEnabled = true;
            }
        }

        private void SkipNextNode(ref int depth)
        {
            CborReaderState state;

            // peek, skipping any tags we might encounter
            while ((state = PeekState()) == CborReaderState.Tag)
            {
                ReadTag();
            }

            switch (state)
            {
                case CborReaderState.UnsignedInteger:
                    ReadUInt64();
                    break;

                case CborReaderState.NegativeInteger:
                    ReadCborNegativeIntegerEncoding();
                    break;

                case CborReaderState.ByteString:
                    SkipString(type: CborMajorType.ByteString);
                    break;

                case CborReaderState.TextString:
                    SkipString(type: CborMajorType.TextString);
                    break;

                case CborReaderState.StartByteString:
                    ReadStartByteStringIndefiniteLength();
                    depth++;
                    break;

                case CborReaderState.EndByteString:
                    ValidatePop(state, depth);
                    ReadEndByteStringIndefiniteLength();
                    depth--;
                    break;

                case CborReaderState.StartTextString:
                    ReadStartTextStringIndefiniteLength();
                    depth++;
                    break;

                case CborReaderState.EndTextString:
                    ValidatePop(state, depth);
                    ReadEndTextStringIndefiniteLength();
                    depth--;
                    break;

                case CborReaderState.StartArray:
                    ReadStartArray();
                    depth++;
                    break;

                case CborReaderState.EndArray:
                    ValidatePop(state, depth);
                    ReadEndArray();
                    depth--;
                    break;

                case CborReaderState.StartMap:
                    ReadStartMap();
                    depth++;
                    break;

                case CborReaderState.EndMap:
                    ValidatePop(state, depth);
                    ReadEndMap();
                    depth--;
                    break;

                case CborReaderState.HalfPrecisionFloat:
                case CborReaderState.SinglePrecisionFloat:
                case CborReaderState.DoublePrecisionFloat:
                    ReadDouble();
                    break;

                case CborReaderState.Null:
                case CborReaderState.Boolean:
                case CborReaderState.SpecialValue:
                    ReadSimpleValue();
                    break;

                case CborReaderState.EndOfData:
                    throw new FormatException("Unexpected end of buffer.");
                case CborReaderState.FormatError:
                    throw new FormatException("Invalid CBOR format.");

                default:
                    throw new InvalidOperationException($"Unexpected CBOR reader state {state}.");
            }

            // guards against cases where the caller attempts to skip when reader is not positioned at the start of a value
            static void ValidatePop(CborReaderState state, int depth)
            {
                if (depth == 0)
                {
                    throw new InvalidOperationException($"Reader state {state} is not at start of a data item.");
                }
            }
        }
    }
}
