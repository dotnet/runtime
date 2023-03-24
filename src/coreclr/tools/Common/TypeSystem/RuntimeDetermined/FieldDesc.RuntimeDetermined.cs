// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class FieldDesc
    {
        public FieldDesc GetNonRuntimeDeterminedFieldFromRuntimeDeterminedFieldViaSubstitution(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            DefType owningType = OwningType;
            TypeDesc owningTypeInstantiated = owningType.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(typeInstantiation, methodInstantiation);
            if (owningTypeInstantiated != owningType)
            {
                return Context.GetFieldForInstantiatedType(GetTypicalFieldDefinition(), (InstantiatedType)owningTypeInstantiated);
            }

            return this;
        }
    }
}
