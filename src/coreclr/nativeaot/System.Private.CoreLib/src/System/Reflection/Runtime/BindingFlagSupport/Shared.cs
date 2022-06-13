// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Runtime.General;

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
        public static bool QualifiesBasedOnParameterCount(this MethodBase methodBase, BindingFlags bindingFlags, CallingConventions callConv, Type?[] argumentTypes)
        {
            Debug.Assert(methodBase is not null);
            Debug.Assert(argumentTypes is not null);
#if DEBUG
            bindingFlags &= ~(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase);
#endif

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

                bool testForParamArray = false;
                bool excessSuppliedArguments = argumentTypes.Length > parameterInfos.Length;

                if (excessSuppliedArguments)
                { // more supplied arguments than parameters, additional arguments could be vararg
                    #region Varargs
                    // If method is not vararg, additional arguments can not be passed as vararg
                    if ((methodBase.CallingConvention & CallingConventions.VarArgs) == 0)
                    {
                        testForParamArray = true;
                    }
                    else
                    {
                        // If Binding flags did not include varargs we would have filtered this vararg method.
                        // This Invariant established during callConv check.
                        Debug.Assert((callConv & CallingConventions.VarArgs) != 0);
                    }
                    #endregion
                }
                else
                {// fewer supplied arguments than parameters, missing arguments could be optional
                    #region OptionalParamBinding
                    if ((bindingFlags & BindingFlags.OptionalParamBinding) == 0)
                    {
                        testForParamArray = true;
                    }
                    else
                    {
                        // From our existing code, our policy here is that if a parameterInfo
                        // is optional then all subsequent parameterInfos shall be optional.

                        // Thus, iff the first parameterInfo is not optional then this MethodInfo is no longer a canidate.
                        if (!parameterInfos[argumentTypes.Length].IsOptional)
                            testForParamArray = true;
                    }
                    #endregion
                }

                #region ParamArray
                if (testForParamArray)
                {
                    if (parameterInfos.Length == 0)
                        return false;

                    // The last argument of the signature could be a param array.
                    bool shortByMoreThanOneSuppliedArgument = argumentTypes.Length < parameterInfos.Length - 1;

                    if (shortByMoreThanOneSuppliedArgument)
                        return false;

                    ParameterInfo lastParameter = parameterInfos[parameterInfos.Length - 1];

                    if (!lastParameter.ParameterType.IsArray)
                        return false;

                    if (!lastParameter.IsDefined(typeof(ParamArrayAttribute), false))
                        return false;
                }
                #endregion

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
                            if (argumentTypes[i] is not null && !argumentTypes[i].MatchesParameterTypeExactly(parameterInfos[i]))
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
            bool isVirtual;
            bool isNewSlot;
            policies.GetMemberAttributes(member, out _, out _, out isVirtual, out isNewSlot);
            if (isNewSlot || !isVirtual)
            {
                return null;
            }
            string name = member.Name;
            TypeInfo typeInfo = member.DeclaringType.GetTypeInfo();
            for (;;)
            {
                Type? baseType = typeInfo.BaseType;
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
                    bool isCandidateVirtual;
                    policies.GetMemberAttributes(member, out _, out _, out isCandidateVirtual, out _);
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
