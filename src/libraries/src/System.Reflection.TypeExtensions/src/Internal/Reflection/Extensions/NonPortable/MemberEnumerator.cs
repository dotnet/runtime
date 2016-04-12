// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Collections.Generic;

namespace Internal.Reflection.Extensions.NonPortable
{
    public static class MemberEnumerator
    {
        //
        // Enumerates members, optionally filtered by a name, in the given class and its base classes (but not implemented interfaces.)
        // Basically emulates the old Type.GetFoo(BindingFlags) api.
        //
        public static IEnumerable<M> GetMembers<M>(this Type type, Object nameFilterOrAnyName, BindingFlags bindingFlags) where M : MemberInfo
        {
            // Do all the up-front argument validation here so that the exception occurs on call rather than on the first move.
            if (type == null)
            {
                throw new ArgumentNullException();
            }
            if (nameFilterOrAnyName == null)
            {
                throw new ArgumentNullException();
            }

            String optionalNameFilter;
            if (nameFilterOrAnyName == AnyName)
            {
                optionalNameFilter = null;
            }
            else
            {
                optionalNameFilter = (String)nameFilterOrAnyName;
            }

            return GetMembersWorker<M>(type, optionalNameFilter, bindingFlags);
        }

        //
        // The iterator worker for GetMember<M>()
        //
        private static IEnumerable<M> GetMembersWorker<M>(Type type, String optionalNameFilter, BindingFlags bindingFlags) where M : MemberInfo
        {
            Type reflectedType = type;
            Type typeOfM = typeof(M);
            Type typeOfEventInfo = typeof(EventInfo);

            MemberPolicies<M> policies = MemberPolicies<M>.Default;
            bindingFlags = policies.ModifyBindingFlags(bindingFlags);

            LowLevelList<M> overridingMembers = new LowLevelList<M>();

            StringComparison comparisonType = (0 != (bindingFlags & BindingFlags.IgnoreCase)) ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
            bool inBaseClass = false;

            bool nameFilterIsPrefix = false;
            if (optionalNameFilter != null && optionalNameFilter.EndsWith("*", StringComparison.Ordinal))
            {
                nameFilterIsPrefix = true;
                optionalNameFilter = optionalNameFilter.Substring(0, optionalNameFilter.Length - 1);
            }

            while (type != null)
            {
                TypeInfo typeInfo = type.GetTypeInfo();

                foreach (M member in policies.GetDeclaredMembers(typeInfo))
                {
                    if (optionalNameFilter != null)
                    {
                        if (nameFilterIsPrefix)
                        {
                            if (!member.Name.StartsWith(optionalNameFilter, comparisonType))
                            {
                                continue;
                            }
                        }
                        else if (!member.Name.Equals(optionalNameFilter, comparisonType))
                        {
                            continue;
                        }
                    }

                    MethodAttributes visibility;
                    bool isStatic;
                    bool isVirtual;
                    bool isNewSlot;
                    policies.GetMemberAttributes(member, out visibility, out isStatic, out isVirtual, out isNewSlot);

                    BindingFlags memberBindingFlags = (BindingFlags)0;
                    memberBindingFlags |= (isStatic ? BindingFlags.Static : BindingFlags.Instance);
                    memberBindingFlags |= ((visibility == MethodAttributes.Public) ? BindingFlags.Public : BindingFlags.NonPublic);
                    if ((bindingFlags & memberBindingFlags) != memberBindingFlags)
                    {
                        continue;
                    }

                    bool passesVisibilityScreen = true;
                    if (inBaseClass && visibility == MethodAttributes.Private)
                    {
                        passesVisibilityScreen = false;
                    }

                    bool passesStaticScreen = true;
                    if (inBaseClass && isStatic && (0 == (bindingFlags & BindingFlags.FlattenHierarchy)))
                    {
                        passesStaticScreen = false;
                    }

                    //
                    // Desktop compat: The order in which we do checks is important.
                    //
                    if (!passesVisibilityScreen)
                    {
                        continue;
                    }
                    if ((!passesStaticScreen) && !(typeOfM.Equals(typeOfEventInfo)))
                    {
                        continue;
                    }

                    bool isImplicitlyOverridden = false;
                    if (isVirtual)
                    {
                        if (isNewSlot)
                        {
                            // A new virtual member definition.
                            for (int i = 0; i < overridingMembers.Count; i++)
                            {
                                if (policies.AreNamesAndSignatureEqual(member, overridingMembers[i]))
                                {
                                    // This member is overridden by a more derived class.
                                    isImplicitlyOverridden = true;
                                    overridingMembers.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < overridingMembers.Count; i++)
                            {
                                if (policies.AreNamesAndSignatureEqual(overridingMembers[i], member))
                                {
                                    // This member overrides another, *and* is overridden by yet another method.
                                    isImplicitlyOverridden = true;
                                    break;
                                }
                            }

                            if (!isImplicitlyOverridden)
                            {
                                // This member overrides another and is the most derived instance of it we've found.
                                overridingMembers.Add(member);
                            }
                        }
                    }

                    if (isImplicitlyOverridden)
                    {
                        continue;
                    }

                    if (!passesStaticScreen)
                    {
                        continue;
                    }

                    if (inBaseClass)
                    {
                        yield return policies.GetInheritedMemberInfo(member, reflectedType);
                    }
                    else
                    {
                        yield return member;
                    }
                }

                if (0 != (bindingFlags & BindingFlags.DeclaredOnly))
                {
                    break;
                }

                inBaseClass = true;
                type = typeInfo.BaseType;
            }
        }

        //
        // If member is a virtual member that implicitly overrides a member in a base class, return the overridden member.
        // Otherwise, return null.
        //
        // - MethodImpls ignored. (I didn't say it made sense, this is just how the desktop api we're porting behaves.)
        // - Implemented interfaces ignores. (I didn't say it made sense, this is just how the desktop api we're porting behaves.) 
        //
        public static M GetImplicitlyOverriddenBaseClassMember<M>(this M member) where M : MemberInfo
        {
            MemberPolicies<M> policies = MemberPolicies<M>.Default;
            MethodAttributes visibility;
            bool isStatic;
            bool isVirtual;
            bool isNewSlot;
            policies.GetMemberAttributes(member, out visibility, out isStatic, out isVirtual, out isNewSlot);
            if (isNewSlot || !isVirtual)
            {
                return null;
            }
            String name = member.Name;
            TypeInfo typeInfo = member.DeclaringType.GetTypeInfo();
            for (; ;)
            {
                Type baseType = typeInfo.BaseType;
                if (baseType == null)
                {
                    return null;
                }
                typeInfo = baseType.GetTypeInfo();
                foreach (M candidate in policies.GetDeclaredMembers(typeInfo))
                {
                    if (candidate.Name != name)
                    {
                        continue;
                    }
                    MethodAttributes candidateVisibility;
                    bool isCandidateStatic;
                    bool isCandidateVirtual;
                    bool isCandidateNewSlot;
                    policies.GetMemberAttributes(member, out candidateVisibility, out isCandidateStatic, out isCandidateVirtual, out isCandidateNewSlot);
                    if (!isCandidateVirtual)
                    {
                        continue;
                    }
                    if (!policies.AreNamesAndSignatureEqual(member, candidate))
                    {
                        continue;
                    }
                    return candidate;
                }
            }
        }

        // Uniquely allocated sentinel "string"
        //  - can't use null as that may be an app-supplied null, which we have to throw ArgumentNullException for.
        //  - risky to use a proper String as the FX or toolchain can unexpectedly give you back a shared string
        //    even when you'd swear you were allocating a new one.
        public static readonly Object AnyName = new Object();
    }
}
