// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
