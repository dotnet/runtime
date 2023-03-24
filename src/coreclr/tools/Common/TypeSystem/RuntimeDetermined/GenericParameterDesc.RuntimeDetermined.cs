// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public partial class GenericParameterDesc
    {
        public sealed override bool IsRuntimeDeterminedSubtype
        {
            get
            {
                Debug.Fail("IsRuntimeDeterminedSubtype of an indefinite type");
                return false;
            }
        }

        public override TypeDesc GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            Debug.Assert(false);
            return this;
        }
    }
}
