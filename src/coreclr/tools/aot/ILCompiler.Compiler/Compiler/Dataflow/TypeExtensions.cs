// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Dataflow
{
    static class TypeExtensions
    {
        public static bool IsTypeOf(this TypeDesc type, string ns, string name)
        {
            return type is MetadataType mdType && mdType.Name == name && mdType.Namespace == ns;
        }

        public static bool IsDeclaredOnType(this MethodDesc method, string ns, string name)
        {
            return method.OwningType.IsTypeOf(ns, name);
        }

        public static bool HasParameterOfType(this MethodDesc method, int index, string ns, string name)
        {
            return index < method.Signature.Length && method.Signature[index].IsTypeOf(ns, name);
        }
    }
}
