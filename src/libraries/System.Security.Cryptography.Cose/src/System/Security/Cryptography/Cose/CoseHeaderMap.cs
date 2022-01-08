// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Cbor;

namespace System.Security.Cryptography.Cose
{
    public class CoseHeaderMap : IEnumerable<(CoseHeaderLabel, ReadOnlyMemory<byte>)>
    {
        private static readonly byte[] s_emptyBstrEncoded = new byte[] { 0x40 };
        private bool _isReadOnly;
        public bool IsReadOnly { get => _isReadOnly; internal set => _isReadOnly = value; }
        private readonly Dictionary<CoseHeaderLabel, ReadOnlyMemory<byte>> _headerParameters = new Dictionary<CoseHeaderLabel, ReadOnlyMemory<byte>>();

        public CoseHeaderMap() : this (isReadOnly: false) { }

        internal CoseHeaderMap(bool isReadOnly)
        {
            _isReadOnly = isReadOnly;
        }

        public bool TryGetEncodedValue(CoseHeaderLabel label, out ReadOnlyMemory<byte> encodedValue)
            => _headerParameters.TryGetValue(label, out encodedValue);

        public ReadOnlyMemory<byte> GetEncodedValue(CoseHeaderLabel label)
            => _headerParameters[label];

        public int GetValueAsInt32(CoseHeaderLabel label)
        {
            var reader = new CborReader(_headerParameters[label]);
            int retVal = reader.ReadInt32();
            Debug.Assert(reader.BytesRemaining == 0);
            return retVal;
        }

        public string GetValueAsString(CoseHeaderLabel label)
        {
            var reader = new CborReader(_headerParameters[label]);
            string retVal = reader.ReadTextString();
            Debug.Assert(reader.BytesRemaining == 0);
            return retVal;
        }

        public ReadOnlySpan<byte> GetValueAsBytes(CoseHeaderLabel label)
        {
            var reader = new CborReader(_headerParameters[label]);
            ReadOnlySpan<byte> retVal = reader.ReadByteString();
            Debug.Assert(reader.BytesRemaining == 0);
            return retVal;
        }

        public void SetEncodedValue(CoseHeaderLabel label, ReadOnlyMemory<byte> encodedValue)
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
            if (_isReadOnly)
            {
                throw new InvalidOperationException(SR.CoseHeaderMapDecodedMapIsReadOnlyCannotSetValue);
            }
        }

        private void ValidateHeaderValue(CoseHeaderLabel label, CborReaderState? state, ReadOnlyMemory<byte>? encodedValue)
        {
            if (state != null)
            {
                Debug.Assert(encodedValue == null);
                if (label.LabelAsInt32 != null)
                {
                    Debug.Assert(label.LabelAsString == null);
                    // all known headers are integers.
                    ValidateKnownHeaderValue(label.LabelAsInt32.Value, state, null);
                }
            }
            else
            {
                Debug.Assert(encodedValue != null);
                var reader = new CborReader(encodedValue.Value);

                if (label.LabelAsInt32 != null)
                {
                    Debug.Assert(label.LabelAsString == null);
                    // all known headers are integers.
                    ValidateKnownHeaderValue(label.LabelAsInt32.Value, reader.PeekState(), reader);
                }
                else
                {
                    Debug.Assert(label.LabelAsString != null);
                    reader.SkipValue();
                }

                if (reader.BytesRemaining != 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label));
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
                        throw new NotSupportedException(); // TODO
                        //if (reader == null)
                        //{
                        //    throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label));
                        //}

                        //int? length = reader.ReadStartArray();
                        //if (length.GetValueOrDefault() < 1)
                        //{
                        //    throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label));
                        //}

                        //for (int i = 0; i < length; i++)
                        //{
                        //    CborReaderState state = reader.PeekState();
                        //    if (state != CborReaderState.NegativeInteger &&
                        //        state != CborReaderState.UnsignedInteger &&
                        //        state != CborReaderState.TextString)
                        //    {
                        //        throw new InvalidOperationException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label));
                        //    }
                        //    reader.SkipValue();
                        //}
                        //reader.ReadEndArray();
                        //break;
                    case KnownHeaders.CounterSignature:
                        throw new NotSupportedException(); // TODO
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

        internal byte[] Encode(bool mustReturnEmptyBstrIfEmpty = false)
        {
            if (_headerParameters.Count == 0 && mustReturnEmptyBstrIfEmpty)
            {
                return s_emptyBstrEncoded;
            }

            var writer = new CborWriter();
            writer.WriteStartMap(_headerParameters.Count);
            foreach ((CoseHeaderLabel Label, ReadOnlyMemory<byte> EncodedValue) header in this)
            {
                CoseHeaderLabel label = header.Label;
                if (label.LabelAsInt32 != null)
                {
                    writer.WriteInt32(label.LabelAsInt32.Value);
                }
                else
                {
                    Debug.Assert(label.LabelAsString != null);
                    writer.WriteTextString(label.LabelAsString);
                }
                writer.WriteEncodedValue(header.EncodedValue.Span);
            }
            writer.WriteEndMap();
            return writer.Encode();
        }

        public IEnumerator<(CoseHeaderLabel, ReadOnlyMemory<byte>)> GetEnumerator() => new Enumerator(_headerParameters.GetEnumerator());

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal struct Enumerator : IEnumerator<(CoseHeaderLabel, ReadOnlyMemory<byte>)>
        {
            private Dictionary<CoseHeaderLabel, ReadOnlyMemory<byte>>.Enumerator _dictionaryEnumerator;

            internal Enumerator(Dictionary<CoseHeaderLabel, ReadOnlyMemory<byte>>.Enumerator dictionaryEnumerator)
            {
                _dictionaryEnumerator = dictionaryEnumerator;
            }

            public (CoseHeaderLabel, ReadOnlyMemory<byte>) Current => (_dictionaryEnumerator.Current.Key, _dictionaryEnumerator.Current.Value);

            object IEnumerator.Current => Current;

            public void Dispose() => _dictionaryEnumerator.Dispose();
            public bool MoveNext() => _dictionaryEnumerator.MoveNext();
            public void Reset() => ((IEnumerator)_dictionaryEnumerator).Reset();
        }
    }
}
