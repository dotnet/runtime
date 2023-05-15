// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Internal.Runtime.Augments;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Set of helpers used to implement callsite-specific reflection intrinsics.
    /// </summary>
    internal static class ReflectionHelpers
    {
        // This entry is used to implement Type.GetType()'s ability to detect the calling assembly and use it as
        // a default assembly name.
        public static Type GetType(string typeName, string callingAssemblyName, bool throwOnError, bool ignoreCase)
        {
            return TypeNameParser.GetType(typeName, throwOnError: throwOnError, ignoreCase: ignoreCase, defaultAssemblyName: callingAssemblyName);
        }

        // This entry is used to implement Type.GetType()'s ability to detect the calling assembly and use it as
        // a default assembly name.
        public static Type ExtensibleGetType(string typeName, string callingAssemblyName, Func<AssemblyName, Assembly?> assemblyResolver, Func<Assembly?, string, bool, Type?>? typeResolver, bool throwOnError, bool ignoreCase)
        {
            return TypeNameParser.GetType(typeName, assemblyResolver, typeResolver, throwOnError: throwOnError, ignoreCase: ignoreCase, defaultAssemblyName: callingAssemblyName);
        }

        // This supports Assembly.GetExecutingAssembly() intrinsic expansion in the compiler
        public static Assembly GetExecutingAssembly(RuntimeTypeHandle typeHandle)
        {
            return RuntimeAugments.Callbacks.GetAssemblyForHandle(typeHandle);
        }

        // This supports MethodBase.GetCurrentMethod() intrinsic expansion in the compiler
        public static MethodBase GetCurrentMethodNonGeneric(RuntimeMethodHandle methodHandle)
        {
            return MethodBase.GetMethodFromHandle(methodHandle);
        }

        // This supports MethodBase.GetCurrentMethod() intrinsic expansion in the compiler
        public static MethodBase GetCurrentMethodGeneric(RuntimeMethodHandle methodHandle, RuntimeTypeHandle typeHandle)
        {
            return MethodBase.GetMethodFromHandle(methodHandle, typeHandle);
        }
    }
}
