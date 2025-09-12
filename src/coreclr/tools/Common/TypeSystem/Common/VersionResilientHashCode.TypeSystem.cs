// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Internal.TypeSystem;

namespace Internal
{
    internal static partial class VersionResilientHashCode
    {
        /// <summary>
        /// CoreCLR <a href="https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/typehashingalgorithms.h#L87">ComputeGenericInstanceHashCode</a>
        /// </summary>
        /// <param name="hashcode">Base hash code</param>
        /// <param name="instantiation">Instantiation to include in the hash</param>
        public static int GenericInstanceHashCode(int hashcode, Instantiation instantiation)
        {
            for (int i = 0; i < instantiation.Length; i++)
            {
                int argumentHashCode = instantiation[i].GetHashCode();
                hashcode = unchecked(hashcode + RotateLeft(hashcode, 13)) ^ argumentHashCode;
            }
            return unchecked(hashcode + RotateLeft(hashcode, 15));
        }

        public static int GenericInstanceHashCode<T>(int hashcode, T[] instantiation)
        {
            for (int i = 0; i < instantiation.Length; i++)
            {
                int argumentHashCode = instantiation[i].GetHashCode();
                hashcode = unchecked(hashcode + RotateLeft(hashcode, 13)) ^ argumentHashCode;
            }
            return unchecked(hashcode + RotateLeft(hashcode, 15));
        }
    }
}
