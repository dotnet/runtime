// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection.Runtime.TypeInfos;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    //==========================================================================================================================
    // Policies for events.
    //==========================================================================================================================
    internal sealed class EventPolicies : MemberPolicies<EventInfo>
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Reflection implementation")]
        public sealed override IEnumerable<EventInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredEvents;
        }

        public sealed override IEnumerable<EventInfo> CoreGetDeclaredMembers(RuntimeTypeInfo type, NameFilter optionalNameFilter, RuntimeTypeInfo reflectedType)
        {
            return type.CoreGetDeclaredEvents(optionalNameFilter, reflectedType);
        }

        public sealed override bool AlwaysTreatAsDeclaredOnly => false;

        public sealed override void GetMemberAttributes(EventInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            MethodInfo accessorMethod = GetAccessorMethod(member);
            MethodAttributes methodAttributes = accessorMethod.Attributes;
            visibility = methodAttributes & MethodAttributes.MemberAccessMask;
            isStatic = (0 != (methodAttributes & MethodAttributes.Static));
            isVirtual = (0 != (methodAttributes & MethodAttributes.Virtual));
            isNewSlot = (0 != (methodAttributes & MethodAttributes.NewSlot));
        }

        //
        // Desktop compat: Events hide events in base types if they have the same name.
        //
        public sealed override bool IsSuppressedByMoreDerivedMember(EventInfo member, EventInfo[] priorMembers, int startIndex, int endIndex)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                if (priorMembers[i].Name == member.Name)
                    return true;
            }
            return false;
        }

        public sealed override bool ImplicitlyOverrides(EventInfo baseMember, EventInfo derivedMember)
        {
            MethodInfo baseAccessor = GetAccessorMethod(baseMember);
            MethodInfo derivedAccessor = GetAccessorMethod(derivedMember);
            return MemberPolicies<MethodInfo>.Default.ImplicitlyOverrides(baseAccessor, derivedAccessor);
        }

        public sealed override bool OkToIgnoreAmbiguity(EventInfo m1, EventInfo m2)
        {
            return false;
        }

        private static MethodInfo GetAccessorMethod(EventInfo e)
        {
            MethodInfo accessor = e.AddMethod;
            return accessor;
        }
    }
}
