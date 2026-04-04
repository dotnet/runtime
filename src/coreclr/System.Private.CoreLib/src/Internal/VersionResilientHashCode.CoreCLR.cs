// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal
{
    /// <summary>
    /// Managed implementation of the version-resilient hash code algorithm.
    /// </summary>
    internal static partial class VersionResilientHashCode
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "VersionResilientHashCode_TypeHashCode")]
        private static partial int TypeHashCode(QCallTypeHandle typeHandle);

        public static int TypeHashCode(RuntimeType type)
            => TypeHashCode(new QCallTypeHandle(ref type));

        private static int NameHashCode(string s1, string s2)
            => NameHashCode(System.Text.Encoding.UTF8.GetBytes(s1), System.Text.Encoding.UTF8.GetBytes(s2));

        /// <summary>
        /// CoreCLR 1-parameter <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/versionresilienthashcode.cpp#L109">GetVersionResilientTypeHashCode</a>
        /// </summary>
        /// <param name="type">TypeName to hash</param>
        public static int TypeHashCode(TypeName type)
        {
            if (type.IsSimple || type.IsConstructedGenericType)
            {
                int hashcode = NameHashCode(type.IsNested ? string.Empty : type.Namespace, type.Name);
                if (type.IsNested)
                {
                    hashcode = NestedTypeHashCode(TypeHashCode(type.DeclaringType), hashcode);
                }
                if (type.IsConstructedGenericType)
                {
                    return GenericInstanceHashCode(hashcode, type.GetGenericArguments());
                }
                else
                {
                    return hashcode;
                }
            }

            if (type.IsArray)
            {
                return ArrayTypeHashCode(TypeHashCode(type.GetElementType()), type.GetArrayRank());
            }

            if (type.IsPointer)
            {
                return PointerTypeHashCode(TypeHashCode(type.GetElementType()));
            }

            if (type.IsByRef)
            {
                return ByrefTypeHashCode(TypeHashCode(type.GetElementType()));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// CoreCLR <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/typehashingalgorithms.h#L87">ComputeGenericInstanceHashCode</a>
        /// </summary>
        /// <param name="hashcode">Base hash code</param>
        /// <param name="instantiation">Instantiation to include in the hash</param>
        private static int GenericInstanceHashCode(int hashcode, ReadOnlySpan<TypeName> instantiation)
        {
            for (int i = 0; i < instantiation.Length; i++)
            {
                int argumentHashCode = TypeHashCode(instantiation[i]);
                hashcode = unchecked(hashcode + RotateLeft(hashcode, 13)) ^ argumentHashCode;
            }
            return unchecked(hashcode + RotateLeft(hashcode, 15));
        }
    }
}
