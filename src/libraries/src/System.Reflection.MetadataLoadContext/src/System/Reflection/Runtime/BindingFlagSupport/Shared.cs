// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection.TypeLoading;

namespace System.Reflection.Runtime.BindingFlagSupport
{
    internal static class Shared
    {
        //
        // This is similar to FilterApplyMethodBase from CoreClr with some important differences:
        //
        //   - Does *not* filter on Public|NonPublic|Instance|Static|FlatternHierarchy. Caller is expected to have done that.
        //
        //   - ArgumentTypes cannot be null.
        //
        // Used by Type.GetMethodImpl(), Type.GetConstructorImpl(), Type.InvokeMember() and Activator.CreateInstance(). Does some
        // preliminary weeding out of candidate methods based on the supplied calling convention and parameter list lengths.
        //
        // Candidates must pass this screen before we involve the binder.
        //
        public static bool QualifiesBasedOnParameterCount(this MethodBase methodBase, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
        {
            Debug.Assert(methodBase != null);
            Debug.Assert(argumentTypes != null);

            #region Check CallingConvention
            if ((callConv & CallingConventions.Any) == 0)
            {
                if ((callConv & CallingConventions.VarArgs) != 0 && (methodBase.CallingConvention & CallingConventions.VarArgs) == 0)
                    return false;

                if ((callConv & CallingConventions.Standard) != 0 && (methodBase.CallingConvention & CallingConventions.Standard) == 0)
                    return false;
            }
            #endregion

            #region ArgumentTypes
            ParameterInfo[] parameterInfos = methodBase.GetParametersNoCopy();

            if (argumentTypes.Length != parameterInfos.Length)
            {
                #region Invoke Member, Get\Set & Create Instance specific case
                // If the number of supplied arguments differs than the number in the signature AND
                // we are not filtering for a dynamic call -- InvokeMethod or CreateInstance -- filter out the method.
                if ((bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.CreateInstance | BindingFlags.GetProperty | BindingFlags.SetProperty)) == 0)
                    return false;

                throw new InvalidOperationException(SR.NoInvokeMember);
#endregion
            }
            else
            {
#region Exact Binding
                if ((bindingFlags & BindingFlags.ExactBinding) != 0)
                {
                    // Legacy behavior is to ignore ExactBinding when InvokeMember is specified.
                    // Why filter by InvokeMember? If the answer is we leave this to the binder then why not leave
                    // all the rest of this  to the binder too? Further, what other semanitc would the binder
                    // use for BindingFlags.ExactBinding besides this one? Further, why not include CreateInstance 
                    // in this if statement? That's just InvokeMethod with a constructor, right?
                    if ((bindingFlags & (BindingFlags.InvokeMethod)) == 0)
                    {
                        for (int i = 0; i < parameterInfos.Length; i++)
                        {
                            // a null argument type implies a null arg which is always a perfect match
                            if (!(argumentTypes[i] is null) && !argumentTypes[i].MatchesParameterTypeExactly(parameterInfos[i]))
                                return false;
                        }
                    }
                }
#endregion
            }
#endregion

            return true;
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
            policies.GetMemberAttributes(member, out MethodAttributes visibility, out bool isStatic, out bool isVirtual, out bool isNewSlot);
            if (isNewSlot || !isVirtual)
            {
                return null;
            }
            string name = member.Name;
            TypeInfo typeInfo = member.DeclaringType.GetTypeInfo();
            for (;;)
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
                    policies.GetMemberAttributes(member, out MethodAttributes candidateVisibility, out bool isCandidateStatic, out bool isCandidateVirtual, out bool isCandidateNewSlot);
                    if (!isCandidateVirtual)
                    {
                        continue;
                    }
                    if (!policies.ImplicitlyOverrides(candidate, member))
                    {
                        continue;
                    }
                    return candidate;
                }
            }
        }
    }
}
