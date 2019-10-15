// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Indicates whether a <see cref="MemberReference"/> references a method or field.
    /// </summary>
    public enum MemberReferenceKind
    {
        /// <summary>
        /// The <see cref="MemberReference"/> references a method.
        /// </summary>
        Method,

        /// <summary>
        /// The <see cref="MemberReference"/> references a field.
        /// </summary>
        Field,
    }
}
