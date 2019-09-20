// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    partial class DebugNameFormatter
    {
        partial void GetDiagnosticName(GenericParameterDesc type, ref string diagnosticName)
        {
            try
            {
                diagnosticName = type.DiagnosticName;
            }
            catch {}
        }

        partial void GetDiagnosticName(DefType type, ref string diagnosticName)
        {
            try
            {
                diagnosticName = type.DiagnosticName;
            }
            catch {}
        }
    }
}
