// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Internal.NativeFormat;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    internal sealed partial class CanonType
    {
        public override string DiagnosticName
        {
            get
            {
                return _Name;
            }
        }
        public override string DiagnosticNamespace
        {
            get
            {
                return _Namespace;
            }
        }
    }

    internal sealed partial class UniversalCanonType
    {
        public override string DiagnosticName
        {
            get
            {
                return _Name;
            }
        }
        public override string DiagnosticNamespace
        {
            get
            {
                return _Namespace;
            }
        }
    }
}
