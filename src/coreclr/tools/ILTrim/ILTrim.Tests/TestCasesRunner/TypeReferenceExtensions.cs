// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Mono.Cecil;

namespace Mono.Linker
{
    // Provides IsTypeOf<T> from illink's TypeReferenceExtensions so that
    // shared test files (e.g. AssemblyChecker) compile without ifdefs.
    internal static class TypeReferenceExtensions
    {
        public static bool IsTypeOf<T>(this TypeReference tr)
        {
            var type = typeof(T);
            return tr.Name == type.Name && tr.Namespace == type.Namespace;
        }
    }
}
