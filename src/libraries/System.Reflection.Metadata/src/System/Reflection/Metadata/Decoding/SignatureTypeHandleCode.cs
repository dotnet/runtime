// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata.Decoding
{
    public enum SignatureTypeHandleCode : byte
    {
        /// <summary>
        /// It is not known in the current context if the type reference or definition is a class or value type.
        /// This will be the case when <see cref="SignatureDecoderOptions.DifferentiateClassAndValueTypes"/> is not specified.
        /// </summary>
        Unresolved = 0,

        /// <summary>
        /// The type definition or reference refers to a class.
        /// </summary>
        Class = CorElementType.ELEMENT_TYPE_CLASS,

        /// <summary>
        /// The type definition or reference refers to a value type.
        /// </summary>
        ValueType = CorElementType.ELEMENT_TYPE_VALUETYPE,
    }
}
