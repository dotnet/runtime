// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class ArrayType
    {
        public override TypeDesc GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            TypeDesc parameterTypeConverted = ParameterType.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(typeInstantiation, methodInstantiation);
            if (ParameterType != parameterTypeConverted)
            {
                return Context.GetArrayType(parameterTypeConverted, _rank);
            }

            return this;
        }
    }

    public partial class ArrayMethod
    {
        public override MethodDesc GetNonRuntimeDeterminedMethodFromRuntimeDeterminedMethodViaSubstitution(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            TypeDesc owningType = this.OwningType;
            TypeDesc instantiatedOwningType = owningType.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(typeInstantiation, methodInstantiation);

            if (owningType != instantiatedOwningType)
                return ((ArrayType)instantiatedOwningType).GetArrayMethod(_kind);
            else
                return this;
        }
    }
}
