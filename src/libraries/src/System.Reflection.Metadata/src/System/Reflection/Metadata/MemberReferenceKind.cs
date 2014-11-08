// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Indicates whether a MemberReference references a method or field.
    /// </summary>
    public enum MemberReferenceKind
    {
        /// <summary>
        /// The MemberReference references a method.
        /// </summary>
        Method,

        /// <summary>
        /// The MemberReference references a field.
        /// </summary>
        Field,
    }
}
