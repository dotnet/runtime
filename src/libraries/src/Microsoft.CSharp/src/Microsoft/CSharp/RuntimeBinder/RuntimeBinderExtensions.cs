// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal static class RuntimeBinderExtensions
    {
        public static bool IsNullableType(this Type type)
        {
            return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        // This method is intended as a means to detect when MemberInfos are the same,
        // modulo the fact that they can appear to have different but equivalent local
        // No-PIA types. It is by the symbol table to determine whether
        // or not members have been added to an AggSym or not.
        public static bool IsEquivalentTo(this MemberInfo mi1, MemberInfo mi2)
        {
            if (mi1 == null || mi2 == null)
            {
                return mi1 == null && mi2 == null;
            }

#if UNSUPPORTEDAPI
            if (mi1 == mi2 || (mi1.DeclaringType.IsGenericallyEqual(mi2.DeclaringType) && mi1.MetadataToken == mi2.MetadataToken))
#else
            if (mi1.Equals(mi2))
#endif
            {
                return true;
            }

            if (mi1 is MethodInfo method1)
            {
                if (!(mi2 is MethodInfo method2) || method1.IsGenericMethod != method2.IsGenericMethod)
                {
                    return false;
                }

                if (method1.IsGenericMethod)
                {
                    method1 = method1.GetGenericMethodDefinition();
                    method2 = method2.GetGenericMethodDefinition();

                    if (method1.GetGenericArguments().Length != method2.GetGenericArguments().Length)
                    {
                        return false; // Methods of different arity are not equivalent.
                    }
                }

                return method1 != method2
                    && method1.CallingConvention == method2.CallingConvention
                    && method1.Name == method2.Name
                    && method1.DeclaringType.IsGenericallyEqual(method2.DeclaringType)
                    && method1.ReturnType.IsGenericallyEquivalentTo(method2.ReturnType, method1, method2)
                    && method1.AreParametersEquivalent(method2);
            }

            if (mi1 is ConstructorInfo ctor1)
            {
                return mi2 is ConstructorInfo ctor2
                    && ctor1 != ctor2
                    && ctor1.CallingConvention == ctor2.CallingConvention
                    && ctor1.DeclaringType.IsGenericallyEqual(ctor2.DeclaringType)
                    && ctor1.AreParametersEquivalent(ctor2);
            }

            return mi1 is PropertyInfo prop1 && mi2 is PropertyInfo prop2
                && prop1 != prop2
                && prop1.Name == prop2.Name
                && prop1.DeclaringType.IsGenericallyEqual(prop2.DeclaringType)
                && prop1.PropertyType.IsGenericallyEquivalentTo(prop2.PropertyType, prop1, prop2)
                && prop1.GetGetMethod(true).IsEquivalentTo(prop2.GetGetMethod(true))
                && prop1.GetSetMethod(true).IsEquivalentTo(prop2.GetSetMethod(true));
        }

        private static bool AreParametersEquivalent(this MethodBase method1, MethodBase method2)
        {
            ParameterInfo[] pis1 = method1.GetParameters();
            ParameterInfo[] pis2 = method2.GetParameters();
            if (pis1.Length != pis2.Length)
            {
                return false;
            }

            for (int i = 0; i < pis1.Length; ++i)
            {
                if (!pis1[i].IsEquivalentTo(pis2[i], method1, method2))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsEquivalentTo(this ParameterInfo pi1, ParameterInfo pi2, MethodBase method1, MethodBase method2)
        {
            if (pi1 == null || pi2 == null)
            {
                return pi1 == null && pi2 == null;
            }

            if (pi1.Equals(pi2))
            {
                return true;
            }

            return pi1.ParameterType.IsGenericallyEquivalentTo(pi2.ParameterType, method1, method2);
        }

        private static bool IsGenericallyEqual(this Type t1, Type t2)
        {
            if (t1 == null || t2 == null)
            {
                return t1 == null && t2 == null;
            }

            if (t1.Equals(t2))
            {
                return true;
            }

            if (t1.IsConstructedGenericType || t2.IsConstructedGenericType)
            {
                Type t1def = t1.IsConstructedGenericType ? t1.GetGenericTypeDefinition() : t1;
                Type t2def = t2.IsConstructedGenericType ? t2.GetGenericTypeDefinition() : t2;

                return t1def.Equals(t2def);
            }

            return false;
        }

        // Compares two types and calls them equivalent if a type parameter equals a type argument.
        // i.e if the inputs are (T, int, C<T>, C<int>) then this will return true.
        private static bool IsGenericallyEquivalentTo(this Type t1, Type t2, MemberInfo member1, MemberInfo member2)
        {
            Debug.Assert(!(member1 is MethodBase) ||
                         !((MethodBase)member1).IsGenericMethod ||
                         (((MethodBase)member1).IsGenericMethodDefinition && ((MethodBase)member2).IsGenericMethodDefinition));

            if (t1.Equals(t2))
            {
                return true;
            }

            // If one of them is a type param and then the other is a real type, then get the type argument in the member
            // or it's declaring type that corresponds to the type param and compare that to the other type.
            if (t1.IsGenericParameter)
            {
                if (t2.IsGenericParameter)
                {
                    // If member's declaring type is not type parameter's declaring type, we assume that it is used as a type argument
                    if (t1.DeclaringMethod == null && member1.DeclaringType.Equals(t1.DeclaringType))
                    {
                        if (!(t2.DeclaringMethod == null && member2.DeclaringType.Equals(t2.DeclaringType)))
                        {
                            return t1.IsTypeParameterEquivalentToTypeInst(t2, member2);
                        }
                    }
                    else if (t2.DeclaringMethod == null && member2.DeclaringType.Equals(t2.DeclaringType))
                    {
                        return t2.IsTypeParameterEquivalentToTypeInst(t1, member1);
                    }

                    // If both of these are type params but didn't compare to be equal then one of them is likely bound to another
                    // open type. Simply disallow such cases.
                    return false;
                }

                return t1.IsTypeParameterEquivalentToTypeInst(t2, member2);
            }
            else if (t2.IsGenericParameter)
            {
                return t2.IsTypeParameterEquivalentToTypeInst(t1, member1);
            }

            // Recurse in for generic types arrays, byref and pointer types.
            if (t1.IsGenericType && t2.IsGenericType)
            {
                var args1 = t1.GetGenericArguments();
                var args2 = t2.GetGenericArguments();

                if (args1.Length == args2.Length)
                {
                    return t1.IsGenericallyEqual(t2) &&
                           Enumerable.All(Enumerable.Zip(args1, args2, (ta1, ta2) => ta1.IsGenericallyEquivalentTo(ta2, member1, member2)), x => x);
                }
            }

            if (t1.IsArray && t2.IsArray)
            {
                return t1.GetArrayRank() == t2.GetArrayRank() &&
                       t1.GetElementType().IsGenericallyEquivalentTo(t2.GetElementType(), member1, member2);
            }

            if ((t1.IsByRef && t2.IsByRef) ||
                (t1.IsPointer && t2.IsPointer))
            {
                return t1.GetElementType().IsGenericallyEquivalentTo(t2.GetElementType(), member1, member2);
            }

            return false;
        }

        private static bool IsTypeParameterEquivalentToTypeInst(this Type typeParam, Type typeInst, MemberInfo member)
        {
            Debug.Assert(typeParam.IsGenericParameter);

            if (typeParam.DeclaringMethod != null)
            {
                // The type param is from a generic method. Since only methods can be generic, anything else
                // here means they are not equivalent.
                if (!(member is MethodBase))
                {
                    return false;
                }

                MethodBase method = (MethodBase)member;
                int position = typeParam.GenericParameterPosition;
                Type[] args = method.IsGenericMethod ? method.GetGenericArguments() : null;

                return args != null &&
                       args.Length > position &&
                       args[position].Equals(typeInst);
            }
            else
            {
                return member.DeclaringType.GetGenericArguments()[typeParam.GenericParameterPosition].Equals(typeInst);
            }
        }

        // s_MemberEquivalence will replace itself with one version or another
        // depending on what works at run time
        private static Func<MemberInfo, MemberInfo, bool> s_MemberEquivalence = (m1, m2) =>
            {
                try
                {
                    Type memberInfo = typeof(MemberInfo);

                    // First, try the actual API. (Post .NetCore 2.0) The api is the only one that gets it completely right on frameworks without MetadataToken.
                    MethodInfo apiMethod = memberInfo.GetMethod(
                        "HasSameMetadataDefinitionAs",
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding,
                        binder: null,
                        types: new Type[] { typeof(MemberInfo) },
                        modifiers: null);
                    if (apiMethod != null)
                    {
                        Func<MemberInfo, MemberInfo, bool> apiDelegate = (Func<MemberInfo, MemberInfo, bool>)(apiMethod.CreateDelegate(typeof(Func<MemberInfo, MemberInfo, bool>)));
                        try
                        {
                            bool result = apiDelegate(m1, m2);
                            // it worked, so publish it
                            s_MemberEquivalence = apiDelegate;
                            return result;
                        }
                        catch
                        {
                            // Api found but apparently stubbed as not supported. Continue on to the next fallback...
                        }
                    }

                    // See if MetadataToken property is available.
                    PropertyInfo property = memberInfo.GetProperty("MetadataToken", typeof(int), Array.Empty<Type>());

                    if ((object)property != null && property.CanRead)
                    {
                        // (parameter1, parameter2) => parameter1.MetadataToken == parameter2.MetadataToken
                        var parameter1 = Expression.Parameter(memberInfo);
                        var parameter2 = Expression.Parameter(memberInfo);
                        var memberEquivalence = Expression.Lambda<Func<MemberInfo, MemberInfo, bool>>(
                            Expression.Equal(
                                Expression.Property(parameter1, property),
                                Expression.Property(parameter2, property)),
                                parameter1, parameter2).Compile();

                        var result = memberEquivalence(m1, m2);
                        // it worked, so publish it
                        s_MemberEquivalence = memberEquivalence;

                        return result;
                    }
                }
                catch
                {
                    // Platform might not allow access to the property
                }

                // MetadataToken is not available in some contexts. Looks like this is one of those cases.
                // fallback to "IsEquivalentTo"
                Func<MemberInfo, MemberInfo, bool> fallbackMemberEquivalence = (m1param, m2param) => m1param.IsEquivalentTo(m2param);

                // fallback must work 
                s_MemberEquivalence = fallbackMemberEquivalence;
                return fallbackMemberEquivalence(m1, m2);
            };

        public static bool HasSameMetadataDefinitionAs(this MemberInfo mi1, MemberInfo mi2)
        {
#if UNSUPPORTEDAPI
            return (mi1.MetadataToken == mi2.MetadataToken) && (mi1.Module == mi2.Module));
#else
            return mi1.Module.Equals(mi2.Module) && s_MemberEquivalence(mi1, mi2);
#endif
        }

        public static string GetIndexerName(this Type type)
        {
            Debug.Assert(type != null);
            string name = GetTypeIndexerName(type);
            if (name == null && type.IsInterface)
            {
                foreach (Type iface in type.GetInterfaces())
                {
                    name = GetTypeIndexerName(iface);
                    if (name != null)
                    {
                        break;
                    }
                }
            }

            return name;
        }

        private static string GetTypeIndexerName(Type type)
        {
            Debug.Assert(type != null);
            string name = type.GetCustomAttribute<DefaultMemberAttribute>()?.MemberName;
            if (name != null)
            {
                if (type.GetProperties(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                    .Any(p => p.Name == name && p.GetIndexParameters().Length != 0))
                {
                    return name;
                }
            }

            return null;
        }
    }
}
