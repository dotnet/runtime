// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public partial class MethodDesc
    {
        /// <summary>
        /// Gets the shared runtime determined form of the method. This is a canonical form of the method
        /// where generic arguments of the method and the owning type have been converted to runtime determined types.
        /// </summary>
        public MethodDesc GetSharedRuntimeFormMethodTarget()
        {
            MethodDesc result = this;

            DefType owningType = OwningType as DefType;
            if (owningType != null)
            {
                // First find the method on the shared runtime form of the owning type
                DefType sharedRuntimeOwningType = owningType.ConvertToSharedRuntimeDeterminedForm();
                if (sharedRuntimeOwningType != owningType)
                {
                    result = Context.GetMethodForInstantiatedType(
                        GetTypicalMethodDefinition(), (InstantiatedType)sharedRuntimeOwningType);
                }

                // Now convert the method instantiation to the shared runtime form
                if (result.HasInstantiation)
                {
                    MethodDesc uninstantiatedMethod = result.GetMethodDefinition();

                    bool changed;
                    Instantiation sharedInstantiation = RuntimeDeterminedTypeUtilities.ConvertInstantiationToSharedRuntimeForm(
                        Instantiation, uninstantiatedMethod.Instantiation, out changed);

                    // If either the instantiation changed, or we switched the owning type, we need to find the matching
                    // instantiated method.
                    if (changed || result != this)
                    {
                        result = Context.GetInstantiatedMethod(uninstantiatedMethod, sharedInstantiation);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the type which holds the implementation of this method. This is typically the owning method,
        /// unless this method models a target of a constrained method call.
        /// </summary>
        public TypeDesc ImplementationType
        {
            get
            {
                // TODO: IsConstrainedMethod
                return OwningType;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a shared method body.
        /// </summary>
        public bool IsSharedByGenericInstantiations
        {
            get
            {
                return IsCanonicalMethod(CanonicalFormKind.Any);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is a canonical method that will only become concrete
        /// at runtime (after supplying the generic context).
        /// </summary>
        public bool IsRuntimeDeterminedExactMethod
        {
            get
            {
                TypeDesc containingType = ImplementationType;
                if (containingType.IsRuntimeDeterminedSubtype)
                    return true;

                // Handles situation when shared code refers to uninstantiated generic
                // method definitions (think: LDTOKEN).
                // Walking the instantiation would make us assert. This is simply
                // not a runtime determined method.
                if (IsGenericMethodDefinition)
                    return false;

                foreach (TypeDesc typeArg in Instantiation)
                {
                    if (typeArg.IsRuntimeDeterminedSubtype)
                        return true;
                }

                return false;
            }
        }

        public virtual MethodDesc GetNonRuntimeDeterminedMethodFromRuntimeDeterminedMethodViaSubstitution(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
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

            MethodDesc method = this;

            TypeDesc owningType = method.OwningType;
            TypeDesc instantiatedOwningType = owningType.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(typeInstantiation, methodInstantiation);
            if (owningType != instantiatedOwningType)
            {
                method = Context.GetMethodForInstantiatedType(method.GetTypicalMethodDefinition(), (InstantiatedType)instantiatedOwningType);
                if (clone == null && instantiation.Length != 0)
                    return Context.GetInstantiatedMethod(method, instantiation);
            }

            return (clone == null) ? method : Context.GetInstantiatedMethod(method.GetMethodDefinition(), new Instantiation(clone));
        }
    }
}
