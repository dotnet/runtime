// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace System.Dynamic.Utils
{
    // Extensions on System.Type and friends
    internal static partial class TypeExtensions
    {
        /// <summary>
        /// Returns the matching method if the parameter types are reference
        /// assignable from the provided type arguments, otherwise null.
        /// </summary>
        public static MethodInfo GetAnyStaticMethodValidated(this Type type, string name, Type[] types)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.Name == name && method.MatchesArgumentTypes(types))
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
        private static bool MatchesArgumentTypes(this MethodInfo mi, Type[] argTypes)
        {
            if (mi == null || argTypes == null)
            {
                return false;
            }

            ParameterInfo[] ps = mi.GetParametersCached();

            if (ps.Length != argTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < ps.Length; i++)
            {
                if (!TypeUtils.AreReferenceAssignable(ps[i].ParameterType, argTypes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static Type GetReturnType(this MethodBase mi)
        {
            return (mi.IsConstructor) ? mi.DeclaringType : ((MethodInfo)mi).ReturnType;
        }

        public static TypeCode GetTypeCode(this Type type) => Type.GetTypeCode(type);
    }
}
