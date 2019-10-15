// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection.TypeLoading;

//
// This file shims just enough stuff so that source code copied from Reflection.Core builds with as few source changes
// as possible.
//

namespace System.Reflection.Runtime.TypeInfos
{
    internal static class RoShims
    {
        internal static RoType CastToRuntimeTypeInfo(this Type t) => (RoType)t;

        internal static bool CanBrowseWithoutMissingMetadataExceptions(this Type t) => true;

        internal static Type[] GetGenericTypeParameters(this Type t)
        {
            Debug.Assert(t.IsGenericTypeDefinition);
            return t.GetGenericArguments();
        }
    }
}

namespace Internal.Reflection.Core.Execution
{
    internal static class ReflectionCoreExecution
    {
        internal static class ExecutionDomain
        {
            // We'll never actually reach this since Ro type objects cannot trigger missing metadata exceptions.
            internal static Exception CreateMissingMetadataException(Type t) => new Exception();
        }
    }
}

namespace System.Collections.Concurrent
{
    //
    // Reflection.Core couldn't use ConcurrentDictionary because of layering so it had its own implementation.
    // This emulates the old interface.
    //
    internal abstract class ConcurrentUnifier<K, V>
        where K : IEquatable<K>
        where V : class
    {
        protected ConcurrentUnifier() { }
        public V GetOrAdd(K key) => _dict.GetOrAdd(key, Factory);
        protected abstract V Factory(K key);
        private readonly ConcurrentDictionary<K, V> _dict = new ConcurrentDictionary<K, V>();
    }
}

namespace System.Reflection.TypeLoading
{
    internal static class DefaultBinderThunks
    {
        internal static ParameterInfo[] GetParametersNoCopy(this MethodBase m)
        {
            if (m is RoMethod roMethod)
                return roMethod.GetParametersNoCopy();
            if (m is RoConstructor roConstructor)
                return roConstructor.GetParametersNoCopy();
            return m.GetParameters();
        }

        internal static int GetGenericParameterCount(this MethodInfo m)
        {
            if (m is RoMethod roMethod)
                return roMethod.GetGenericArgumentsOrParametersNoCopy().Length;
            return m.GetGenericArguments().Length;
        }
    }
}
