// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.TypeSystem
{
    public abstract partial class GenericParameterDesc
    {
        /// <summary>
        /// Gets the name of the generic parameter as defined in the metadata. This must not throw
        /// </summary>
        public virtual string DiagnosticName
        {
            get
            {
                return string.Concat("T", Index.ToStringInvariant());
            }
        }
    }
}
