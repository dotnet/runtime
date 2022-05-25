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

		internal partial bool HasParameters () => Method.Parameters.Length > 0;

		internal partial int GetParametersCount () => Method.Parameters.Length;

		internal partial bool HasParameterOfType (int parameterIndex, string fullTypeName)
			=> Method.Parameters.Length > parameterIndex && IsTypeOf (Method.Parameters[parameterIndex].Type, fullTypeName);

		internal partial string GetParameterDisplayName (int parameterIndex) => Method.Parameters[parameterIndex].GetDisplayName ();

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

		internal partial bool ReturnsVoid () => Method.ReturnType.SpecialType == SpecialType.System_Void;

		static bool IsTypeOf (ITypeSymbol type, string fullTypeName)
		{
			if (type is not INamedTypeSymbol namedType)
				return false;

			return namedType.HasName (fullTypeName);
		}

		public ReferenceKind ParameterReferenceKind (int index)
			=> Method.Parameters[index].RefKind switch {
				RefKind.In => ReferenceKind.In,
				RefKind.Out => ReferenceKind.Out,
				RefKind.Ref => ReferenceKind.Ref,
				_ => ReferenceKind.None
			};

		public override string ToString () => Method.ToString ();
	}
}
