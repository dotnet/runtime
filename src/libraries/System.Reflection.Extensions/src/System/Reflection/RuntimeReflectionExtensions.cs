// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using Internal.Reflection.Extensions.NonPortable;

namespace System.Reflection
{
    public static partial class RuntimeReflectionExtensions
    {
        private static readonly BindingFlags defaultPublicFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
        private static readonly BindingFlags defaultNonPublicFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

        //=============================================================================================================
        // This group of apis are equivalent to the desktop CLR's:
        //
        //      type.Get***(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance)
        //
        // Returns all directly declared members.
        // Members from base classes are returned if they're not private, static or overridden.
        //=============================================================================================================
        public static IEnumerable<FieldInfo> GetRuntimeFields(this Type type)
        {
            return type.GetFields(defaultNonPublicFlags).AsNothingButIEnumerable();
        }

        public static IEnumerable<MethodInfo> GetRuntimeMethods(this Type type)
        {
            return type.GetMethods(defaultNonPublicFlags).AsNothingButIEnumerable();
        }

        public static IEnumerable<PropertyInfo> GetRuntimeProperties(this Type type)
        {
            return type.GetProperties(defaultNonPublicFlags).AsNothingButIEnumerable();
        }

        public static IEnumerable<EventInfo> GetRuntimeEvents(this Type type)
        {
            return type.GetEvents(defaultNonPublicFlags).AsNothingButIEnumerable();
        }

        //=============================================================================================================
        // This group of apis are equivalent to the desktop CLR's:
        //
        //      type.Get***(name, BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance)
        //
        // Note that unlike the GetRuntime***() apis defined on this same class, non-public members are excluded from the search.
        //
        // Searches all *public* directly declared and inherited members. If there's a multiple match,
        // the most derived one wins.
        //
        // throws AmbiguousMatchException() if the names two members declared by the same class.
        //=============================================================================================================

        public static FieldInfo GetRuntimeField(this Type type, String name)
        {
            return type.GetField(name, defaultPublicFlags);
        }

        public static MethodInfo GetRuntimeMethod(this Type type, String name, Type[] parameters)
        {
            return type.GetMethod(name, parameters);
        }

        public static PropertyInfo GetRuntimeProperty(this Type type, String name)
        {
            return type.GetProperty(name, defaultPublicFlags);
        }

        public static EventInfo GetRuntimeEvent(this Type type, String name)
        {
            return type.GetEvent(name, defaultPublicFlags);
        }

        private static M MostDerived<M>(this IEnumerable<M> members) where M : MemberInfo
        {
            IEnumerator<M> enumerator = members.GetEnumerator();
            if (!enumerator.MoveNext())
                return null;
            M result = enumerator.Current;
            if (!enumerator.MoveNext())
                return result;
            M anotherResult = enumerator.Current;
            if (anotherResult.DeclaringType.Equals(result.DeclaringType))
                throw new AmbiguousMatchException();
            return result;
        }


        //======================================================================================
        // For virtual non-new slot methods, returns the least derived method that occupies the slot.
        // For everything else, return "this".
        //======================================================================================
        public static MethodInfo GetRuntimeBaseDefinition(this MethodInfo method)
        {
            return method.GetBaseDefinition();
        }

        //======================================================================================

        public static InterfaceMapping GetRuntimeInterfaceMap(this TypeInfo typeInfo, Type interfaceType)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_InterfaceMap);
        }

        //======================================================================================

        public static MethodInfo GetMethodInfo(this Delegate del)
        {
            return DelegateMethodInfoRetriever.GetDelegateMethodInfo(del);
        }
    }
}

