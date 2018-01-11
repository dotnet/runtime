// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Dynamic.Utils
{
    // Extensions on System.Type and friends
    internal static class TypeExtensions
    {
        private static readonly CacheDict<MethodBase, ParameterInfo[]> s_paramInfoCache = new CacheDict<MethodBase, ParameterInfo[]>(75);

        /// <summary>
        /// Returns the matching method if the parameter types are reference
        /// assignable from the provided type arguments, otherwise null.
        /// </summary>
        public static MethodInfo GetAnyStaticMethodValidated(this Type type, string name, Type[] types)
        {
            Debug.Assert(types != null);
            MethodInfo method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly, null, types, null);
            return method.MatchesArgumentTypes(types) ? method : null;
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
            Debug.Assert(argTypes != null);

            if (mi == null)
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

        public static Type GetReturnType(this MethodBase mi) => mi.IsConstructor ? mi.DeclaringType : ((MethodInfo)mi).ReturnType;

        public static TypeCode GetTypeCode(this Type type) => Type.GetTypeCode(type);
        internal static ParameterInfo[] GetParametersCached(this MethodBase method)
        {
            CacheDict<MethodBase, ParameterInfo[]> pic = s_paramInfoCache;
            if (!pic.TryGetValue(method, out ParameterInfo[] pis))
            {
                pis = method.GetParameters();
                if (method.DeclaringType?.IsCollectible == false)
                {
                    pic[method] = pis;
                }
            }

            return pis;
        }

#if FEATURE_COMPILE
        // Expression trees/compiler just use IsByRef, why do we need this?
        // (see LambdaCompiler.EmitArguments for usage in the compiler)
        internal static bool IsByRefParameter(this ParameterInfo pi)
        {
            // not using IsIn/IsOut properties as they are not available in Silverlight:
            if (pi.ParameterType.IsByRef)
                return true;

            return (pi.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out;
        }
#endif
    }
}
