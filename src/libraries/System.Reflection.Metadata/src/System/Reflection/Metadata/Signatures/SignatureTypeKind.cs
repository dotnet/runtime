// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public enum SignatureTypeKind : byte
    {
        /// <summary>
        /// It is not known in the current context if the type reference or definition is a class or value type.
        /// </summary>
        Unknown = 0,

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
