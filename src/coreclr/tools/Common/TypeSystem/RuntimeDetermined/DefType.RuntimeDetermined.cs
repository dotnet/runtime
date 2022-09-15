// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class DefType
    {
        public override bool IsRuntimeDeterminedSubtype
        {
            get
            {
                // Handles situation when shared code refers to uninstantiated generic
                // type definitions (think: LDTOKEN).
                // Walking the instantiation would make us assert. This is simply
                // not a runtime determined type.
                if (IsGenericDefinition)
                    return false;

                foreach (TypeDesc type in Instantiation)
                {
                    if (type.IsRuntimeDeterminedSubtype)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Converts the type to the shared runtime determined form where the types this type is instantiatied
        /// over become bound to the generic parameters of this type.
        /// </summary>
        public DefType ConvertToSharedRuntimeDeterminedForm()
        {
            Instantiation instantiation = Instantiation;
            if (instantiation.Length > 0)
            {
                MetadataType typeDefinition = (MetadataType)GetTypeDefinition();

                bool changed;
                Instantiation sharedInstantiation = RuntimeDeterminedTypeUtilities.ConvertInstantiationToSharedRuntimeForm(
                    instantiation, typeDefinition.Instantiation, out changed);
                if (changed)
                {
                    return Context.GetInstantiatedType(typeDefinition, sharedInstantiation);
                }
            }

            return this;
        }

        public override TypeDesc GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            TypeDesc typeDefinition = GetTypeDefinition();
            if (this == typeDefinition)
                return this;

            Instantiation instantiation = Instantiation;
            TypeDesc[] clone = null;

            for (int i = 0; i < instantiation.Length; i++)
            {
                TypeDesc uninst = instantiation[i];
                TypeDesc inst = uninst.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(typeInstantiation, methodInstantiation);
                if (inst != uninst)
                {
                    if (clone == null)
                    {
                        clone = new TypeDesc[instantiation.Length];
                        for (int j = 0; j < clone.Length; j++)
                        {
                            clone[j] = instantiation[j];
                        }
                    }
                    clone[i] = inst;
                }
            }

            if (clone != null)
            {
                return Context.GetInstantiatedType((MetadataType)typeDefinition, new Instantiation(clone));
            }

            return this;
        }
    }
}
