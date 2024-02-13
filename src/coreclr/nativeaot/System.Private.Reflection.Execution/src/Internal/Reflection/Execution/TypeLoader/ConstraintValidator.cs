// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;

using Debug = global::System.Diagnostics.Debug;

namespace Internal.Reflection.Execution
{
    internal static partial class ConstraintValidator
    {
        private static bool SatisfiesConstraints(this Type genericVariable, SigTypeContext typeContextOfConstraintDeclarer, Type typeArg)
        {
            GenericParameterAttributes specialConstraints = genericVariable.GenericParameterAttributes;

            if ((specialConstraints & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                if (!typeArg.IsValueType)
                {
                    return false;
                }
                else
                {
                    // the type argument is a value type, however if it is any kind of Nullable we want to fail
                    // as the constraint accepts any value type except Nullable types (Nullable itself is a value type)
                    if (typeArg.IsNullable())
                        return false;
                }
            }

            if ((specialConstraints & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                if (typeArg.IsValueType)
                    return false;
            }

            if ((specialConstraints & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
            {
                if (!typeArg.HasExplicitOrImplicitPublicDefaultConstructor())
                    return false;
            }

            if (typeArg.IsByRefLike && (specialConstraints & (GenericParameterAttributes)0x20 /* GenericParameterAttributes.AllowByRefLike */) == 0)
                return false;

            // Now check general subtype constraints
            foreach (var constraint in genericVariable.GetGenericParameterConstraints())
            {
                Type instantiatedTypeConstraint = constraint.Instantiate(typeContextOfConstraintDeclarer);

                // System.Object constraint will be always satisfied - even if argList is empty
                if (instantiatedTypeConstraint.IsSystemObject())
                    continue;

                // if a concrete type can be cast to the constraint, then this constraint will be satisfied
                if (!AreTypesAssignable(typeArg, instantiatedTypeConstraint))
                    return false;
            }

            return true;
        }

        private static void EnsureSatisfiesClassConstraints(Type[] typeParameters, Type[] typeArguments, object definition, SigTypeContext typeContext)
        {
            if (typeParameters.Length != typeArguments.Length)
            {
                throw new ArgumentException(SR.Argument_GenericArgsCount);
            }

            // Do sanity validation of all arguments first. The actual constraint validation can fail in unexpected ways
            // if it hits SigTypeContext with these never valid types.
            for (int i = 0; i < typeParameters.Length; i++)
            {
                Type actualArg = typeArguments[i];

                if (actualArg.IsSystemVoid() || (actualArg.HasElementType && !actualArg.IsArray) || actualArg.IsFunctionPointer)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_NeverValidGenericArgument, actualArg));
                }
            }

            for (int i = 0; i < typeParameters.Length; i++)
            {
                Type formalArg = typeParameters[i];
                Type actualArg = typeArguments[i];

                if (!formalArg.SatisfiesConstraints(typeContext, actualArg))
                {
                    throw new ArgumentException(SR.Format(SR.Argument_ConstraintFailed, actualArg, definition.ToString(), formalArg));
                }
            }
        }

        public static void EnsureSatisfiesClassConstraints(Type typeDefinition, Type[] typeArguments)
        {
            Type[] typeParameters = typeDefinition.GetGenericArguments();
            SigTypeContext typeContext = new SigTypeContext(typeArguments, null);
            EnsureSatisfiesClassConstraints(typeParameters, typeArguments, typeDefinition, typeContext);
        }

        public static void EnsureSatisfiesClassConstraints(MethodInfo reflectionMethodInfo)
        {
            MethodInfo genericMethodDefinition = reflectionMethodInfo.GetGenericMethodDefinition();
            Type[] methodArguments = reflectionMethodInfo.GetGenericArguments();
            Type[] methodParameters = genericMethodDefinition.GetGenericArguments();
            Type[] typeArguments = reflectionMethodInfo.DeclaringType.GetGenericArguments();
            SigTypeContext typeContext = new SigTypeContext(typeArguments, methodArguments);
            EnsureSatisfiesClassConstraints(methodParameters, methodArguments, genericMethodDefinition, typeContext);
        }
    }
}
