// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Cbor
{
    /// <summary>Specifies the state of a CborReader instance.</summary>
    public enum CborReaderState
    {
        /// <summary>Indicates the undefined state.</summary>
        Undefined = 0,

        /// <summary>Indicates that the next CBOR data item is an unsigned integer (major type 0).</summary>
        UnsignedInteger,

        /// <summary>Indicates that the next CBOR data item is a negative integer (major type 1).</summary>
        NegativeInteger,

        /// <summary>Indicates that the next CBOR data item is a byte string (major type 2).</summary>
        ByteString,

        /// <summary>Indicates that the next CBOR data item denotes the start of an indefinite-length byte string (major type 2).</summary>
        StartIndefiniteLengthByteString,

        /// <summary>Indicates that the reader is at the end of an indefinite-length byte string (major type 2).</summary>
        EndIndefiniteLengthByteString,

        /// <summary>Indicates that the next CBOR data item is a UTF-8 string (major type 3).</summary>
        TextString,

        /// <summary>Indicates that the next CBOR data item denotes the start of an indefinite-length UTF-8 text string (major type 3).</summary>
        StartIndefiniteLengthTextString,

        /// <summary>Indicates that the reader is at the end of an indefinite-length UTF-8 text string (major type 3).</summary>
        EndIndefiniteLengthTextString,

        /// <summary>Indicates that the next CBOR data item denotes the start of an array (major type 4).</summary>
        StartArray,

        /// <summary>Indicates that the reader is at the end of an array (major type 4).</summary>
        EndArray,

        /// <summary>Indicates that the next CBOR data item denotes the start of a map (major type 5).</summary>
        StartMap,

        /// <summary>Indicates that the reader is at the end of a map (major type 5).</summary>
        EndMap,

        /// <summary>Indicates that the next CBOR data item is a semantic tag (major type 6).</summary>
        Tag,

        /// <summary>Indicates that the next CBOR data item is a simple value (major type 7).</summary>
        SimpleValue,

        /// <summary>Indicates that the next CBOR data item is an IEEE 754 Half-Precision float (major type 7).</summary>
        HalfPrecisionFloat,

        /// <summary>Indicates that the next CBOR data item is an IEEE 754 Single-Precision float (major type 7).</summary>
        SinglePrecisionFloat,

        /// <summary>Indicates that the next CBOR data item is an IEEE 754 Double-Precision float (major type 7).</summary>
        DoublePrecisionFloat,

        /// <summary>Indicates that the next CBOR data item is a <see langword="null" /> literal (major type 7).</summary>
        Null,

        /// <summary>Indicates that the next CBOR data item encodes a <see cref="bool" /> value (major type 7).</summary>
        Boolean,

        /// <summary>
        /// <para>Indicates that the reader has completed reading a full CBOR document.</para>
        /// <para>If <see cref="CborReader.AllowMultipleRootLevelValues" /> is set to <see langword="false" />,
        /// the reader will report this value even if the buffer contains trailing bytes.</para>
        /// </summary>
        Finished,
    }
}
