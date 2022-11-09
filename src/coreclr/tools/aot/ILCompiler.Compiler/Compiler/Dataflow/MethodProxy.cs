// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using ILCompiler;
using ILCompiler.Dataflow;
using Internal.TypeSystem;

#nullable enable

namespace ILLink.Shared.TypeSystemProxy
{
    internal readonly partial struct MethodProxy : IEquatable<MethodProxy>
    {
        public MethodProxy(MethodDesc method) => Method = method;

        public static implicit operator MethodProxy(MethodDesc method) => new(method);

        public readonly MethodDesc Method;

        public string Name { get => Method.Name; }

        public string GetDisplayName() => Method.GetDisplayName();

        internal partial bool IsDeclaredOnType(string fullTypeName) => Method.IsDeclaredOnType(fullTypeName);

        internal partial bool HasMetadataParameters() => GetMetadataParametersCount() > 0;

        /// <summary>
        /// Gets the number of entries in the 'Parameters' section of a method's metadata (i.e. excludes the implicit 'this' from the count)
        /// </summary>
        internal partial int GetMetadataParametersCount() => Method.GetMetadataParametersCount();

        internal partial int GetParametersCount() => Method.GetParametersCount();

        /// <summary>
        /// Use only when iterating over all parameters. When wanting to index, use GetParameters(ParameterIndex)
        /// </summary>
        internal partial ParameterProxyEnumerable GetParameters() => new ParameterProxyEnumerable(0, Method.GetParametersCount(), Method);

        internal partial ParameterProxy GetParameter(ParameterIndex index)
        {
            return GetParametersCount() <= (int)index || (int)index < 0
                ? throw new InvalidOperationException($"Cannot get parameter #{(int)index} of method {GetDisplayName()} with {GetParametersCount()} parameters")
                : new ParameterProxy(this, index);
        }

        internal partial bool HasGenericParameters() => Method.HasInstantiation;

        internal partial bool HasGenericParametersCount(int genericParameterCount) => Method.Instantiation.Length == genericParameterCount;

        internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters()
        {
            MethodDesc methodDef = Method.GetMethodDefinition();

            if (!methodDef.HasInstantiation)
                return ImmutableArray<GenericParameterProxy>.Empty;

            ImmutableArray<GenericParameterProxy>.Builder builder = ImmutableArray.CreateBuilder<GenericParameterProxy>(methodDef.Instantiation.Length);
            foreach (TypeDesc? genericParameter in methodDef.Instantiation)
            {
                builder.Add(new GenericParameterProxy((GenericParameterDesc)genericParameter));
            }

            return builder.ToImmutableArray();
        }

        internal partial bool IsStatic() => Method.Signature.IsStatic;

        internal partial bool HasImplicitThis() => !Method.Signature.IsStatic;

        internal partial bool ReturnsVoid() => Method.Signature.ReturnType.IsVoid;

        public override string ToString() => Method.ToString();

        public bool Equals(MethodProxy other) => Method.Equals(other.Method);

        public override bool Equals(object? obj) => obj is MethodProxy other && Equals(other);

        public override int GetHashCode() => Method.GetHashCode();
    }
}
