// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Debug = System.Diagnostics.Debug;

using Internal.NativeFormat;

namespace Internal.TypeSystem.Ecma
{
    /// <summary>
    /// Override of MetadataType that uses actual Ecma335 metadata.
    /// </summary>
    public sealed partial class EcmaType
    {
        public override string DiagnosticName
        {
            get
            {
                if (_typeName == null)
                    return InitializeName();
                return _typeName;
            }
        }
    }
}
