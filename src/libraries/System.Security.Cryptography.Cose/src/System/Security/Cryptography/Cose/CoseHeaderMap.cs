// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Cbor;
using System.Runtime.Versioning;

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseHeaderMap : IEnumerable<(CoseHeaderLabel Label, ReadOnlyMemory<byte> EncodedValue)>
    {
        private static readonly byte[] s_emptyBstrEncoded = new byte[] { 0x40 };
        private static readonly CoseHeaderMap s_emptyMap = new CoseHeaderMap(isReadOnly: true);

        public bool IsReadOnly { get; internal set; }

        private readonly Dictionary<CoseHeaderLabel, ReadOnlyMemory<byte>> _headerParameters = new Dictionary<CoseHeaderLabel, ReadOnlyMemory<byte>>();

        public CoseHeaderMap() : this (isReadOnly: false) { }

        internal CoseHeaderMap(bool isReadOnly)
        {
            IsReadOnly = isReadOnly;
        }

        public bool TryGetEncodedValue(CoseHeaderLabel label, out ReadOnlyMemory<byte> encodedValue)
            => _headerParameters.TryGetValue(label, out encodedValue);

        public ReadOnlyMemory<byte> GetEncodedValue(CoseHeaderLabel label)
        {
            if (TryGetEncodedValue(label, out ReadOnlyMemory<byte> encodedValue))
            {
                return encodedValue;
            }

            throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapLabelDoeNotExist, label.LabelName));
        }

        public int GetValueAsInt32(CoseHeaderLabel label)
        {
            var reader = new CborReader(GetEncodedValue(label));
            int retVal = reader.ReadInt32();
            Debug.Assert(reader.BytesRemaining == 0);
            return retVal;
        }

        public string GetValueAsString(CoseHeaderLabel label)
        {
            var reader = new CborReader(GetEncodedValue(label));
            string retVal = reader.ReadTextString();
            Debug.Assert(reader.BytesRemaining == 0);
            return retVal;
        }

        public ReadOnlySpan<byte> GetValueAsBytes(CoseHeaderLabel label)
        {
            var reader = new CborReader(GetEncodedValue(label));
            ReadOnlySpan<byte> retVal = reader.ReadByteString();
            Debug.Assert(reader.BytesRemaining == 0);
            return retVal;
        }

        public void SetEncodedValue(CoseHeaderLabel label, ReadOnlySpan<byte> encodedValue)
            => SetEncodedValue(label, new ReadOnlyMemory<byte>(encodedValue.ToArray()));

        internal void SetEncodedValue(CoseHeaderLabel label, ReadOnlyMemory<byte> encodedValue)
        {
            ValidateIsReadOnly();
            ValidateHeaderValue(label, null, encodedValue);

            _headerParameters[label] = encodedValue;
        }

        public void SetValue(CoseHeaderLabel label, int value)
        {
            ValidateIsReadOnly();
            ValidateHeaderValue(label, value < 0 ? CborReaderState.NegativeInteger : CborReaderState.UnsignedInteger, null);

            var writer = new CborWriter();
            writer.WriteInt32(value);
            _headerParameters[label] = writer.Encode();
        }

        public void SetValue(CoseHeaderLabel label, string value)
        {
            ValidateIsReadOnly();
            ValidateHeaderValue(label, CborReaderState.TextString, null);

            var writer = new CborWriter();
            writer.WriteTextString(value);
            _headerParameters[label] = writer.Encode();
        }

        public void SetValue(CoseHeaderLabel label, ReadOnlySpan<byte> value)
        {
            ValidateIsReadOnly();
            ValidateHeaderValue(label, CborReaderState.ByteString, null);

            var writer = new CborWriter();
            writer.WriteByteString(value);
            _headerParameters[label] = writer.Encode();
        }

        public void Remove(CoseHeaderLabel label)
        {
            ValidateIsReadOnly();
            _headerParameters.Remove(label);
        }

        private void ValidateIsReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(SR.CoseHeaderMapDecodedMapIsReadOnlyCannotSetValue);
            }
        }

        private void ValidateHeaderValue(CoseHeaderLabel label, CborReaderState? state, ReadOnlyMemory<byte>? encodedValue)
        {
            if (state != null)
            {
                Debug.Assert(encodedValue == null);
                if (label.LabelAsString == null)
                {
                    // all known headers are integers.
                    ValidateKnownHeaderValue(label.LabelAsInt32, state, null);
                }
            }
            else
            {
                Debug.Assert(encodedValue != null);
                var reader = new CborReader(encodedValue.Value);

                if (label.LabelAsString == null)
                {
                    // all known headers are integers.
                    ValidateKnownHeaderValue(label.LabelAsInt32, reader.PeekState(), reader);
                }
                else
                {
                    reader.SkipValue();
                }

                if (reader.BytesRemaining != 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapCborEncodedValueNotValid, label));
                }
            }

            static void ValidateKnownHeaderValue(int label, CborReaderState? initialState, CborReader? reader)
            {
                switch (label)
                {
                    case KnownHeaders.Alg:
                        if (initialState != CborReaderState.NegativeInteger &&
                            initialState != CborReaderState.UnsignedInteger &&
                            initialState != CborReaderState.TextString)
                        {
                            throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label));
                        }
                        reader?.SkipValue();
                        break;
                    case KnownHeaders.Crit:
                        reader?.SkipValue(); // TODO
                        break;
                    case KnownHeaders.CounterSignature:
                        reader?.SkipValue(); // TODO
                        break;
                    case KnownHeaders.ContentType:
                        if (initialState != CborReaderState.TextString &&
                            initialState != CborReaderState.UnsignedInteger)
                        {
                            throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label));
                        }
                        reader?.SkipValue();
                        break;
                    case KnownHeaders.Kid:
                    case KnownHeaders.IV:
                    case KnownHeaders.PartialIV:
                        if (initialState != CborReaderState.ByteString)
                        {
                            throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label));
                        }
                        reader?.SkipValue();
                        break;
                    default:
                        reader?.SkipValue();
                        break;
                }
            }
        }

        internal static byte[] Encode(CoseHeaderMap? map, bool mustReturnEmptyBstrIfEmpty = false, int? algHeaderValueToSlip = null)
        {
            map ??= s_emptyMap;
            bool shouldSlipAlgHeader = algHeaderValueToSlip.HasValue;

            if (map._headerParameters.Count == 0 && mustReturnEmptyBstrIfEmpty && !shouldSlipAlgHeader)
            {
                return s_emptyBstrEncoded;
            }

            int mapLength = map._headerParameters.Count;
            if (shouldSlipAlgHeader)
            {
                mapLength++;
            }

            var writer = new CborWriter();
            writer.WriteStartMap(mapLength);

            if (shouldSlipAlgHeader)
            {
                Debug.Assert(!map.TryGetEncodedValue(CoseHeaderLabel.Algorithm, out _));
                writer.WriteInt32(KnownHeaders.Alg);
                writer.WriteInt32(algHeaderValueToSlip!.Value);
            }

            foreach ((CoseHeaderLabel label, ReadOnlyMemory<byte> encodedValue) in map)
            {
                if (label.LabelAsString == null)
                {
                    writer.WriteInt32(label.LabelAsInt32);
                }
                else
                {
                    writer.WriteTextString(label.LabelAsString);
                }
                writer.WriteEncodedValue(encodedValue.Span);
            }
            writer.WriteEndMap();
            return writer.Encode();
        }

        public Enumerator GetEnumerator() => new Enumerator(_headerParameters.GetEnumerator());
        IEnumerator<(CoseHeaderLabel Label, ReadOnlyMemory<byte> EncodedValue)> IEnumerable<(CoseHeaderLabel Label, ReadOnlyMemory<byte> EncodedValue)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<(CoseHeaderLabel Label, ReadOnlyMemory<byte> EncodedValue)>
        {
            private Dictionary<CoseHeaderLabel, ReadOnlyMemory<byte>>.Enumerator _dictionaryEnumerator;

            internal Enumerator(Dictionary<CoseHeaderLabel, ReadOnlyMemory<byte>>.Enumerator dictionaryEnumerator)
            {
                _dictionaryEnumerator = dictionaryEnumerator;
            }

            public readonly (CoseHeaderLabel Label, ReadOnlyMemory<byte> EncodedValue) Current => (_dictionaryEnumerator.Current.Key, _dictionaryEnumerator.Current.Value);

            object IEnumerator.Current => Current;

            public void Dispose() => _dictionaryEnumerator.Dispose();
            public bool MoveNext() => _dictionaryEnumerator.MoveNext();
            public void Reset() => ((IEnumerator)_dictionaryEnumerator).Reset();
        }
    }
}
