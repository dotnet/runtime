// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Dummy implementation of diagnostic names that just forwards to Name/Namespace
    abstract partial class DefType
    {
        /// <summary>
        /// Gets the Name of a type. This must not throw
        /// </summary>
        public string DiagnosticName => Name;

        /// <summary>
        /// Gets the Namespace of a type. This must not throw
        /// </summary>
        public string DiagnosticNamespace => Namespace;
    }
}
