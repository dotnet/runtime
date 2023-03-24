// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public partial class FunctionPointerType
    {
        public override bool IsRuntimeDeterminedSubtype
        {
            get
            {
                if (_signature.ReturnType.IsRuntimeDeterminedSubtype)
                    return true;

                for (int i = 0; i < _signature.Length; i++)
                    if (_signature[i].IsRuntimeDeterminedSubtype)
                        return true;

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
