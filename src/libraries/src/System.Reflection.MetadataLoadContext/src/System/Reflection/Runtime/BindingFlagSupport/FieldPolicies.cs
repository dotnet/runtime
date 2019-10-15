// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using RuntimeTypeInfo = System.Reflection.TypeLoading.RoType;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    /// <summary>
    /// Policies for fields.
    /// </summary>
    internal sealed class FieldPolicies : MemberPolicies<FieldInfo>
    {
        public sealed override IEnumerable<FieldInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredFields;
        }

        public sealed override IEnumerable<FieldInfo> CoreGetDeclaredMembers(RuntimeTypeInfo type, NameFilter filter, RuntimeTypeInfo reflectedType)
        {
            return type.GetFieldsCore(filter, reflectedType);
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
