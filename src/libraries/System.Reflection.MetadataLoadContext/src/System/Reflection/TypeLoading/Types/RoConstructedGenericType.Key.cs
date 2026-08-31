// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
                    Type t1 = GenericTypeArguments[i];
                    Type t2 = other.GenericTypeArguments[i];

                    // Modified types do not support Equals\GetHashCode.
                    if (t1 is RoModifiedType || t2 is RoModifiedType)
                    {
                        return ReferenceEquals(t1, t2);
                    }
                    else if (t1 != t2)
                    {
                        return false;
                    }
                }
                return true;
            }

            public override bool Equals([NotNullWhen(true)] object? obj) => obj is Key other && Equals(other);

            public override int GetHashCode()
            {
                int hashCode = GenericTypeDefinition.GetHashCode();
                for (int i = 0; i < GenericTypeArguments.Length; i++)
                {
                    RoType argType = GenericTypeArguments[i];
                    hashCode ^= argType is RoModifiedType ?
                        argType.UnderlyingSystemType.GetHashCode() :
                        argType.GetHashCode();
                }
                return hashCode;
            }
        }
    }
}
