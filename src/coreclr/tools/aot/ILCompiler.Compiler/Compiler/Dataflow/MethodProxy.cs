// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using ILCompiler;
using ILCompiler.Dataflow;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

#nullable enable

namespace ILLink.Shared.TypeSystemProxy
{
    readonly partial struct MethodProxy
    {
        public MethodProxy(MethodDesc method) => Method = method;

        public static implicit operator MethodProxy(MethodDesc method) => new(method);

        public readonly MethodDesc Method;

        public string Name { get => Method.Name; }

        public string GetDisplayName() => Method.GetDisplayName();

        internal partial bool IsDeclaredOnType(string fullTypeName) => Method.IsDeclaredOnType(fullTypeName);

        internal partial bool HasParameters() => Method.Signature.Length > 0;

        internal partial int GetParametersCount() => Method.Signature.Length;

        internal partial bool HasParameterOfType(int parameterIndex, string fullTypeName) => Method.HasParameterOfType(parameterIndex, fullTypeName);

        internal partial string GetParameterDisplayName(int parameterIndex) =>
            (Method is EcmaMethod ecmaMethod)
                ? ecmaMethod.GetParameterDisplayName(parameterIndex)
                : $"#{parameterIndex}";

        internal partial bool HasGenericParameters() => Method.HasInstantiation;

        internal partial bool HasGenericParametersCount(int genericParameterCount) => Method.Instantiation.Length == genericParameterCount;

        internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters()
        {
            var methodDef = Method.GetMethodDefinition();

            if (!methodDef.HasInstantiation)
                return ImmutableArray<GenericParameterProxy>.Empty;

            var builder = ImmutableArray.CreateBuilder<GenericParameterProxy>(methodDef.Instantiation.Length);
            foreach (var genericParameter in methodDef.Instantiation)
            {
                builder.Add(new GenericParameterProxy((GenericParameterDesc)genericParameter));
            }

            return builder.ToImmutableArray();
        }

        internal partial bool IsStatic() => Method.Signature.IsStatic;

        internal partial bool ReturnsVoid() => Method.Signature.ReturnType.IsVoid;

        public override string ToString() => Method.ToString();
    }
}
