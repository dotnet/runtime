// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    public static class TypeExtensions
    {
        // This implementation is simplistic, but sufficient for the tests in this assembly.
        public static ConstructorInfo GetConstructor(this Type _this, Type[] types)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null)
                    throw new ArgumentNullException(nameof(types));
            }

            foreach (ConstructorInfo candidate in _this.GetTypeInfo().DeclaredConstructors)
            {
                if (!candidate.IsPublic)
                    continue;
                if (candidate.IsStatic)
                    continue;
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != types.Length)
                    continue;
                bool foundMismatch = false;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType != types[i])
                    {
                        foundMismatch = true;
                        break;
                    }
                }
                if (foundMismatch)
                    continue;
                return candidate;
            }
            return null;
        }

        internal static Type GetReturnType(this MethodBase mi)
        {
            return (mi.IsConstructor) ? mi.DeclaringType : ((MethodInfo)mi).ReturnType;
        }

        // Expression trees/compiler just use IsByRef, why do we need this?
        // (see LambdaCompiler.EmitArguments for usage in the compiler)
        internal static bool IsByRefParameter(this ParameterInfo pi)
        {
            // not using IsIn/IsOut properties as they are not available in Silverlight:
            if (pi.ParameterType.IsByRef) return true;

            return (pi.Attributes & (ParameterAttributes.Out)) == ParameterAttributes.Out;
        }

        // Returns the matching method if the parameter types are reference
        // assignable from the provided type arguments, otherwise null. 
        internal static MethodInfo GetAnyStaticMethodValidated(
            this Type type,
            string name,
            Type[] types)
        {
            foreach (MethodInfo method in type.GetTypeInfo().DeclaredMethods)
            {
                if (method.IsStatic && method.Name == name && method.MatchesArgumentTypes(types))
                {
                    return method;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if the method's parameter types are reference assignable from
        /// the argument types, otherwise false.
        /// 
        /// An example that can make the method return false is that 
        /// typeof(double).GetMethod("op_Equality", ..., new[] { typeof(double), typeof(int) })
        /// returns a method with two double parameters, which doesn't match the provided
        /// argument types.
        /// </summary>
        /// <returns></returns>
        private static bool MatchesArgumentTypes(this MethodInfo mi, Type[] argTypes)
        {
            if (mi == null || argTypes == null)
            {
                return false;
            }
            var ps = mi.GetParameters();

            if (ps.Length != argTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < ps.Length; i++)
            {
                if (!AreReferenceAssignable(ps[i].ParameterType, argTypes[i]))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool AreEquivalent(Type t1, Type t2)
        {
            return t1 == t2;
            //            return t1 == t2 || t1.IsEquivalentTo(t2);
        }

        internal static bool AreReferenceAssignable(Type dest, Type src)
        {
            // WARNING: This actually implements "Is this identity assignable and/or reference assignable?"
            if (AreEquivalent(dest, src))
            {
                return true;
            }
            if (!dest.GetTypeInfo().IsValueType && !src.GetTypeInfo().IsValueType && dest.GetTypeInfo().IsAssignableFrom(src.GetTypeInfo()))
            {
                return true;
            }
            return false;
        }
    }
}
