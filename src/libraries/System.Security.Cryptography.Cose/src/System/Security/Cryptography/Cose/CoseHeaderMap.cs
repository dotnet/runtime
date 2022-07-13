// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;

namespace System.Security.Cryptography.Cose
{
    public sealed class CoseHeaderMap : IDictionary<CoseHeaderLabel, CoseHeaderValue>, IReadOnlyDictionary<CoseHeaderLabel, CoseHeaderValue>
    {
        private static readonly CoseHeaderMap s_emptyMap = new CoseHeaderMap(isReadOnly: true);

        private readonly Dictionary<CoseHeaderLabel, CoseHeaderValue> _headerParameters = new Dictionary<CoseHeaderLabel, CoseHeaderValue>();

        public bool IsReadOnly { get; internal set; }

        public CoseHeaderMap() : this(isReadOnly: false) { }

        private CoseHeaderMap(bool isReadOnly)
        {
            IsReadOnly = isReadOnly;
        }

        private ICollection<KeyValuePair<CoseHeaderLabel, CoseHeaderValue>> HeaderParametersAsCollection => _headerParameters;

        public ICollection<CoseHeaderLabel> Keys => _headerParameters.Keys;

        public ICollection<CoseHeaderValue> Values => _headerParameters.Values;

        public int Count => _headerParameters.Count;

        IEnumerable<CoseHeaderLabel> IReadOnlyDictionary<CoseHeaderLabel, CoseHeaderValue>.Keys => _headerParameters.Keys;

        IEnumerable<CoseHeaderValue> IReadOnlyDictionary<CoseHeaderLabel, CoseHeaderValue>.Values => _headerParameters.Values;

        public CoseHeaderValue this[CoseHeaderLabel key]
        {
            get => _headerParameters[key];
            set
            {
                ValidateIsReadOnly();
                ValidateInsertion(key, value);
                _headerParameters[key] = value;
            }
        }

        public int GetValueAsInt32(CoseHeaderLabel label) => _headerParameters[label].GetValueAsInt32();

        public string GetValueAsString(CoseHeaderLabel label) => _headerParameters[label].GetValueAsString();

        public byte[] GetValueAsBytes(CoseHeaderLabel label) => _headerParameters[label].GetValueAsBytes();

        public int GetValueAsBytes(CoseHeaderLabel label, Span<byte> destination) => _headerParameters[label].GetValueAsBytes(destination);

        public void Add(CoseHeaderLabel key, CoseHeaderValue value)
        {
            ValidateIsReadOnly();
            ValidateInsertion(key, value);
            _headerParameters.Add(key, value);
        }

        public void Add(KeyValuePair<CoseHeaderLabel, CoseHeaderValue> item) => Add(item.Key, item.Value);

        public void Add(CoseHeaderLabel label, int value) => Add(label, CoseHeaderValue.FromInt32(value));

        public void Add(CoseHeaderLabel label, string value) => Add(label, CoseHeaderValue.FromString(value));

        public void Add(CoseHeaderLabel label, byte[] value) => Add(label, CoseHeaderValue.FromBytes(value));

        public void Add(CoseHeaderLabel label, ReadOnlySpan<byte> value) => Add(label, CoseHeaderValue.FromBytes(value));

        public bool ContainsKey(CoseHeaderLabel key) => _headerParameters.ContainsKey(key);

        public bool TryGetValue(CoseHeaderLabel key, out CoseHeaderValue value) => _headerParameters.TryGetValue(key, out value);

        public void Clear()
        {
            ValidateIsReadOnly();
            _headerParameters.Clear();
        }

        public bool Contains(KeyValuePair<CoseHeaderLabel, CoseHeaderValue> item)
            => HeaderParametersAsCollection.Contains(item);

        public void CopyTo(KeyValuePair<CoseHeaderLabel, CoseHeaderValue>[] array, int arrayIndex)
            => HeaderParametersAsCollection.CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<CoseHeaderLabel, CoseHeaderValue>> GetEnumerator()
            => _headerParameters.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _headerParameters.GetEnumerator();

        public bool Remove(CoseHeaderLabel label)
        {
            ValidateIsReadOnly();
            return _headerParameters.Remove(label);
        }

        public bool Remove(KeyValuePair<CoseHeaderLabel, CoseHeaderValue> item)
        {
            ValidateIsReadOnly();
            return HeaderParametersAsCollection.Remove(item);
        }

        private void ValidateIsReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(SR.CoseHeaderMapDecodedMapIsReadOnlyCannotSetValue);
            }
        }

        private static void ValidateInsertion(CoseHeaderLabel label, CoseHeaderValue headerValue)
        {
            var reader = new CborReader(headerValue.EncodedValue);
            try
            {
                if (label.LabelAsString != null) // all known headers are integers.
                {
                    reader.SkipValue();
                }
                else
                {
                    CborReaderState initialState = reader.PeekState();
                    switch (label.LabelAsInt32)
                    {
                        case KnownHeaders.Alg:
                            if (initialState != CborReaderState.NegativeInteger &&
                                initialState != CborReaderState.UnsignedInteger &&
                                initialState != CborReaderState.TextString)
                            {
                                throw new ArgumentException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label.LabelName));
                            }
                            reader.SkipValue();
                            break;
                        case KnownHeaders.Crit:
                            int length = reader.ReadStartArray().GetValueOrDefault();
                            if (length < 1)
                            {
                                throw new ArgumentException(SR.CriticalHeadersMustBeArrayOfAtLeastOne);
                            }

                            for (int i = 0; i < length; i++)
                            {
                                CborReaderState state = reader.PeekState();
                                if (state == CborReaderState.UnsignedInteger || state == CborReaderState.NegativeInteger)
                                {
                                    reader.ReadInt32();
                                }
                                else if (state == CborReaderState.TextString)
                                {
                                    reader.ReadTextString();
                                }
                                else
                                {
                                    throw new ArgumentException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label.LabelName));
                                }
                            }
                            reader.SkipToParent();
                            break;
                        case KnownHeaders.ContentType:
                            if (initialState != CborReaderState.TextString &&
                                initialState != CborReaderState.UnsignedInteger)
                            {
                                throw new ArgumentException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label.LabelName));
                            }
                            reader.SkipValue();
                            break;
                        case KnownHeaders.Kid:
                            if (initialState != CborReaderState.ByteString)
                            {
                                throw new ArgumentException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label.LabelName));
                            }
                            reader.SkipValue();
                            break;
                        default:
                            reader.SkipValue();
                            break;
                    }
                }

                if (reader.BytesRemaining != 0)
                {
                    throw new CborContentException(SR.CoseHeaderMapCborEncodedValueNotValid);
                }
            }
            catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
            {
                throw new ArgumentException(SR.Format(SR.CoseHeaderMapArgumentCoseHeaderValueIncorrect, label.LabelName), ex);
            }
        }

        internal static int Encode(CoseHeaderMap? map, Span<byte> destination, bool isProtected = false, int? algHeaderValueToSlip = null)
        {
            map ??= s_emptyMap;
            bool shouldSlipAlgHeader = algHeaderValueToSlip.HasValue;

            if (map._headerParameters.Count == 0 && isProtected && !shouldSlipAlgHeader)
            {
                return 0;
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
                Debug.Assert(!map.ContainsKey(CoseHeaderLabel.Algorithm));
                writer.WriteInt32(KnownHeaders.Alg);
                writer.WriteInt32(algHeaderValueToSlip!.Value);
            }

            foreach (KeyValuePair<CoseHeaderLabel, CoseHeaderValue> kvp in map)
            {
                CoseHeaderLabel label = kvp.Key;
                CoseHeaderValue value = kvp.Value;

                if (label.LabelAsString == null)
                {
                    writer.WriteInt32(label.LabelAsInt32);
                }
                else
                {
                    writer.WriteTextString(label.LabelAsString);
                }

                writer.WriteEncodedValue(value.EncodedValue.Span);
            }
            writer.WriteEndMap();

            int bytesWritten = writer.Encode(destination);
            Debug.Assert(bytesWritten == ComputeEncodedSize(map, algHeaderValueToSlip));

            return bytesWritten;
        }

        internal static int ComputeEncodedSize(CoseHeaderMap? map, int? algHeaderValueToSlip = null)
        {
            map ??= s_emptyMap;

            // encoded map length => map length + (label + value)*
            int encodedSize = 0;
            int mapLength = map._headerParameters.Count;

            if (algHeaderValueToSlip != null)
            {
                mapLength += 1;
                encodedSize += CoseHeaderLabel.Algorithm.EncodedSize;
                encodedSize += CoseHelpers.GetIntegerEncodedSize(algHeaderValueToSlip.Value);
            }

            encodedSize += CoseHelpers.GetIntegerEncodedSize(mapLength);

            foreach (KeyValuePair<CoseHeaderLabel, CoseHeaderValue> kvp in map)
            {
                CoseHeaderLabel label = kvp.Key;
                CoseHeaderValue value = kvp.Value;

                encodedSize += label.EncodedSize + value.EncodedValue.Length;
            }

            return encodedSize;
        }
    }
}
