// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
