// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Represents a method (definition, reference, or standalone) or property signature.
    /// In the case of properties, the signature matches that of a getter with a distinguishing <see cref="SignatureHeader"/>.
    /// </summary>
    public readonly struct MethodSignature<TType>
    {
        /// <summary>
        /// Represents the information in the leading byte of the signature (kind, calling convention, flags).
        /// </summary>
        public SignatureHeader Header { get; }

        /// <summary>
        /// Gets the method's return type.
        /// </summary>
        public TType ReturnType { get; }

        /// <summary>
        /// Gets the number of parameters that are required. Will be equal to the length <see cref="ParameterTypes"/> of
        /// unless this signature represents the standalone call site of a vararg method, in which case the entries
        /// extra entries in <see cref="ParameterTypes"/> are the types used for the optional parameters.
        /// </summary>
        public int RequiredParameterCount { get; }

        /// <summary>
        /// Gets the number of generic type parameters of the method. Will be 0 for non-generic methods.
        /// </summary>
        public int GenericParameterCount { get; }

        /// <summary>
        /// Gets the method's parameter types.
        /// </summary>
        public ImmutableArray<TType> ParameterTypes { get; }

        public MethodSignature(SignatureHeader header, TType returnType, int requiredParameterCount, int genericParameterCount, ImmutableArray<TType> parameterTypes)
        {
            Header = header;
            ReturnType = returnType;
            GenericParameterCount = genericParameterCount;
            RequiredParameterCount = requiredParameterCount;
            ParameterTypes = parameterTypes;
        }
    }
}
