// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    internal sealed partial class RoConstructedGenericType
    {
        public readonly struct Key : IEquatable<Key>
        {
            public Key(RoDefinitionType genericTypeDefinition, RoType[] genericTypeArguments)
            {
                Debug.Assert(genericTypeDefinition != null);
                Debug.Assert(genericTypeArguments != null);

                GenericTypeDefinition = genericTypeDefinition;
                GenericTypeArguments = genericTypeArguments;
            }

            public RoDefinitionType GenericTypeDefinition { get; }
            public RoType[] GenericTypeArguments { get; }

            public bool Equals(Key other)
            {
                if (GenericTypeDefinition != other.GenericTypeDefinition)
                    return false;
                if (GenericTypeArguments.Length != other.GenericTypeArguments.Length)
                    return false;
                for (int i = 0; i < GenericTypeArguments.Length; i++)
                {
                    if (GenericTypeArguments[i] != other.GenericTypeArguments[i])
                        return false;
                }
                return true;
            }

            public override bool Equals(object obj) => obj is Key other && Equals(other);

            public override int GetHashCode()
            {
                int hashCode = GenericTypeDefinition.GetHashCode();
                for (int i = 0; i < GenericTypeArguments.Length; i++)
                {
                    hashCode ^= GenericTypeArguments[i].GetHashCode();
                }
                return hashCode;
            }
        }
    }
}
