// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Indicates whether a <see cref="StandaloneSignature"/> represents a standalone method or local variable signature.
    /// </summary>
    public enum StandaloneSignatureKind
    {
        /// <summary>
        /// The <see cref="StandaloneSignature"/> represents a standalone method signature.
        /// </summary>
        Method,

        /// <summary>
        /// The <see cref="MemberReference"/> references a local variable signature.
        /// </summary>
        LocalVariables,
    }
}
