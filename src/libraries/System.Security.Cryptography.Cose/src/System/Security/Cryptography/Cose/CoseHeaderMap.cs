// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Cbor;

namespace System.Security.Cryptography.Cose
{
    /// <summary>
    /// Represents a collection of header parameters of a COSE message.
    /// </summary>
    public sealed class CoseHeaderMap : IDictionary<CoseHeaderLabel, CoseHeaderValue>, IReadOnlyDictionary<CoseHeaderLabel, CoseHeaderValue>
    {
        private static readonly CoseHeaderMap s_emptyMap = new CoseHeaderMap(isReadOnly: true);

        private readonly Dictionary<CoseHeaderLabel, CoseHeaderValue> _headerParameters = new Dictionary<CoseHeaderLabel, CoseHeaderValue>();

        /// <summary>
        /// Gets a value that indicates whether the header map is read-only.
        /// </summary>
        /// <value><see langword="true"/> if the header map is read-only; otherwise, <see langword="false"/></value>
        /// <remarks>The "protected headers" collection in a COSE message is always read-only.</remarks>
        public bool IsReadOnly { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoseHeaderMap"/> class.
        /// </summary>
        public CoseHeaderMap() : this(isReadOnly: false) { }

        private CoseHeaderMap(bool isReadOnly)
        {
            IsReadOnly = isReadOnly;
        }

        private ICollection<KeyValuePair<CoseHeaderLabel, CoseHeaderValue>> HeaderParametersAsCollection => _headerParameters;

        /// <summary>
        /// Gets a collection containing the labels in the header map.
        /// </summary>
        /// <value>A collection containing the labels in the header map.</value>
        public ICollection<CoseHeaderLabel> Keys => _headerParameters.Keys;

        /// <summary>
        /// Gets a collection containing the values in the header map.
        /// </summary>
        /// <value>A collection containing the values in the header map.</value>
        public ICollection<CoseHeaderValue> Values => _headerParameters.Values;

        /// <summary>
        /// Gets the number of label/value pairs contained in the header map.
        /// </summary>
        /// <value>The number of label/value pairs contained in the header map.</value>
        public int Count => _headerParameters.Count;

        /// <inheritdoc/>
        IEnumerable<CoseHeaderLabel> IReadOnlyDictionary<CoseHeaderLabel, CoseHeaderValue>.Keys => _headerParameters.Keys;

        /// <inheritdoc/>
        IEnumerable<CoseHeaderValue> IReadOnlyDictionary<CoseHeaderLabel, CoseHeaderValue>.Values => _headerParameters.Values;

        /// <summary>
        /// Gets or sets the value associated with the specified label.
        /// </summary>
        /// <param name="key">The label of the value to get or set.</param>
        /// <value>The value associated with the specified label.</value>
        /// <exception cref="InvalidOperationException">The property is set and the <see cref="CoseHeaderMap"/> is read-only.</exception>
        /// <exception cref="ArgumentException">The property is set and the encoded bytes in the specified <see cref="CoseHeaderValue"/> contain trailing data or more than one CBOR value.</exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> is not found.</exception>
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

        /// <summary>
        /// Gets the value associated with the specified label, as a signed integer.
        /// </summary>
        /// <param name="label">The label of the value to get.</param>
        /// <returns>The value associated with the specified label, as a signed integer.</returns>
        /// <exception cref="InvalidOperationException">The value could not be decoded as a 32-bit signed integer.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="label"/> is not found.</exception>
        public int GetValueAsInt32(CoseHeaderLabel label) => _headerParameters[label].GetValueAsInt32();

        /// <summary>
        /// Gets the value associated with the specified label, as a text string.
        /// </summary>
        /// <param name="label">The label of the value to get.</param>
        /// <returns>The value associated with the specified label, as a text string.</returns>
        /// <exception cref="InvalidOperationException">The value could not be decoded as text string.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="label"/> is not found.</exception>
        public string GetValueAsString(CoseHeaderLabel label) => _headerParameters[label].GetValueAsString();

        /// <summary>
        /// Gets the value associated with the specified label, as a byte string.
        /// </summary>
        /// <param name="label">The label of the value to get.</param>
        /// <returns>The value associated with the specified label, as a byte string.</returns>
        /// <exception cref="InvalidOperationException">The value could not be decoded as byte string.</exception>
        public byte[] GetValueAsBytes(CoseHeaderLabel label) => _headerParameters[label].GetValueAsBytes();

        /// <summary>
        /// Gets the value associated with the specified label, as a byte string.
        /// </summary>
        /// <param name="label">The label of the value to get.</param>
        /// <param name="destination">The buffer in which to write the value.</param>
        /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is too small to hold the value.</exception>
        /// <exception cref="InvalidOperationException">The value could not be decoded as byte string.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="label"/> is not found.</exception>
        public int GetValueAsBytes(CoseHeaderLabel label, Span<byte> destination) => _headerParameters[label].GetValueAsBytes(destination);

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="InvalidOperationException">The header map is read-only.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid CBOR value.</exception>
        public void Add(CoseHeaderLabel key, CoseHeaderValue value)
        {
            ValidateIsReadOnly();
            ValidateInsertion(key, value);
            _headerParameters.Add(key, value);
        }

        /// <summary>
        /// Adds the specified value to the header map with the specified key.
        /// </summary>
        /// <param name="item">The label (key) and value to add to the header map.</param>
        /// <exception cref="InvalidOperationException">The header map is read-only.</exception>
        /// <exception cref="ArgumentException"><paramref name="item"/>'s value is not a valid CBOR value.</exception>
        public void Add(KeyValuePair<CoseHeaderLabel, CoseHeaderValue> item) => Add(item.Key, item.Value);

        /// <summary>
        /// Adds the specified label and value to the header map.
        /// </summary>
        /// <param name="label">The label for the header to add.</param>
        /// <param name="value">The value of the header to add.</param>
        /// <exception cref="InvalidOperationException">The header map is read-only.</exception>
        public void Add(CoseHeaderLabel label, int value) => Add(label, CoseHeaderValue.FromInt32(value));

        /// <summary>
        /// Adds the specified label and value to the header map.
        /// </summary>
        /// <param name="label">The label for the header to add.</param>
        /// <param name="value">The value of the header to add.</param>
        /// <exception cref="InvalidOperationException">The header map is read-only.</exception>
        public void Add(CoseHeaderLabel label, string value) => Add(label, CoseHeaderValue.FromString(value));

        /// <summary>
        /// Adds the specified label and value to the header map.
        /// </summary>
        /// <param name="label">The label for the header to add.</param>
        /// <param name="value">The value of the header to add.</param>
        /// <exception cref="InvalidOperationException">The header map is read-only.</exception>
        /// <remarks>
        /// <paramref name="value"/> does not need to contain a valid CBOR-encoded value, as it will be encoded as a CBOR byte string.
        /// To specify a CBOR-encoded value directly, see <see cref="CoseHeaderValue.FromEncodedValue(ReadOnlySpan{byte})"/> and <see cref="Add(CoseHeaderLabel, CoseHeaderValue)"/>.
        /// </remarks>
        public void Add(CoseHeaderLabel label, byte[] value) => Add(label, CoseHeaderValue.FromBytes(value));

        /// <summary>
        /// Adds the specified label and value to the header map.
        /// </summary>
        /// <param name="label">The label for the header to add.</param>
        /// <param name="value">The value of the header to add.</param>
        /// <exception cref="InvalidOperationException">The header map is read-only.</exception>
        /// <remarks>
        /// <paramref name="value"/> does not need to contain a valid CBOR-encoded value, as it will be encoded as a CBOR byte string.
        /// To specify a CBOR-encoded value directly, see <see cref="CoseHeaderValue.FromEncodedValue(ReadOnlySpan{byte})"/> and <see cref="Add(CoseHeaderLabel, CoseHeaderValue)"/>.
        /// </remarks>
        public void Add(CoseHeaderLabel label, ReadOnlySpan<byte> value) => Add(label, CoseHeaderValue.FromBytes(value));

        /// <inheritdoc/>
        public bool ContainsKey(CoseHeaderLabel key) => _headerParameters.ContainsKey(key);

        /// <inheritdoc/>
        public bool TryGetValue(CoseHeaderLabel key, out CoseHeaderValue value) => _headerParameters.TryGetValue(key, out value);

        /// <summary>
        /// Removes all labels and values from the header map.
        /// </summary>
        /// <exception cref="InvalidOperationException">The header map is read-only.</exception>
        public void Clear()
        {
            ValidateIsReadOnly();
            _headerParameters.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<CoseHeaderLabel, CoseHeaderValue> item)
            => HeaderParametersAsCollection.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<CoseHeaderLabel, CoseHeaderValue>[] array, int arrayIndex)
            => HeaderParametersAsCollection.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<CoseHeaderLabel, CoseHeaderValue>> GetEnumerator()
            => _headerParameters.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
            => _headerParameters.GetEnumerator();

        /// <summary>
        /// Removes the value with the specified label from the header map.
        /// </summary>
        /// <param name="label">The label of the element to remove.</param>
        /// <returns><see langword="true"/> if <paramref name="label"/> was found in the map; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">The header map is read-only.</exception>
        public bool Remove(CoseHeaderLabel label)
        {
            ValidateIsReadOnly();
            return _headerParameters.Remove(label);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the header map.
        /// </summary>
        /// <param name="item">The object to remove from the map.</param>
        /// <returns><see langword="true"/> if the key and value represented by <paramref name="item"/> are successfully found in the map; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">The header map is read-only.</exception>
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

        private static void ValidateInsertion(CoseHeaderLabel label, CoseHeaderValue value)
        {
            var reader = new CborReader(value.EncodedValue);
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
                                throw new ArgumentException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label.LabelName), nameof(value));
                            }
                            reader.SkipValue();
                            break;
                        case KnownHeaders.Crit:
                            reader.ReadStartArray();
                            bool isEmpty = true;

                            while (true)
                            {
                                CborReaderState state = reader.PeekState();
                                if (state == CborReaderState.EndArray)
                                {
                                    reader.ReadEndArray();
                                    break;
                                }
                                else if (state == CborReaderState.UnsignedInteger || state == CborReaderState.NegativeInteger)
                                {
                                    reader.ReadInt32();
                                }
                                else if (state == CborReaderState.TextString)
                                {
                                    reader.ReadTextString();
                                }
                                else
                                {
                                    throw new ArgumentException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label.LabelName), nameof(value));
                                }
                                isEmpty = false;
                            }

                            if (isEmpty)
                            {
                                throw new ArgumentException(SR.CriticalHeadersMustBeArrayOfAtLeastOne, nameof(value));
                            }
                            break;
                        case KnownHeaders.ContentType:
                            if (initialState != CborReaderState.TextString &&
                                initialState != CborReaderState.UnsignedInteger)
                            {
                                throw new ArgumentException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label.LabelName), nameof(value));
                            }
                            reader.SkipValue();
                            break;
                        case KnownHeaders.Kid:
                            if (initialState != CborReaderState.ByteString)
                            {
                                throw new ArgumentException(SR.Format(SR.CoseHeaderMapHeaderDoesNotAcceptSpecifiedValue, label.LabelName), nameof(value));
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
                throw new ArgumentException(SR.Format(SR.CoseHeaderMapArgumentCoseHeaderValueIncorrect, label.LabelName), nameof(value), ex);
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
