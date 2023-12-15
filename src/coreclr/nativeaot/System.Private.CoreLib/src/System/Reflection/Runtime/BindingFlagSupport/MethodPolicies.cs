// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.TypeInfos;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    //==========================================================================================================================
    // Policies for methods.
    //==========================================================================================================================
    internal sealed class MethodPolicies : MemberPolicies<MethodInfo>
    {
        public static readonly MethodPolicies Instance = new MethodPolicies();

        public MethodPolicies() : base(MemberTypeIndex.Method) { }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Reflection implementation")]
        public sealed override IEnumerable<MethodInfo> GetDeclaredMembers(Type type)
        {
            return type.GetMethods(DeclaredOnlyLookup);
        }

        public sealed override IEnumerable<MethodInfo> CoreGetDeclaredMembers(RuntimeTypeInfo type, NameFilter? optionalNameFilter, RuntimeTypeInfo reflectedType)
        {
            return type.CoreGetDeclaredMethods(optionalNameFilter, reflectedType);
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

        public sealed override bool ImplicitlyOverrides(MethodInfo? baseMember, MethodInfo? derivedMember)
        {
            // TODO (https://github.com/dotnet/corert/issues/1896) Comparing signatures is fragile. The runtime and/or toolchain should have a way of sharing this info.
            return AreNamesAndSignaturesEqual(baseMember!, derivedMember!);
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
