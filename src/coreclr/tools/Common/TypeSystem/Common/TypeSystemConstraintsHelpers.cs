// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public class InstantiationContext
    {
        public readonly Instantiation TypeInstantiation;
        public readonly Instantiation MethodInstantiation;

        public InstantiationContext(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            TypeInstantiation = typeInstantiation;
            MethodInstantiation = methodInstantiation;
        }
    }

    public static class TypeSystemConstraintsHelpers
    {
        private static bool VerifyGenericParamConstraint(InstantiationContext genericParamContext, GenericParameterDesc genericParam,
            InstantiationContext instantiationParamContext, TypeDesc instantiationParam)
        {
            GenericConstraints constraints = genericParam.Constraints;

            // Check class constraint
            if ((constraints & GenericConstraints.ReferenceTypeConstraint) != 0)
            {
                if (!instantiationParam.IsGCPointer
                    && !CheckGenericSpecialConstraint(instantiationParam, GenericConstraints.ReferenceTypeConstraint))
                    return false;
            }

            // Check default constructor constraint
            if ((constraints & GenericConstraints.DefaultConstructorConstraint) != 0)
            {
                if (!instantiationParam.HasExplicitOrImplicitDefaultConstructor()
                    && !CheckGenericSpecialConstraint(instantiationParam, GenericConstraints.DefaultConstructorConstraint))
                    return false;
            }

            // Check struct constraint
            if ((constraints & GenericConstraints.NotNullableValueTypeConstraint) != 0)
            {
                if ((!instantiationParam.IsValueType || instantiationParam.IsNullable)
                    && !CheckGenericSpecialConstraint(instantiationParam, GenericConstraints.NotNullableValueTypeConstraint))
                    return false;
            }

            // Check for ByRefLike support
            if (instantiationParam.IsByRefLike && (constraints & GenericConstraints.AcceptByRefLike) == 0)
                return false;

            var instantiatedConstraints = new ArrayBuilder<TypeDesc>();
            GetInstantiatedConstraintsRecursive(instantiationParamContext, instantiationParam, ref instantiatedConstraints);

            foreach (var constraintType in genericParam.TypeConstraints)
            {
                var instantiatedType = constraintType.InstantiateSignature(genericParamContext.TypeInstantiation, genericParamContext.MethodInstantiation);
                if (CanCastConstraint(ref instantiatedConstraints, instantiatedType))
                    continue;

                if (!instantiationParam.CanCastTo(instantiatedType))
                    return false;
            }

            return true;
        }

        // Used to determine whether a type parameter used to instantiate another type parameter with a specific special
        // constraint satisfies that constraint.
        private static bool CheckGenericSpecialConstraint(TypeDesc type, GenericConstraints specialConstraint)
        {
            if (!type.IsGenericParameter)
                return false;

            var genericType = (GenericParameterDesc)type;

            GenericConstraints constraints = genericType.Constraints;

            // Check if type has specialConstraint on its own
            if ((constraints & specialConstraint) != 0)
                return true;

            // Value type always has default constructor
            if (specialConstraint == GenericConstraints.DefaultConstructorConstraint && (constraints & GenericConstraints.NotNullableValueTypeConstraint) != 0)
                return true;

            // The special constraints did not match, check if there is a primary type constraint,
            // that would always satisfy the special constraint
            foreach (var constraint in genericType.TypeConstraints)
            {
                if (constraint.IsGenericParameter || constraint.IsInterface)
                    continue;

                switch (specialConstraint)
                {
                    case GenericConstraints.NotNullableValueTypeConstraint:
                        if (constraint.IsValueType && !constraint.IsNullable)
                            return true;
                        break;
                    case GenericConstraints.ReferenceTypeConstraint:
                        if (!constraint.IsValueType)
                            return true;
                        break;
                    case GenericConstraints.DefaultConstructorConstraint:
                        // As constraint is only ancestor, can only be sure whether type has public default constructor if it is a value type
                        if (constraint.IsValueType)
                            return true;
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
            }

            // type did not satisfy special constraint in any way
            return false;
        }

        private static void GetInstantiatedConstraintsRecursive(InstantiationContext typeContext, TypeDesc type, ref ArrayBuilder<TypeDesc> instantiatedConstraints)
        {
            if (!type.IsGenericParameter || typeContext == null)
                return;

            GenericParameterDesc genericParam = (GenericParameterDesc)type;

            foreach (var constraint in genericParam.TypeConstraints)
            {
                var instantiatedType = constraint.InstantiateSignature(typeContext.TypeInstantiation, typeContext.MethodInstantiation);

                if (instantiatedType.IsGenericParameter)
                {
                    // Make sure it is save to call this method recursively
                    if (!instantiatedConstraints.Contains(instantiatedType))
                    {
                        instantiatedConstraints.Add(instantiatedType);

                        // Constraints of this constraint apply to 'genericParam' too
                        GetInstantiatedConstraintsRecursive(typeContext, instantiatedType, ref instantiatedConstraints);
                    }
                }
                else
                {
                    instantiatedConstraints.Add(instantiatedType);
                }
            }
        }

        private static bool CanCastConstraint(ref ArrayBuilder<TypeDesc> instantiatedConstraints, TypeDesc instantiatedType)
        {
            for (int i = 0; i < instantiatedConstraints.Count; ++i)
            {
                if (instantiatedConstraints[i].CanCastTo(instantiatedType))
                    return true;
            }

            return false;
        }

        public static bool CheckValidInstantiationArguments(this Instantiation instantiation)
        {
            foreach(var arg in instantiation)
            {
                if (arg.IsPointer || arg.IsByRef || arg.IsGenericParameter || arg.IsVoid)
                    return false;

                if (arg.HasInstantiation)
                {
                    if (!CheckValidInstantiationArguments(arg.Instantiation))
                        return false;
                }
            }
            return true;
        }

        public static bool CheckConstraints(this TypeDesc type, InstantiationContext context = null)
        {
            TypeDesc uninstantiatedType = type.GetTypeDefinition();

            // Non-generic types always pass constraints check
            if (uninstantiatedType == type)
                return true;

            var paramContext = new InstantiationContext(type.Instantiation, default(Instantiation));
            for (int i = 0; i < uninstantiatedType.Instantiation.Length; i++)
            {
                if (!VerifyGenericParamConstraint(paramContext, (GenericParameterDesc)uninstantiatedType.Instantiation[i], context, type.Instantiation[i]))
                    return false;
            }

            return true;
        }

        public static bool CheckConstraints(this MethodDesc method, InstantiationContext context = null)
        {
            if (!method.OwningType.CheckConstraints(context))
                return false;

            // Non-generic methods always pass constraints check
            if (!method.HasInstantiation)
                return true;

            var paramContext = new InstantiationContext(method.OwningType.Instantiation, method.Instantiation);
            MethodDesc uninstantiatedMethod = method.GetMethodDefinition();
            for (int i = 0; i < uninstantiatedMethod.Instantiation.Length; i++)
            {
                if (!VerifyGenericParamConstraint(paramContext, (GenericParameterDesc)uninstantiatedMethod.Instantiation[i], context, method.Instantiation[i]))
                    return false;
            }

            return true;
        }
    }
}
