// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace Internal.Reflection.Extensions.NonPortable
{
    //=================================================================================================================
    // This class encapsulates the minimum set of arcane desktop CLR policies needed to implement the System.Reflections.Extensions contract.
    //
    // In particular, it encapsulates behaviors such as what exactly determines the "visibility" of a property and event, and
    // what determines whether and how they are overridden.
    //=================================================================================================================
    internal abstract class MemberPolicies<M> where M : MemberInfo
    {
        //=================================================================================================================
        // Subclasses for specific MemberInfo types must override these:
        //=================================================================================================================

        //
        // Returns all of the directly declared members on the given TypeInfo.
        //
        public abstract IEnumerable<M> GetDeclaredMembers(TypeInfo typeInfo);

        //
        // Policy to decide whether a member is considered "virtual", "virtual new" and what its member visibility is.
        // (For "visibility", we reuse the MethodAttributes enum since Reflection lacks an element-agnostic enum for this.
        //  Only the MemberAccessMask bits are set.)
        //
        public abstract void GetMemberAttributes(M member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot);

        //
        // Policy to decide whether two virtual members are signature-compatible for the purpose of implicit overriding. 
        //
        public abstract bool AreNamesAndSignatureEqual(M member1, M member2);

        //
        // Policy to decide how BindingFlags should be reinterpreted for a given member type.
        // This is overridden for nested types which all match on any combination Instance | Static and are never inherited.
        // It is also overridden for constructors which are never inherited.
        //
        public virtual BindingFlags ModifyBindingFlags(BindingFlags bindingFlags)
        {
            return bindingFlags;
        }

        //
        // Policy to create a wrapper MemberInfo (if appropriate). This is due to the fact that MemberInfo's actually have their identity
        // tied to the type they were queried off of and this unfortunate fact shows up in certain api behaviors.
        //
        public virtual M GetInheritedMemberInfo(M underlyingMemberInfo, Type reflectedType)
        {
            return underlyingMemberInfo;
        }

        //
        // Helper method for determining whether two methods are signature-compatible for the purpose of implicit overriding.
        //
        protected static bool AreNamesAndSignaturesEqual(MethodInfo method1, MethodInfo method2)
        {
            if (method1.Name != method2.Name)
            {
                return false;
            }

            ParameterInfo[] p1 = method1.GetParameters();
            ParameterInfo[] p2 = method2.GetParameters();
            if (p1.Length != p2.Length)
            {
                return false;
            }

            for (int i = 0; i < p1.Length; i++)
            {
                Type parameterType1 = p1[i].ParameterType;
                Type parameterType2 = p2[i].ParameterType;
                if (!(parameterType1.Equals(parameterType2)))
                {
                    return false;
                }
            }
            return true;
        }

        //
        // This is a singleton class one for each MemberInfo category: Return the appropriate one. 
        //
        public static MemberPolicies<M> Default
        {
            get
            {
                if (_default == null)
                {
                    Type t = typeof(M);
                    if (t.Equals(typeof(FieldInfo)))
                    {
                        _default = (MemberPolicies<M>)(Object)(new FieldPolicies());
                    }
                    else if (t.Equals(typeof(MethodInfo)))
                    {
                        _default = (MemberPolicies<M>)(Object)(new MethodPolicies());
                    }
                    else if (t.Equals(typeof(ConstructorInfo)))
                    {
                        _default = (MemberPolicies<M>)(Object)(new ConstructorPolicies());
                    }
                    else if (t.Equals(typeof(PropertyInfo)))
                    {
                        _default = (MemberPolicies<M>)(Object)(new PropertyPolicies());
                    }
                    else if (t.Equals(typeof(EventInfo)))
                    {
                        _default = (MemberPolicies<M>)(Object)(new EventPolicies());
                    }
                    else if (t.Equals(typeof(TypeInfo)))
                    {
                        _default = (MemberPolicies<M>)(Object)(new NestedTypePolicies());
                    }
                    else
                    {
                        Debug.Assert(false, "Unknown MemberInfo type.");
                    }
                }
                return _default;
            }
        }

        private static volatile MemberPolicies<M> _default;
    }

    //==========================================================================================================================
    // Policies for fields.
    //==========================================================================================================================
    internal sealed class FieldPolicies : MemberPolicies<FieldInfo>
    {
        public sealed override IEnumerable<FieldInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredFields;
        }

        public sealed override void GetMemberAttributes(FieldInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            FieldAttributes fieldAttributes = member.Attributes;
            visibility = (MethodAttributes)(fieldAttributes & FieldAttributes.FieldAccessMask);
            isStatic = (0 != (fieldAttributes & FieldAttributes.Static));
            isVirtual = false;
            isNewSlot = false;
        }

        public sealed override bool AreNamesAndSignatureEqual(FieldInfo member1, FieldInfo member2)
        {
            Debug.Assert(false, "This code path should be unreachable as fields are never \"virtual\".");
            throw new NotSupportedException();
        }
    }


    //==========================================================================================================================
    // Policies for constructors.
    //==========================================================================================================================
    internal sealed class ConstructorPolicies : MemberPolicies<ConstructorInfo>
    {
        public sealed override IEnumerable<ConstructorInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredConstructors;
        }

        public sealed override BindingFlags ModifyBindingFlags(BindingFlags bindingFlags)
        {
            // Constructors are not inherited.
            return bindingFlags | BindingFlags.DeclaredOnly;
        }

        public sealed override void GetMemberAttributes(ConstructorInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            MethodAttributes methodAttributes = member.Attributes;
            visibility = methodAttributes & MethodAttributes.MemberAccessMask;
            isStatic = (0 != (methodAttributes & MethodAttributes.Static));
            isVirtual = false;
            isNewSlot = false;
        }

        public sealed override bool AreNamesAndSignatureEqual(ConstructorInfo member1, ConstructorInfo member2)
        {
            Debug.Assert(false, "This code path should be unreachable as constructors are never \"virtual\".");
            throw new NotSupportedException();
        }
    }


    //==========================================================================================================================
    // Policies for methods.
    //==========================================================================================================================
    internal sealed class MethodPolicies : MemberPolicies<MethodInfo>
    {
        public sealed override IEnumerable<MethodInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredMethods;
        }

        public sealed override void GetMemberAttributes(MethodInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            MethodAttributes methodAttributes = member.Attributes;
            visibility = methodAttributes & MethodAttributes.MemberAccessMask;
            isStatic = (0 != (methodAttributes & MethodAttributes.Static));
            isVirtual = (0 != (methodAttributes & MethodAttributes.Virtual));
            isNewSlot = (0 != (methodAttributes & MethodAttributes.NewSlot));
        }

        public sealed override bool AreNamesAndSignatureEqual(MethodInfo member1, MethodInfo member2)
        {
            return AreNamesAndSignaturesEqual(member1, member2);
        }
    }

    //==========================================================================================================================
    // Policies for properties.
    //==========================================================================================================================
    internal sealed class PropertyPolicies : MemberPolicies<PropertyInfo>
    {
        public sealed override IEnumerable<PropertyInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredProperties;
        }

        public sealed override void GetMemberAttributes(PropertyInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            MethodInfo accessorMethod = GetAccessorMethod(member);
            MethodAttributes methodAttributes = accessorMethod.Attributes;
            visibility = methodAttributes & MethodAttributes.MemberAccessMask;
            isStatic = (0 != (methodAttributes & MethodAttributes.Static));
            isVirtual = (0 != (methodAttributes & MethodAttributes.Virtual));
            isNewSlot = (0 != (methodAttributes & MethodAttributes.NewSlot));
        }

        public sealed override bool AreNamesAndSignatureEqual(PropertyInfo member1, PropertyInfo member2)
        {
            return AreNamesAndSignaturesEqual(GetAccessorMethod(member1), GetAccessorMethod(member2));
        }

        public sealed override PropertyInfo GetInheritedMemberInfo(PropertyInfo underlyingMemberInfo, Type reflectedType)
        {
            return new InheritedPropertyInfo(underlyingMemberInfo, reflectedType);
        }

        private MethodInfo GetAccessorMethod(PropertyInfo property)
        {
            MethodInfo accessor = property.GetMethod;
            if (accessor == null)
            {
                accessor = property.SetMethod;
            }

            return accessor;
        }
    }

    //==========================================================================================================================
    // Policies for events.
    //==========================================================================================================================
    internal sealed class EventPolicies : MemberPolicies<EventInfo>
    {
        public sealed override IEnumerable<EventInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredEvents;
        }

        public sealed override void GetMemberAttributes(EventInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            MethodInfo accessorMethod = GetAccessorMethod(member);
            MethodAttributes methodAttributes = accessorMethod.Attributes;
            visibility = methodAttributes & MethodAttributes.MemberAccessMask;
            isStatic = (0 != (methodAttributes & MethodAttributes.Static));
            isVirtual = (0 != (methodAttributes & MethodAttributes.Virtual));
            isNewSlot = (0 != (methodAttributes & MethodAttributes.NewSlot));
        }

        public sealed override bool AreNamesAndSignatureEqual(EventInfo member1, EventInfo member2)
        {
            return AreNamesAndSignaturesEqual(GetAccessorMethod(member1), GetAccessorMethod(member2));
        }

        private MethodInfo GetAccessorMethod(EventInfo e)
        {
            MethodInfo accessor = e.AddMethod;
            return accessor;
        }
    }

    //==========================================================================================================================
    // Policies for nested types.
    //
    // Nested types enumerate a little differently than other members:
    //
    //    Base classes are never searched, regardless of BindingFlags.DeclaredOnly value.
    //
    //    Public|NonPublic|IgnoreCase are the only relevant BindingFlags. The apis ignore any other bits.
    //
    //    There is no such thing as a "static" or "instanced" nested type. For enumeration purposes,
    //    we'll arbitrarily denote all nested types as "static."
    //
    //==========================================================================================================================
    internal sealed class NestedTypePolicies : MemberPolicies<TypeInfo>
    {
        public sealed override IEnumerable<TypeInfo> GetDeclaredMembers(TypeInfo typeInfo)
        {
            return typeInfo.DeclaredNestedTypes;
        }

        public sealed override void GetMemberAttributes(TypeInfo member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot)
        {
            isStatic = true;
            isVirtual = false;
            isNewSlot = false;

            // Since we never search base types for nested types, we don't need to map every visibility value one to one.
            // We just need to distinguish between "public" and "everything else."
            visibility = member.IsNestedPublic ? MethodAttributes.Public : MethodAttributes.Private;
        }

        public sealed override bool AreNamesAndSignatureEqual(TypeInfo member1, TypeInfo member2)
        {
            Debug.Assert(false, "This code path should be unreachable as nested types are never \"virtual\".");
            throw new NotSupportedException();
        }

        public sealed override BindingFlags ModifyBindingFlags(BindingFlags bindingFlags)
        {
            bindingFlags &= BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            bindingFlags |= BindingFlags.Static | BindingFlags.DeclaredOnly;
            return bindingFlags;
        }
    }
}
