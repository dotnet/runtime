// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using RuntimeTypeInfo = System.Reflection.TypeLoading.RoType;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    /// <summary>
    /// Policies for methods.
    /// </summary>
    internal sealed class MethodPolicies : MemberPolicies<MethodInfo>
    {
        public sealed override IEnumerable<MethodInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredMethods;
        }

        public sealed override IEnumerable<MethodInfo> CoreGetDeclaredMembers(RuntimeTypeInfo type, NameFilter filter, RuntimeTypeInfo reflectedType)
        {
            return type.GetMethodsCore(filter, reflectedType);
        }

        public sealed override bool AlwaysTreatAsDeclaredOnly => false;

        public sealed override void GetMemberAttributes(MethodInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            MethodAttributes methodAttributes = member.Attributes;
            visibility = methodAttributes & MethodAttributes.MemberAccessMask;
            isStatic = (0 != (methodAttributes & MethodAttributes.Static));
            isVirtual = (0 != (methodAttributes & MethodAttributes.Virtual));
            isNewSlot = (0 != (methodAttributes & MethodAttributes.NewSlot));
        }

        public sealed override bool ImplicitlyOverrides(MethodInfo baseMember, MethodInfo derivedMember)
        {
            return AreNamesAndSignaturesEqual(baseMember, derivedMember);
        }

        //
        // Methods hide methods in base types if they share the same vtable slot.
        //
        public sealed override bool IsSuppressedByMoreDerivedMember(MethodInfo member, MethodInfo[] priorMembers, int startIndex, int endIndex)
        {
            if (!member.IsVirtual)
                return false;

            for (int i = startIndex; i < endIndex; i++)
            {
                MethodInfo prior = priorMembers[i];
                MethodAttributes attributes = prior.Attributes & (MethodAttributes.Virtual | MethodAttributes.VtableLayoutMask);
                if (attributes != (MethodAttributes.Virtual | MethodAttributes.ReuseSlot))
                    continue;
                if (!ImplicitlyOverrides(member, prior))
                    continue;

                return true;
            }
            return false;
        }

        public sealed override bool OkToIgnoreAmbiguity(MethodInfo m1, MethodInfo m2)
        {
            return DefaultBinder.CompareMethodSig(m1, m2);  // If all candidates have the same signature, pick the most derived one without throwing an AmbiguousMatchException.
        }
    }
}
