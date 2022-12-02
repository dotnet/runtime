// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
	readonly partial struct MethodProxy
	{
		public MethodProxy (IMethodSymbol method) => Method = method;

		public readonly IMethodSymbol Method;

		public string Name { get => Method.Name; }

		public string GetDisplayName () => Method.GetDisplayName ();

		internal partial bool IsDeclaredOnType (string fullTypeName) => IsTypeOf (Method.ContainingType, fullTypeName);

		internal partial bool HasMetadataParameters () => Method.Parameters.Length > 0;

		internal partial int GetMetadataParametersCount () => Method.GetMetadataParametersCount ();

		internal partial int GetParametersCount () => Method.GetParametersCount ();

		internal partial ParameterProxyEnumerable GetParameters () => Method.GetParameters ();

		internal partial ParameterProxy GetParameter (ParameterIndex index) => Method.GetParameter (index);

		internal partial bool HasGenericParameters () => Method.IsGenericMethod;

		internal partial bool HasGenericParametersCount (int genericParameterCount) => Method.TypeParameters.Length == genericParameterCount;

		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ()
		{
			if (Method.TypeParameters.IsEmpty)
				return ImmutableArray<GenericParameterProxy>.Empty;

			var builder = ImmutableArray.CreateBuilder<GenericParameterProxy> (Method.TypeParameters.Length);
			foreach (var typeParameter in Method.TypeParameters) {
				builder.Add (new GenericParameterProxy (typeParameter));
			}

			return builder.ToImmutableArray ();
		}

		internal partial bool IsStatic () => Method.IsStatic;

		internal partial bool HasImplicitThis () => Method.HasImplicitThis ();

		internal partial bool ReturnsVoid () => Method.ReturnType.SpecialType == SpecialType.System_Void;

		static bool IsTypeOf (ITypeSymbol type, string fullTypeName)
		{
			if (type is not INamedTypeSymbol namedType)
				return false;

			return namedType.HasName (fullTypeName);
		}

		public override string ToString () => Method.ToString ();
	}
}
