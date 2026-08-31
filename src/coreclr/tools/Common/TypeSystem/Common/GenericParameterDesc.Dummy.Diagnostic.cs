// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Dummy implementation of diagnostic names that just forwards to Name
    public abstract partial class GenericParameterDesc
    {
        /// <summary>
        /// Gets the name of the generic parameter as defined in the metadata. This must not throw
        /// </summary>
        public string DiagnosticName => Name;
    }
}
