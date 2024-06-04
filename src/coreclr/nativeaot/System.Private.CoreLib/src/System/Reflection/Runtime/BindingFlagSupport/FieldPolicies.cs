// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.TypeInfos;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    //==========================================================================================================================
    // Policies for fields.
    //==========================================================================================================================
    internal sealed class FieldPolicies : MemberPolicies<FieldInfo>
    {
        public static readonly FieldPolicies Instance = new FieldPolicies();

        public FieldPolicies() : base(MemberTypeIndex.Field) { }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Reflection implementation")]
        public sealed override IEnumerable<FieldInfo> GetDeclaredMembers(Type type)
        {
            return type.GetFields(DeclaredOnlyLookup);
        }

        public sealed override IEnumerable<FieldInfo> CoreGetDeclaredMembers(RuntimeTypeInfo type, NameFilter? optionalNameFilter, RuntimeTypeInfo reflectedType)
        {
            return type.CoreGetDeclaredFields(optionalNameFilter, reflectedType);
        }

        public sealed override bool AlwaysTreatAsDeclaredOnly => false;

        public sealed override void GetMemberAttributes(FieldInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            FieldAttributes fieldAttributes = member.Attributes;
            visibility = (MethodAttributes)(fieldAttributes & FieldAttributes.FieldAccessMask);
            isStatic = (0 != (fieldAttributes & FieldAttributes.Static));
            isVirtual = false;
            isNewSlot = false;
        }

        public sealed override bool ImplicitlyOverrides(FieldInfo baseMember, FieldInfo derivedMember) => false;

        public sealed override bool IsSuppressedByMoreDerivedMember(FieldInfo member, FieldInfo[] priorMembers, int startIndex, int endIndex)
        {
            return false;
        }

        public sealed override bool OkToIgnoreAmbiguity(FieldInfo m1, FieldInfo m2)
        {
            return true; // Unlike most member types, Field ambiguities are tolerated as long as they're defined in different classes.
        }
    }
}
