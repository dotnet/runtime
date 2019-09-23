// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Internal.TypeSystem
{
    public abstract partial class GenericParameterDesc
    {
        /// <summary>
        /// Gets the name of the generic parameter as defined in the metadata. This must not throw
        /// </summary>
        public abstract string DiagnosticName { get; }
    }
}
