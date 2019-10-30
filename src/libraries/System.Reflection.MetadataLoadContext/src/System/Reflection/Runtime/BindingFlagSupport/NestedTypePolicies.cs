// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;
using RuntimeTypeInfo = System.Reflection.TypeLoading.RoType;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    /// <summary>
    /// Policies for nested types.
    ///
    /// Nested types enumerate a little differently than other members:
    ///    Base classes are never searched, regardless of BindingFlags.DeclaredOnly value.
    ///    Public|NonPublic|IgnoreCase are the only relevant BindingFlags. The apis ignore any other bits.
    ///    There is no such thing as a "static" or "instanced" nested type. For enumeration purposes,
    ///    we'll arbitrarily denote all nested types as "static."
    /// </summary>
    internal sealed class NestedTypePolicies : MemberPolicies<Type>
    {
        public sealed override IEnumerable<Type> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredNestedTypes;
        }

        public sealed override IEnumerable<Type> CoreGetDeclaredMembers(RuntimeTypeInfo type, NameFilter filter, RuntimeTypeInfo reflectedType)
        {
            Debug.Assert(reflectedType.Equals(type));  // NestedType queries are always performed as if BindingFlags.DeclaredOnly are set so the reflectedType should always be the declaring type.
            return type.GetNestedTypesCore(filter);
        }

        public sealed override bool AlwaysTreatAsDeclaredOnly => true;

        public sealed override void GetMemberAttributes(Type member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            isStatic = true;
            isVirtual = false;
            isNewSlot = false;

            // Since we never search base types for nested types, we don't need to map every visibility value one to one.
            // We just need to distinguish between "public" and "everything else."
            visibility = member.IsNestedPublic ? MethodAttributes.Public : MethodAttributes.Private;
        }

        public sealed override bool ImplicitlyOverrides(Type baseMember, Type derivedMember) => false;

        public sealed override bool IsSuppressedByMoreDerivedMember(Type member, Type[] priorMembers, int startIndex, int endIndex)
        {
            return false;
        }

        public sealed override BindingFlags ModifyBindingFlags(BindingFlags bindingFlags)
        {
            bindingFlags &= BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            bindingFlags |= BindingFlags.Static | BindingFlags.DeclaredOnly;
            return bindingFlags;
        }

        public sealed override bool OkToIgnoreAmbiguity(Type m1, Type m2)
        {
            return false;
        }
    }
}
