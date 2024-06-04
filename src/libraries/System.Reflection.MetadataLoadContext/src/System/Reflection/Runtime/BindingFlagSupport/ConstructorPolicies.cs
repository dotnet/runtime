// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using RuntimeTypeInfo = System.Reflection.TypeLoading.RoType;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    /// <summary>
    /// Policies for constructors
    /// </summary>
    internal sealed class ConstructorPolicies : MemberPolicies<ConstructorInfo>
    {
        public sealed override IEnumerable<ConstructorInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredConstructors;
        }

        public sealed override IEnumerable<ConstructorInfo> CoreGetDeclaredMembers(RuntimeTypeInfo type, NameFilter? filter, RuntimeTypeInfo reflectedType)
        {
            Debug.Assert(reflectedType.Equals(type));  // Constructor queries are always performed as if BindingFlags.DeclaredOnly are set so the reflectedType should always be the declaring type.
            return type.GetConstructorsCore(filter);
        }

        public sealed override BindingFlags ModifyBindingFlags(BindingFlags bindingFlags)
        {
            // Constructors are not inherited.
            return bindingFlags | BindingFlags.DeclaredOnly;
        }

        public sealed override bool AlwaysTreatAsDeclaredOnly => true;

        public sealed override void GetMemberAttributes(ConstructorInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            MethodAttributes methodAttributes = member.Attributes;
            visibility = methodAttributes & MethodAttributes.MemberAccessMask;
            isStatic = (0 != (methodAttributes & MethodAttributes.Static));
            isVirtual = false;
            isNewSlot = false;
        }

        public sealed override bool ImplicitlyOverrides(ConstructorInfo? baseMember, ConstructorInfo? derivedMember) => false;

        public sealed override bool IsSuppressedByMoreDerivedMember(ConstructorInfo member, ConstructorInfo[] priorMembers, int startIndex, int endIndex)
        {
            return false;
        }

        public sealed override bool OkToIgnoreAmbiguity(ConstructorInfo m1, ConstructorInfo m2)
        {
            // Constructors are only resolvable using an array of parameter types so this should never be called.
            Debug.Fail("This code path should be unreachable.");
            throw new NotSupportedException();
        }
    }
}
