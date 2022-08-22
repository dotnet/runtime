// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection.Runtime.TypeInfos;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    //==========================================================================================================================
    // Policies for properties.
    //==========================================================================================================================
    internal sealed class PropertyPolicies : MemberPolicies<PropertyInfo>
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Reflection implementation")]
        public sealed override IEnumerable<PropertyInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredProperties;
        }

        public sealed override IEnumerable<PropertyInfo> CoreGetDeclaredMembers(RuntimeTypeInfo type, NameFilter? optionalNameFilter, RuntimeTypeInfo reflectedType)
        {
            return type.CoreGetDeclaredProperties(optionalNameFilter, reflectedType);
        }

        public sealed override bool AlwaysTreatAsDeclaredOnly => false;

        public sealed override void GetMemberAttributes(PropertyInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            MethodInfo? accessorMethod = GetAccessorMethod(member);
            if (accessorMethod == null)
            {
                // If we got here, this is a inherited PropertyInfo that only had private accessors and is now refusing to give them out
                // because that's what the rules of inherited PropertyInfo's are. Such a PropertyInfo is also considered private and will never be
                // given out of a Type.GetProperty() call. So all we have to do is set its visibility to Private and it will get filtered out.
                // Other values need to be set to satisify C# but they are meaningless.
                visibility = MethodAttributes.Private;
                isStatic = false;
                isVirtual = false;
                isNewSlot = true;
                return;
            }

            MethodAttributes methodAttributes = accessorMethod.Attributes;
            visibility = methodAttributes & MethodAttributes.MemberAccessMask;
            isStatic = (0 != (methodAttributes & MethodAttributes.Static));
            isVirtual = (0 != (methodAttributes & MethodAttributes.Virtual));
            isNewSlot = (0 != (methodAttributes & MethodAttributes.NewSlot));
        }

        public sealed override bool ImplicitlyOverrides(PropertyInfo? baseMember, PropertyInfo? derivedMember)
        {
            MethodInfo? baseAccessor = GetAccessorMethod(baseMember!);
            MethodInfo? derivedAccessor = GetAccessorMethod(derivedMember!);
            return MemberPolicies<MethodInfo>.Default.ImplicitlyOverrides(baseAccessor, derivedAccessor);
        }

        //
        // Desktop compat: Properties hide properties in base types if they share the same vtable slot, or
        // have the same name, return type, signature and hasThis value.
        //
        public sealed override bool IsSuppressedByMoreDerivedMember(PropertyInfo member, PropertyInfo[] priorMembers, int startIndex, int endIndex)
        {
            MethodInfo? baseAccessor = GetAccessorMethod(member);
            for (int i = startIndex; i < endIndex; i++)
            {
                PropertyInfo prior = priorMembers[i];
                MethodInfo? derivedAccessor = GetAccessorMethod(prior);
                if (!AreNamesAndSignaturesEqual(baseAccessor, derivedAccessor))
                    continue;
                if (derivedAccessor.IsStatic != baseAccessor.IsStatic)
                    continue;
                if (!(prior.PropertyType.Equals(member.PropertyType)))
                    continue;

                return true;
            }
            return false;
        }

        public sealed override bool OkToIgnoreAmbiguity(PropertyInfo m1, PropertyInfo m2)
        {
            return false;
        }

        private static MethodInfo? GetAccessorMethod(PropertyInfo property)
        {
            MethodInfo? accessor = property.GetMethod;
            if (accessor == null)
            {
                accessor = property.SetMethod;
            }

            return accessor;
        }
    }
}
