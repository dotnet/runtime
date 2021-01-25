// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using Internal.TypeSystem;

using Xunit;


namespace TypeSystemTests
{
    public partial class SyntheticVirtualOverrideTests
    {
        private partial class SyntheticMethod : MethodDesc
        {
            public override string DiagnosticName
            {
                get
                {
                    return _name;
                }
            }
        }
    }
}
