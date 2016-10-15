// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Dynamic.Utils
{
    // Extensions on System.Type and friends
    internal static partial class TypeExtensions
    {
        private static readonly CacheDict<MethodBase, ParameterInfo[]> s_paramInfoCache = new CacheDict<MethodBase, ParameterInfo[]>(75);

        internal static ParameterInfo[] GetParametersCached(this MethodBase method)
        {
            ParameterInfo[] pis;
            CacheDict<MethodBase, ParameterInfo[]> pic = s_paramInfoCache;
            if (!pic.TryGetValue(method, out pis))
            {
                pis = method.GetParameters();

                Type t = method.DeclaringType;
                if (t != null && TypeUtils.CanCache(t))
                {
                    pic[method] = pis;
                }
            }

            return pis;
        }


        public static bool IsSubclassOf(this Type source, Type other)
        {
            return source.GetTypeInfo().IsSubclassOf(other);
        }

#if FEATURE_COMPILE
        // Expression trees/compiler just use IsByRef, why do we need this?
        // (see LambdaCompiler.EmitArguments for usage in the compiler)
        internal static bool IsByRefParameter(this ParameterInfo pi)
        {
            // not using IsIn/IsOut properties as they are not available in Silverlight:
            if (pi.ParameterType.IsByRef) return true;

            return (pi.Attributes & (ParameterAttributes.Out)) == ParameterAttributes.Out;
        }
#endif 
    }
}
