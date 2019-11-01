// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Type codes used to encode types of values in Custom Attribute value blob.
    /// </summary>
    public enum SerializationTypeCode : byte
    {
        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.Invalid"/>.
        /// </summary>
        Invalid = SignatureTypeCode.Invalid,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.Boolean"/>.
        /// </summary>
        Boolean = SignatureTypeCode.Boolean,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.Char"/>.
        /// </summary>
        Char = SignatureTypeCode.Char,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.SByte"/>.
        /// </summary>
        SByte = SignatureTypeCode.SByte,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.Byte"/>.
        /// </summary>
        Byte = SignatureTypeCode.Byte,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.Int16"/>.
        /// </summary>
        Int16 = SignatureTypeCode.Int16,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.UInt16"/>.
        /// </summary>
        UInt16 = SignatureTypeCode.UInt16,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.Int32"/>.
        /// </summary>
        Int32 = SignatureTypeCode.Int32,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.UInt32"/>.
        /// </summary>
        UInt32 = SignatureTypeCode.UInt32,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.Int64"/>.
        /// </summary>
        Int64 = SignatureTypeCode.Int64,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.UInt64"/>.
        /// </summary>
        UInt64 = SignatureTypeCode.UInt64,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.Single"/>.
        /// </summary>
        Single = SignatureTypeCode.Single,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.Double"/>.
        /// </summary>
        Double = SignatureTypeCode.Double,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.String"/>.
        /// </summary>
        String = SignatureTypeCode.String,

        /// <summary>
        /// Equivalent to <see cref="SignatureTypeCode.SZArray"/>.
        /// </summary>
        SZArray = SignatureTypeCode.SZArray,

        /// <summary>
        /// The attribute argument is a System.Type instance.
        /// </summary>
        Type = 0x50,

        /// <summary>
        /// The attribute argument is "boxed" (passed to a parameter, field, or property of type object) and carries type information in the attribute blob.
        /// </summary>
        TaggedObject = 0x51,

        /// <summary>
        /// The attribute argument is an Enum instance.
        /// </summary>
        Enum = 0x55,
    }
}
