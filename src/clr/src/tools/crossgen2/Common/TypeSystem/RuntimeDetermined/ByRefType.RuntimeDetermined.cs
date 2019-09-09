// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Internal.TypeSystem
{
    partial class ByRefType
    {
        public override TypeDesc GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            TypeDesc parameterTypeConverted = ParameterType.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(typeInstantiation, methodInstantiation);
            if (ParameterType != parameterTypeConverted)
            {
                return Context.GetByRefType(parameterTypeConverted);
            }

            return this;
        }
    }
}
