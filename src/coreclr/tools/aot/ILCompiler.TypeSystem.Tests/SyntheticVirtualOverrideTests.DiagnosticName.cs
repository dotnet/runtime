// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;


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
