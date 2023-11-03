// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// A lightweight abstraction for a payload of bytes that supports converting between string, stream, JSON, and bytes.
    /// </summary>
    [JsonConverter(typeof(BinaryDataJsonConverter))]
    public class BinaryData
    {
        private const string JsonSerializerRequiresDynamicCode = "JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation.";
        private const string JsonSerializerRequiresUnreferencedCode = "JSON serialization and deserialization might require types that cannot be statically analyzed.";
        private const string MediaTypeApplicationJson = "application/json";

        /// <summary>
        /// The backing store for the <see cref="BinaryData"/> instance.
        /// </summary>
        private readonly ReadOnlyMemory<byte> _bytes;

        /// <summary>
        /// Returns an empty BinaryData.
        /// </summary>
        public static BinaryData Empty { get; } = new BinaryData(ReadOnlyMemory<byte>.Empty);

        /// <summary>
        /// Gets the number of bytes of this data.
        /// </summary>
        /// <returns>The number of bytes of this data.</returns>
        public int Length => _bytes.Length;

        /// <summary>
        /// Gets a value that indicates whether this data is empty.
        /// </summary>
        /// <returns><see langword="true" /> if the data is empty (that is, its <see cref="Length" /> is 0); otherwise, <see langword="false" />.</returns>
        public bool IsEmpty => _bytes.IsEmpty;

        /// <summary>
        /// Gets the MIME type of this data, e.g. <see cref="MediaTypeNames.Application.Octet"/>.
        /// </summary>
        /// <seealso cref="MediaTypeNames"/>
        public string? MediaType { get; }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by wrapping the
        /// provided byte array.
        /// </summary>
        /// <param name="data">The array to wrap.</param>
        public BinaryData(byte[] data)
        {
            _bytes = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by wrapping the provided byte array
        /// and sets <see cref="MediaType"/> to <see pref="mediaType"/> value.
        /// </summary>
        /// <param name="data">The array to wrap.</param>
        /// <param name="mediaType">MIME type of this data, e.g. <see cref="MediaTypeNames.Application.Octet"/>.</param>
        /// <seealso cref="MediaTypeNames"/>
        public BinaryData(byte[] data, string? mediaType) : this(data)
        {
            MediaType = mediaType;
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by serializing the provided object to JSON
        /// using <see cref="JsonSerializer"/>.
        /// and sets <see cref="MediaType"/> to "application/json".
        /// </summary>
        /// <param name="jsonSerializable">The object that will be serialized to JSON using
        /// <see cref="JsonSerializer"/>.</param>
        /// <param name="options">The options to use when serializing to JSON.</param>
        /// <param name="type">The type to use when serializing the data. If not specified, <see cref="object.GetType"/> will
        /// be used to determine the type.</param>
        /// <seealso cref="MediaTypeNames"/>
        [RequiresDynamicCode(JsonSerializerRequiresDynamicCode)]
        [RequiresUnreferencedCode(JsonSerializerRequiresUnreferencedCode)]
        public BinaryData(object? jsonSerializable, JsonSerializerOptions? options = default, Type? type = default) : this(
            JsonSerializer.SerializeToUtf8Bytes(jsonSerializable, type ?? jsonSerializable?.GetType() ?? typeof(object), options),
            MediaTypeApplicationJson)
        {
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by serializing the provided object to JSON
        /// using <see cref="JsonSerializer"/>
        /// and sets <see cref="MediaType"/> to "application/json".
        /// </summary>
        /// <param name="jsonSerializable">The object that will be serialized to JSON using
        /// <see cref="JsonSerializer"/>.</param>
        /// <param name="context">The <see cref="JsonSerializerContext" /> to use when serializing to JSON.</param>
        /// <param name="type">The type to use when serializing the data. If not specified, <see cref="object.GetType"/> will
        /// be used to determine the type.</param>
        /// <seealso cref="MediaTypeNames"/>
        public BinaryData(object? jsonSerializable, JsonSerializerContext context, Type? type = default) : this(
            JsonSerializer.SerializeToUtf8Bytes(jsonSerializable, type ?? jsonSerializable?.GetType() ?? typeof(object), context),
            MediaTypeApplicationJson)
        {
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by wrapping the
        /// provided bytes.
        /// </summary>
        /// <param name="data">Byte data to wrap.</param>
        public BinaryData(ReadOnlyMemory<byte> data)
        {
            _bytes = data;
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by wrapping the provided bytes
        /// and sets <see cref="MediaType"/> to <see pref="mediaType"/> value.
        /// </summary>
        /// <param name="data">Byte data to wrap.</param>
        /// <param name="mediaType">MIME type of this data, e.g. <see cref="MediaTypeNames.Application.Octet"/>.</param>
        /// <seealso cref="MediaTypeNames"/>
        public BinaryData(ReadOnlyMemory<byte> data, string? mediaType) : this(data)
        {
            MediaType = mediaType;
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance from a string by converting
        /// the string to bytes using the UTF-8 encoding.
        /// </summary>
        /// <param name="data">The string data.</param>
        public BinaryData(string data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _bytes = Encoding.UTF8.GetBytes(data);
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance from a string by converting
        /// the string to bytes using the UTF-8 encoding
        /// and sets <see cref="MediaType"/> to <see pref="mediaType"/> value.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <param name="mediaType">MIME type of this data, e.g. <see cref="MediaTypeNames.Application.Octet"/>.</param>
        /// <seealso cref="MediaTypeNames"/>
        public BinaryData(string data, string? mediaType) : this(data)
        {
            MediaType = mediaType;
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by wrapping the provided
        /// <see cref="ReadOnlyMemory{Byte}"/>.
        /// </summary>
        /// <param name="data">Byte data to wrap.</param>
        /// <returns>A wrapper over <paramref name="data"/>.</returns>
        public static BinaryData FromBytes(ReadOnlyMemory<byte> data) => new BinaryData(data);

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by wrapping the provided
        /// <see cref="ReadOnlyMemory{Byte}"/>
        /// and sets <see cref="MediaType"/> to <see pref="mediaType"/> value.
        /// </summary>
        /// <param name="data">Byte data to wrap.</param>
        /// <param name="mediaType">MIME type of this data, e.g. <see cref="MediaTypeNames.Application.Octet"/>.</param>
        /// <returns>A wrapper over <paramref name="data"/>.</returns>
        /// <seealso cref="MediaTypeNames"/>
        public static BinaryData FromBytes(ReadOnlyMemory<byte> data, string? mediaType)
            => new BinaryData(data, mediaType);

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by wrapping the provided
        /// byte array.
        /// </summary>
        /// <param name="data">The array to wrap.</param>
        /// <returns>A wrapper over <paramref name="data"/>.</returns>
        public static BinaryData FromBytes(byte[] data) => new BinaryData(data);

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by wrapping the provided byte array
        /// and sets <see cref="MediaType"/> to <see pref="mediaType"/> value.
        /// </summary>
        /// <param name="data">The array to wrap.</param>
        /// <param name="mediaType">MIME type of this data, e.g. <see cref="MediaTypeNames.Application.Octet"/>.</param>
        /// <returns>A wrapper over <paramref name="data"/>.</returns>
        /// <seealso cref="MediaTypeNames"/>
        public static BinaryData FromBytes(byte[] data, string? mediaType)
            => new BinaryData(data, mediaType);

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance from a string by converting
        /// the string to bytes using the UTF-8 encoding.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <returns>A value representing the UTF-8 encoding of <paramref name="data"/>.</returns>
        public static BinaryData FromString(string data) => new BinaryData(data);

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance from a string by converting
        /// the string to bytes using the UTF-8 encoding
        /// and sets <see cref="MediaType"/> to <see pref="mediaType"/> value.
        /// </summary>
        /// <param name="data">The string data.</param>
        /// <param name="mediaType">MIME type of this data, e.g. <see cref="MediaTypeNames.Text.Plain"/>.</param>
        /// <returns>A value representing the UTF-8 encoding of <paramref name="data"/>.</returns>
        /// <seealso cref="MediaTypeNames"/>
        public static BinaryData FromString(string data, string? mediaType)
            => new BinaryData(data, mediaType);

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance from the specified stream.
        /// The stream is not disposed by this method.
        /// </summary>
        /// <param name="stream">Stream containing the data.</param>
        /// <returns>A value representing all of the data remaining in <paramref name="stream"/>.</returns>
        public static BinaryData FromStream(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return FromStreamAsync(stream, async: false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance from the specified stream
        /// and sets <see cref="MediaType"/> to <see pref="mediaType"/> value.
        /// The stream is not disposed by this method.
        /// </summary>
        /// <param name="stream">Stream containing the data.</param>
        /// <param name="mediaType">MIME type of this data, e.g. <see cref="MediaTypeNames.Application.Octet"/>.</param>
        /// <returns>A value representing all of the data remaining in <paramref name="stream"/>.</returns>
        /// <seealso cref="MediaTypeNames"/>
        public static BinaryData FromStream(Stream stream, string? mediaType)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return FromStreamAsync(stream, async: false, mediaType).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance from the specified stream.
        /// The stream is not disposed by this method.
        /// </summary>
        /// <param name="stream">Stream containing the data.</param>
        /// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
        /// <returns>A value representing all of the data remaining in <paramref name="stream"/>.</returns>
        public static Task<BinaryData> FromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return FromStreamAsync(stream, async: true, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance from the specified stream
        /// and sets <see cref="MediaType"/> to <see pref="mediaType"/> value.
        /// The stream is not disposed by this method.
        /// </summary>
        /// <param name="stream">Stream containing the data.</param>
        /// <param name="mediaType">MIME type of this data, e.g. <see cref="MediaTypeNames.Application.Octet"/>.</param>
        /// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
        /// <returns>A value representing all of the data remaining in <paramref name="stream"/>.</returns>
        /// <seealso cref="MediaTypeNames"/>
        public static Task<BinaryData> FromStreamAsync(Stream stream, string? mediaType,
            CancellationToken cancellationToken = default)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return FromStreamAsync(stream, async: true, mediaType, cancellationToken);
        }

        private static async Task<BinaryData> FromStreamAsync(Stream stream, bool async,
            string? mediaType = default, CancellationToken cancellationToken = default)
        {
            const int CopyToBufferSize = 81920;  // the default used by Stream.CopyToAsync
            int bufferSize = CopyToBufferSize;
            MemoryStream memoryStream;

            if (stream.CanSeek)
            {
                long longLength = stream.Length - stream.Position;
                if (longLength > int.MaxValue || longLength < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(stream), SR.ArgumentOutOfRange_StreamLengthMustBeNonNegativeInt32);
                }

                // choose a minimum valid (non-zero) buffer size.
                bufferSize = longLength == 0 ? 1 : Math.Min((int)longLength, CopyToBufferSize);
                memoryStream = new MemoryStream((int)longLength);
            }
            else
            {
                memoryStream = new MemoryStream();
            }

            using (memoryStream)
            {
                if (async)
                {
                    await stream.CopyToAsync(memoryStream, bufferSize, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    stream.CopyTo(memoryStream, bufferSize);
                }
                return new BinaryData(memoryStream.GetBuffer().AsMemory(0, (int)memoryStream.Position), mediaType);
            }
        }

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by serializing the provided object using
        /// the <see cref="JsonSerializer"/>
        /// and sets <see cref="MediaType"/> to "application/json".
        /// </summary>
        /// <typeparam name="T">The type to use when serializing the data.</typeparam>
        /// <param name="jsonSerializable">The data to use.</param>
        /// <param name="options">The options to use when serializing to JSON.</param>
        /// <returns>A value representing the UTF-8 encoding of the JSON representation of <paramref name="jsonSerializable" />.</returns>
        /// <seealso cref="MediaTypeNames"/>
        [RequiresDynamicCode(JsonSerializerRequiresDynamicCode)]
        [RequiresUnreferencedCode(JsonSerializerRequiresUnreferencedCode)]
        public static BinaryData FromObjectAsJson<T>(T jsonSerializable, JsonSerializerOptions? options = default)
            => new BinaryData(JsonSerializer.SerializeToUtf8Bytes(jsonSerializable, options), MediaTypeApplicationJson);

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by serializing the provided object using
        /// the <see cref="JsonSerializer"/>
        /// and sets <see cref="MediaType"/> to "application/json".
        /// </summary>
        /// <typeparam name="T">The type to use when serializing the data.</typeparam>
        /// <param name="jsonSerializable">The data to use.</param>
        /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> to use when serializing to JSON.</param>
        /// <returns>A value representing the UTF-8 encoding of the JSON representation of <paramref name="jsonSerializable" />.</returns>
        /// <seealso cref="MediaTypeNames"/>
        public static BinaryData FromObjectAsJson<T>(T jsonSerializable, JsonTypeInfo<T> jsonTypeInfo)
            => new BinaryData(JsonSerializer.SerializeToUtf8Bytes(jsonSerializable, jsonTypeInfo), MediaTypeApplicationJson);

        /// <summary>
        /// Creates a <see cref="BinaryData"/> instance by wrapping the same data
        /// and changed <see cref="MediaType"/> to <see pref="mediaType"/> value.
        /// </summary>
        /// <returns>A wrapper over the same data with specified <see cref="MediaType"/>.</returns>
        /// <seealso cref="MediaTypeNames"/>
        public BinaryData WithMediaType(string? mediaType)
            => new BinaryData(_bytes, mediaType);

        /// <summary>
        /// Converts the value of this instance to a string using UTF-8.
        /// </summary>
        /// <remarks>
        /// No special treatment is given to the contents of the data, it is merely decoded as a UTF-8 string.
        /// For a JPEG or other binary file format the string will largely be nonsense with many embedded NUL characters,
        /// and UTF-8 JSON values will look like their file/network representation,
        /// including starting and stopping quotes on a string.
        /// </remarks>
        /// <returns>
        /// A string from the value of this instance, using UTF-8 to decode the bytes.
        /// </returns>
        /// <seealso cref="ToObjectFromJson{String}(JsonSerializerOptions)" />
        public override unsafe string ToString()
        {
            ReadOnlySpan<byte> span = _bytes.Span;

            if (span.IsEmpty)
            {
                return string.Empty;
            }

            fixed (byte* ptr = span)
            {
                return Encoding.UTF8.GetString(ptr, span.Length);
            }
        }

        /// <summary>
        /// Converts the <see cref="BinaryData"/> to a read-only stream.
        /// </summary>
        /// <returns>A stream representing the data.</returns>
        public Stream ToStream() => new ReadOnlyMemoryStream(_bytes);

        /// <summary>
        /// Gets the value of this instance as bytes without any further interpretation.
        /// </summary>
        /// <returns>The value of this instance as bytes without any further interpretation.</returns>
        public ReadOnlyMemory<byte> ToMemory() => _bytes;

        /// <summary>
        /// Converts the <see cref="BinaryData"/> to a byte array.
        /// </summary>
        /// <returns>A byte array representing the data.</returns>
        public byte[] ToArray() => _bytes.ToArray();

        /// <summary>
        /// Converts the <see cref="BinaryData"/> to the specified type using
        /// <see cref="JsonSerializer"/>.
        /// </summary>
        /// <typeparam name="T">The type that the data should be
        /// converted to.</typeparam>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use when serializing to JSON.</param>
        /// <returns>The data converted to the specified type.</returns>
        [RequiresDynamicCode(JsonSerializerRequiresDynamicCode)]
        [RequiresUnreferencedCode(JsonSerializerRequiresUnreferencedCode)]
        public T? ToObjectFromJson<T>(JsonSerializerOptions? options = default)
            => JsonSerializer.Deserialize<T>(GetBytesWithTrimmedBom(), options);

        /// <summary>
        /// Converts the <see cref="BinaryData"/> to the specified type using
        /// <see cref="JsonSerializer"/>.
        /// </summary>
        /// <typeparam name="T">The type that the data should be
        /// converted to.</typeparam>
        /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> to use when serializing to JSON.</param>
        /// <returns>The data converted to the specified type.</returns>
        public T? ToObjectFromJson<T>(JsonTypeInfo<T> jsonTypeInfo)
            => JsonSerializer.Deserialize(GetBytesWithTrimmedBom(), jsonTypeInfo);

        private ReadOnlySpan<byte> GetBytesWithTrimmedBom()
        {
            ReadOnlySpan<byte> span = _bytes.Span;

            // Check for the UTF-8 byte order mark (BOM) EF BB BF
            if (span.Length > 2 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
                span = span.Slice(3);

            return span;
        }

        /// <summary>
        /// Defines an implicit conversion from a <see cref="BinaryData" /> to a <see cref="ReadOnlyMemory{Byte}"/>.
        /// </summary>
        /// <param name="data">The value to be converted.</param>
        public static implicit operator ReadOnlyMemory<byte>(BinaryData? data) => data?._bytes ?? default;

        /// <summary>
        /// Defines an implicit conversion from a <see cref="BinaryData" /> to a <see cref="ReadOnlySpan{Byte}"/>.
        /// </summary>
        /// <param name="data">The value to be converted.</param>
        public static implicit operator ReadOnlySpan<byte>(BinaryData? data)
        {
            if (data == null)
            {
                return default;
            }
            return data._bytes.Span;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>
        /// <see langword="true" /> if the specified object is equal to the current object; otherwise, <see langword="false" />.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals([NotNullWhen(true)] object? obj) => ReferenceEquals(this, obj);

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();
    }
}
