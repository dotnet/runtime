// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	readonly partial struct MethodProxy
	{
		public MethodProxy (MethodDefinition method) => Method = method;

		public static implicit operator MethodProxy (MethodDefinition method) => new (method);

		public readonly MethodDefinition Method;

		public string Name { get => Method.Name; }

		public string GetDisplayName () => Method.GetDisplayName ();

		internal partial bool IsDeclaredOnType (string fullTypeName) => Method.IsDeclaredOnType (fullTypeName);

		internal partial bool HasParameters () => Method.HasParameters;

		internal partial int GetParametersCount () => Method.Parameters.Count;

		internal partial bool HasParameterOfType (int parameterIndex, string fullTypeName) => Method.HasParameterOfType (parameterIndex, fullTypeName);

		internal partial string GetParameterDisplayName (int parameterIndex) => Method.Parameters[parameterIndex].Name;

		internal partial bool HasGenericParameters () => Method.HasGenericParameters;

		internal partial bool HasGenericParametersCount (int genericParameterCount) => Method.GenericParameters.Count == genericParameterCount;

		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ()
		{
			if (!Method.HasGenericParameters)
				return ImmutableArray<GenericParameterProxy>.Empty;

			var builder = ImmutableArray.CreateBuilder<GenericParameterProxy> (Method.GenericParameters.Count);
			foreach (var genericParameter in Method.GenericParameters) {
				builder.Add (new GenericParameterProxy (genericParameter));
			}

			return builder.ToImmutableArray ();
		}

		internal partial bool IsStatic () => Method.IsStatic;

		internal partial bool ReturnsVoid () => Method.ReturnsVoid ();

		public override string ToString () => Method.ToString ();
	}
}
