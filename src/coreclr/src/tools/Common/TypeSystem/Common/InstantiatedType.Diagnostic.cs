// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public sealed partial class InstantiatedType
    {
        public override string DiagnosticName
        {
            get
            {
                return _typeDef.DiagnosticName;
            }
        }

        public override string DiagnosticNamespace
        {
            get
            {
                return _typeDef.DiagnosticNamespace;
            }
        }
    }
}
