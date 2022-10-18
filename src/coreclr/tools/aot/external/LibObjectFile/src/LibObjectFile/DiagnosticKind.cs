// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile
{
    /// <summary>
    /// Defines the kind of a <see cref="DiagnosticMessage"/>
    /// </summary>
    public enum DiagnosticKind
    {
        /// <summary>
        /// A warning message.
        /// </summary>
        Warning,

        /// <summary>
        /// An error message.
        /// </summary>
        Error,
    }
}