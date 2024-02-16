// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.TypeLoading;
using RuntimeTypeInfo = System.Reflection.TypeLoading.RoType;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    /// <summary>
    /// This class encapsulates the minimum set of arcane .NET Framework CLR policies needed to implement the Get*(BindingFlags) apis.
    /// In particular, it encapsulates behaviors such as what exactly determines the "visibility" of a property and event, and
    /// what determines whether and how they are overridden.
    /// </summary>
    internal abstract class MemberPolicies<M> where M : MemberInfo
    {
        // Subclasses for specific MemberInfo types must override these:

        //
        // Returns all of the directly declared members on the given TypeInfo.
        //
        public abstract IEnumerable<M> GetDeclaredMembers(TypeInfo typeInfo);

        //
        // Returns all of the directly declared members on the given TypeInfo whose name matches filter. If filter is null,
        // returns all directly declared members.
        //
        public abstract IEnumerable<M> CoreGetDeclaredMembers(RuntimeTypeInfo type, NameFilter? filter, RuntimeTypeInfo reflectedType);

        //
        // Policy to decide whether a member is considered "virtual", "virtual new" and what its member visibility is.
        // (For "visibility", we reuse the MethodAttributes enum since Reflection lacks an element-agnostic enum for this.
        //  Only the MemberAccessMask bits are set.)
        //
        public abstract void GetMemberAttributes(M member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot);

        //
        // Policy to decide whether "derivedMember" is a virtual override of "baseMember." Used to implement MethodInfo.GetBaseDefinition(),
        // parent chain traversal for discovering inherited custom attributes, and suppressing lookup results in the Type.Get*() api family.
        //
        // Does not consider explicit overrides (methodimpls.) Does not consider "overrides" of interface methods.
        //
        public abstract bool ImplicitlyOverrides(M? baseMember, M? derivedMember);

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
        // Policy to decide if BindingFlags is always interpreted as having set DeclaredOnly.
        //
        public abstract bool AlwaysTreatAsDeclaredOnly { get; }

        //
        // Policy to decide how or if members in more derived types hide same-named members in base types.
        // Due to .NET Framework compat concerns, the definitions are a bit more arbitrary than we'd like.
        //
        public abstract bool IsSuppressedByMoreDerivedMember(M member, M[] priorMembers, int startIndex, int endIndex);

        //
        // Policy to decide whether to throw an AmbiguousMatchException on an ambiguous Type.Get*() call.
        // Does not apply to GetConstructor/GetMethod/GetProperty calls that have a non-null Type[] array passed to it.
        //
        // If method returns true, the Get() api will pick the member that's in the most derived type.
        // If method returns false, the Get() api throws AmbiguousMatchException.
        //
        public abstract bool OkToIgnoreAmbiguity(M m1, M m2);

        //
        // Helper method for determining whether two methods are signature-compatible.
        //
        protected static bool AreNamesAndSignaturesEqual(MethodInfo method1, MethodInfo method2)
        {
            if (method1.Name != method2.Name)
                return false;

            ParameterInfo[] p1 = method1.GetParametersNoCopy();
            ParameterInfo[] p2 = method2.GetParametersNoCopy();
            if (p1.Length != p2.Length)
                return false;

            bool isGenericMethod1 = method1.IsGenericMethodDefinition;
            bool isGenericMethod2 = method2.IsGenericMethodDefinition;
            if (isGenericMethod1 != isGenericMethod2)
                return false;
            if (!isGenericMethod1)
            {
                for (int i = 0; i < p1.Length; i++)
                {
                    Type parameterType1 = p1[i].ParameterType;
                    Type parameterType2 = p2[i].ParameterType;
                    if (!(parameterType1.Equals(parameterType2)))
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (method1.GetGenericArguments().Length != method2.GetGenericArguments().Length)
                    return false;
                for (int i = 0; i < p1.Length; i++)
                {
                    Type parameterType1 = p1[i].ParameterType;
                    Type parameterType2 = p2[i].ParameterType;
                    if (!GenericMethodAwareAreParameterTypesEqual(parameterType1, parameterType2))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        //
        // This helper compares the types of the corresponding parameters of two methods to see if one method is signature equivalent to the other.
        // This is needed when comparing the signatures of two generic methods as Type.Equals() is not up to that job.
        //
        private static bool GenericMethodAwareAreParameterTypesEqual(Type t1, Type t2)
        {
            // Fast-path - if Reflection has already deemed them equivalent, we can trust its result.
            if (t1.Equals(t2))
                return true;

            // If we got here, Reflection determined the types not equivalent. Most of the time, that's the result we want.
            // There is however, one wrinkle. If the type is or embeds a generic method parameter type, Reflection will always report them
            // non-equivalent, since generic parameter type comparison always compares both the position and the declaring method. For our purposes, though,
            // we only want to consider the position.

            // Fast-path: if the types don't embed any generic parameters, we can go ahead and use Reflection's result.
            if (!(t1.ContainsGenericParameters && t2.ContainsGenericParameters))
                return false;

            if ((t1.IsArray && t2.IsArray) || (t1.IsByRef && t2.IsByRef) || (t1.IsPointer && t2.IsPointer))
            {
                if (t1.IsSZArray() != t2.IsSZArray())
                    return false;

                if (t1.IsArray && (t1.GetArrayRank() != t2.GetArrayRank()))
                    return false;

                return GenericMethodAwareAreParameterTypesEqual(t1.GetElementType()!, t2.GetElementType()!);
            }

            if (t1.IsConstructedGenericType)
            {
                // We can use regular old Equals() rather than recursing into GenericMethodAwareAreParameterTypesEqual() since the
                // generic type definition will always be a plain old named type and won't embed any generic method parameters.
                if (!(t1.GetGenericTypeDefinition().Equals(t2.GetGenericTypeDefinition())))
                    return false;

                Type[] ga1 = t1.GenericTypeArguments;
                Type[] ga2 = t2.GenericTypeArguments;
                if (ga1.Length != ga2.Length)
                    return false;

                for (int i = 0; i < ga1.Length; i++)
                {
                    if (!GenericMethodAwareAreParameterTypesEqual(ga1[i], ga2[i]))
                        return false;
                }
                return true;
            }

            if (t1.IsGenericMethodParameter() && t2.IsGenericMethodParameter())
            {
                // A generic method parameter. The DeclaringMethods will be different but we don't care about that - we can assume that
                // the declaring method will be the method that declared the parameter's whose type we're testing. We only need to
                // compare the positions.
                return t1.GenericParameterPosition == t2.GenericParameterPosition;
            }

            // If we got here, either t1 and t2 are different flavors of types or they are both simple named types or both generic type parameters.
            // Either way, we can trust Reflection's result here.
            return false;
        }

#pragma warning disable CA1810 // explicit static cctor
        static MemberPolicies()
        {
            Type t = typeof(M);
            if (t.Equals(typeof(FieldInfo)))
            {
                MemberTypeIndex = BindingFlagSupport.MemberTypeIndex.Field;
                Default = (MemberPolicies<M>)(object)(new FieldPolicies());
            }
            else if (t.Equals(typeof(MethodInfo)))
            {
                MemberTypeIndex = BindingFlagSupport.MemberTypeIndex.Method;
                Default = (MemberPolicies<M>)(object)(new MethodPolicies());
            }
            else if (t.Equals(typeof(ConstructorInfo)))
            {
                MemberTypeIndex = BindingFlagSupport.MemberTypeIndex.Constructor;
                Default = (MemberPolicies<M>)(object)(new ConstructorPolicies());
            }
            else if (t.Equals(typeof(PropertyInfo)))
            {
                MemberTypeIndex = BindingFlagSupport.MemberTypeIndex.Property;
                Default = (MemberPolicies<M>)(object)(new PropertyPolicies());
            }
            else if (t.Equals(typeof(EventInfo)))
            {
                MemberTypeIndex = BindingFlagSupport.MemberTypeIndex.Event;
                Default = (MemberPolicies<M>)(object)(new EventPolicies());
            }
            else if (t.Equals(typeof(Type)))
            {
                MemberTypeIndex = BindingFlagSupport.MemberTypeIndex.NestedType;
                Default = (MemberPolicies<M>)(object)(new NestedTypePolicies());
            }
            else
            {
                Debug.Fail("Unknown MemberInfo type.");
            }
        }
#pragma warning restore CA1810

        //
        // This is a singleton class one for each MemberInfo category: Return the appropriate one.
        //
        public static readonly MemberPolicies<M> Default = null!;

        //
        // This returns a fixed value from 0 to MemberIndex.Count-1 with each possible type of M
        // being assigned a unique index (see the MemberTypeIndex for possible values). This is useful
        // for converting a type reference to M to an array index or switch case label.
        //
        public static readonly int MemberTypeIndex;
    }
}
