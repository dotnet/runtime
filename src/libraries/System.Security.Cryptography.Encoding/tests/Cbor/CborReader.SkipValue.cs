// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        /// <summary>
        ///   Reads the contents of the next value, discarding the result and advancing the reader.
        /// </summary>
        /// <param name="disableConformanceLevelChecks">
        ///   Disable conformance level validation for the skipped value,
        ///   equivalent to using <see cref="CborConformanceLevel.Lax"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   the reader is not at the start of new value.
        /// </exception>
        /// <exception cref="FormatException">
        ///   the next value has an invalid CBOR encoding. -or-
        ///   there was an unexpected end of CBOR encoding data. -or-
        ///   the next value uses a CBOR encoding that is not valid under the current conformance level.
        /// </exception>
        public void SkipValue(bool disableConformanceLevelChecks = false)
        {
            SkipToAncestor(0, disableConformanceLevelChecks);
        }

        /// <summary>
        ///   Reads the remaining contents of the current value context,
        ///   discarding results and advancing the reader to the next value
        ///   in the parent context.
        /// </summary>
        /// <param name="disableConformanceLevelChecks">
        ///   Disable conformance level validation for the skipped values,
        ///   equivalent to using <see cref="CborConformanceLevel.Lax"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   the reader is at the root context
        /// </exception>
        /// <exception cref="FormatException">
        ///   the next value has an invalid CBOR encoding. -or-
        ///   there was an unexpected end of CBOR encoding data. -or-
        ///   the next value uses a CBOR encoding that is not valid under the current conformance level.
        /// </exception>
        public void SkipToParent(bool disableConformanceLevelChecks = false)
        {
            if (_currentMajorType is null)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_IsAtRootContext);
            }

            SkipToAncestor(1, disableConformanceLevelChecks);
        }

        private void SkipToAncestor(int depth, bool disableConformanceLevelChecks)
        {
            Debug.Assert(0 <= depth && depth <= CurrentDepth);
            Checkpoint checkpoint = CreateCheckpoint();
            _isConformanceLevelCheckEnabled = !disableConformanceLevelChecks;

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
            while ((state = PeekStateCore(throwOnFormatErrors: true)) == CborReaderState.Tag)
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
                    ReadStartByteString();
                    depth++;
                    break;

                case CborReaderState.EndByteString:
                    ValidatePop(state, depth);
                    ReadEndByteString();
                    depth--;
                    break;

                case CborReaderState.StartTextString:
                    ReadStartTextString();
                    depth++;
                    break;

                case CborReaderState.EndTextString:
                    ValidatePop(state, depth);
                    ReadEndTextString();
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
                case CborReaderState.SimpleValue:
                    ReadSimpleValue();
                    break;

                case CborReaderState.EndOfData:
                    throw new FormatException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
                case CborReaderState.FormatError:
                    Debug.Fail("Peek format errors should be surfaced as FormatExceptions.");
                    throw new FormatException();

                default:
                    throw new InvalidOperationException(SR.Format(SR.Cbor_Reader_Skip_InvalidState, state));
            }

            // guards against cases where the caller attempts to skip when reader is not positioned at the start of a value
            static void ValidatePop(CborReaderState state, int depth)
            {
                if (depth == 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.Cbor_Reader_Skip_InvalidState, state));
                }
            }
        }
    }
}
