// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class InstantiatedType
    {
        public override TypeIdentifierData TypeIdentifierData => _typeDef.TypeIdentifierData;
        public override bool IsWindowsRuntime => _typeDef.IsWindowsRuntime;

    }
}
