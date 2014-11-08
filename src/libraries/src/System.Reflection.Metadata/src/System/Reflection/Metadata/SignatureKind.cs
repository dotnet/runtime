// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Specifies the signature kind. Underlying values correspond to the representation 
    /// in the leading signature byte represented by <see cref="SignatureHeader"/>.
    /// </summary>
    public enum SignatureKind : byte
    {
        /// <summary>
        /// Method reference, method definition, or standalone method signature.
        /// </summary>
        Method = 0x0,

        /// <summary>
        /// Field signature.
        /// </summary>
        Field = 0x6,

        /// <summary>
        /// Local variables signature.
        /// </summary>
        LocalVariables = 0x7,

        /// <summary>
        /// Property signature.
        /// </summary>
        Property = 0x8,

        /// <summary>
        /// Method specification signature.
        /// </summary>
        MethodSpecification = 0xA,
    }
}
