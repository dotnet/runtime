// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    // Dummy implementation of diagnostic names that just forwards to Name
    partial class MethodDesc
    {
        public string DiagnosticName => Name;
    }
}
