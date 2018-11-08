// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace System.Reflection.Tests
{
    internal static class NetCoreApiEmulators
    {
#if !netstandard
        public static bool IsSZArray(this Type t) => t.IsSZArray;
        public static bool IsVariableBoundArray(this Type t) => t.IsVariableBoundArray;
        public static bool IsTypeDefinition(this Type t) => t.IsTypeDefinition;
        public static bool IsGenericTypeParameter(this Type t) => t.IsGenericTypeParameter;
        public static bool IsGenericMethodParameter(this Type t) => t.IsGenericMethodParameter;
        public static bool IsConstructedGenericMethod(this MethodBase m) => m.IsConstructedGenericMethod;
        public static bool IsByRefLike(this Type t) => t.IsByRefLike;
        public static Type[] GetForwardedTypesThunk(this Assembly a) => a.GetForwardedTypes();
#else
        public static bool IsSZArray(this Type t) => t.IsImplementedByRuntime() ? (t.IsArray && t.GetArrayRank() == 1 && t.Name.EndsWith("[]")) : t.CallUsingReflection<bool>("get_IsSZArray");
        public static bool IsVariableBoundArray(this Type t) => t.IsImplementedByRuntime() ? (t.IsArray && !t.IsSZArray()) : t.CallUsingReflection<bool>("get_IsVariableBoundArray");
        public static bool IsTypeDefinition(this Type t) => t.IsImplementedByRuntime() ? (!(t.HasElementType || t.IsConstructedGenericType || t.IsGenericParameter)) : t.CallUsingReflection<bool>("get_IsTypeDefinition");
        public static bool IsGenericTypeParameter(this Type t) => t.IsImplementedByRuntime() ? (t.IsGenericParameter && t.DeclaringMethod == null) : t.CallUsingReflection<bool>("get_IsGenericTypeParameter");
        public static bool IsGenericMethodParameter(this Type t) => t.IsImplementedByRuntime() ? (t.IsGenericParameter && t.DeclaringMethod != null) : t.CallUsingReflection<bool>("get_IsGenericMethodParameter");
        public static bool IsConstructedGenericMethod(this MethodBase m) => m.IsImplementedByRuntime() ? (m.IsGenericMethod && !m.IsGenericMethodDefinition) : m.CallUsingReflection<bool>("get_IsConstructedGenericMethod");

        public static bool IsByRefLike(this Type t)
        {
            if (t.IsImplementedByRuntime())
                return t.CustomAttributes.Any(cad => cad.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute");

            return t.CallUsingReflection<bool>("get_IsByRefLike");
        }

        public static Type[] GetForwardedTypesThunk(this Assembly a) => a.CallUsingReflection<Type[]>("GetForwardedTypes");
#endif

        //
        // This is something no app should be doing but it does allow us to get full code coverage even when running tests on NETFX.
        // NETFX can't directly invoke the Reflection apis added in NetCore, but we do know the underlying MetadataLoadContext Reflection objects
        // implement the full NetCore set even in its NetStandard. We just have to do a little sneaky Reflection to get to it.
        //
        public static T CallUsingReflection<T>(this object _this, string name, Type[] parameterTypes = null, object[] arguments = null)
        {
            parameterTypes = parameterTypes ?? Array.Empty<Type>();
            arguments = arguments ?? Array.Empty<object>();
            Type implementationType = _this.GetType();
            MethodInfo m = implementationType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.ExactBinding, null, parameterTypes, null);
            if (m == null)
                throw new Exception("This Reflection provider does not support this method: " + name);

            try
            {
                return (T)(m.Invoke(_this, arguments));
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }
    }
}
