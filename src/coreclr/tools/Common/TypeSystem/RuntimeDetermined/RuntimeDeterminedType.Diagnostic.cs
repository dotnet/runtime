// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    partial class RuntimeDeterminedType
    {
        public override string DiagnosticName
        {
            get
            {
                return _rawCanonType.DiagnosticName;
            }
        }

        public override string DiagnosticNamespace
        {
            get
            {
                return _rawCanonType.DiagnosticNamespace;
            }
        }
    }
}
