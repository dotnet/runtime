// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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
                return String.Concat(_runtimeDeterminedDetailsType.DiagnosticName, "_", _rawCanonType.DiagnosticNamespace); ;
            }
        }
    }
}
