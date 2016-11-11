// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Dynamic.Utils
{
    internal static partial class TypeUtils
    {
        public static bool AreEquivalent(Type t1, Type t2)
        {
            return t1.IsEquivalentTo(t2);
        }

        public static bool IsEquivalentTo(this Type t1, Type t2)
        {
            return t1 == t2;
        }

        public static bool AreReferenceAssignable(Type dest, Type src)
        {
            // This actually implements "Is this identity assignable and/or reference assignable?"
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

        public static bool IsSameOrSubclass(Type type, Type subType)
        {
            return AreEquivalent(type, subType) || subType.GetTypeInfo().IsSubclassOf(type);
        }

        public static void ValidateType(Type type, string paramName)
        {
            ValidateType(type, paramName, -1);
        }

        public static void ValidateType(Type type, string paramName, int index)
        {
            if (type != typeof(void))
            {
                // A check to avoid a bunch of reflection (currently not supported) during cctor
                if (type.GetTypeInfo().ContainsGenericParameters)
                {
                    throw type.GetTypeInfo().IsGenericTypeDefinition ? Error.TypeIsGeneric(type, paramName, index) : Error.TypeContainsGenericParameters(type, paramName, index);
                }
            }
        }

        private static Assembly s_mscorlib;

        private static Assembly _mscorlib
        {
            get
            {
                if (s_mscorlib == null)
                {
                    s_mscorlib = typeof(object).GetTypeInfo().Assembly;
                }

                return s_mscorlib;
            }
        }

        /// <summary>
        /// We can cache references to types, as long as they aren't in
        /// collectible assemblies. Unfortunately, we can't really distinguish
        /// between different flavors of assemblies. But, we can at least
        /// create a cache for types in mscorlib (so we get the primitives, etc).
        /// </summary>
        public static bool CanCache(this Type t)
        {
            // Note: we don't have to scan base or declaring types here.
            // There's no way for a type in mscorlib to derive from or be
            // contained in a type from another assembly. The only thing we
            // need to look at is the generic arguments, which are the thing
            // that allows mscorlib types to be specialized by types in other
            // assemblies.

            Assembly asm = t.GetTypeInfo().Assembly;
            if (asm != _mscorlib)
            {
                // Not in mscorlib or our assembly
                return false;
            }

            if (t.GetTypeInfo().IsGenericType)
            {
                foreach (Type g in t.GetGenericArguments())
                {
                    if (!g.CanCache())
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
