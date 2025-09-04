// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

using Internal.TypeSystem;

namespace Internal
{
    /// <summary>
    /// Managed implementation of the version-resilient hash code algorithm.
    /// </summary>
    internal static partial class VersionResilientHashCode
    {
        /// <summary>
        /// CoreCLR 3-parameter <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/versionresilienthashcode.cpp#L9">GetVersionResilientTypeHashCode</a>
        /// </summary>
        /// <param name="type">Type to hash</param>
        public static int TypeTableHashCode(DefType type)
        {
            int hashcode = 0;
            do
            {
                hashcode ^= NameHashCode(type.U8Namespace, type.U8Name);
                type = type.ContainingType;
            }
            while (type != null);
            return hashcode;
        }

        /// <summary>
        /// CoreCLR 1-parameter <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/versionresilienthashcode.cpp#L109">GetVersionResilientTypeHashCode</a>
        /// </summary>
        /// <param name="type">Type to hash</param>
        public static int TypeHashCode(TypeDesc type)
        {
            if (type.GetTypeDefinition() is DefType defType)
            {
                int hashcode = NameHashCode(defType.U8Namespace, defType.U8Name);
                DefType containingType = defType.ContainingType;
                if (containingType != null)
                {
                    hashcode = NestedTypeHashCode(TypeHashCode(containingType), hashcode);
                }
                if (type.HasInstantiation && !type.IsGenericDefinition)
                {
                    return GenericInstanceHashCode(hashcode, type.Instantiation);
                }
                else
                {
                    return hashcode;
                }
            }

            if (type is ArrayType arrayType)
            {
                return ArrayTypeHashCode(TypeHashCode(arrayType.ElementType), arrayType.Rank);
            }

            if (type is PointerType pointerType)
            {
                return PointerTypeHashCode(TypeHashCode(pointerType.ParameterType));
            }

            if (type is ByRefType byRefType)
            {
                return ByrefTypeHashCode(TypeHashCode(byRefType.ParameterType));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// CoreCLR <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/typehashingalgorithms.h#L87">ComputeGenericInstanceHashCode</a>
        /// </summary>
        /// <param name="hashcode">Base hash code</param>
        /// <param name="instantiation">Instantiation to include in the hash</param>
        private static int GenericInstanceHashCode(int hashcode, Instantiation instantiation)
        {
            for (int i = 0; i < instantiation.Length; i++)
            {
                int argumentHashCode = TypeHashCode(instantiation[i]);
                hashcode = unchecked(hashcode + RotateLeft(hashcode, 13)) ^ argumentHashCode;
            }
            return unchecked(hashcode + RotateLeft(hashcode, 15));
        }

        /// <summary>
        /// CoreCLR <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/versionresilienthashcode.cpp#L161">GetVersionResilientMethodHashCode</a>
        /// </summary>
        /// <param name="method">Method to hash</param>
        public static int MethodHashCode(MethodDesc method)
        {
            int hashCode = TypeHashCode(method.OwningType);
            int methodNameHashCode = NameHashCode(method.U8Name);

            // Todo: Add signature to hash.
            if (method.HasInstantiation && !method.IsGenericMethodDefinition)
            {
                hashCode ^= GenericInstanceHashCode(methodNameHashCode, method.Instantiation);
            }
            else
            {
                hashCode ^= methodNameHashCode;
            }

            return hashCode;
        }

        public static int ModuleNameHashCode(ModuleDesc module)
        {
            IAssemblyDesc assembly = module.Assembly;
            Debug.Assert(assembly == module);
            return NameHashCode(assembly.Name);
        }
    }
}
