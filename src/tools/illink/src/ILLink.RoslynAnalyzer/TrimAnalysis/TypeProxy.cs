// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy
	{
		public TypeProxy (ITypeSymbol type) => Type = type;

		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ()
		{
			if (Type is not INamedTypeSymbol namedType ||
				namedType.TypeParameters.IsEmpty)
				return ImmutableArray<GenericParameterProxy>.Empty;

			var builder = ImmutableArray.CreateBuilder<GenericParameterProxy> (namedType.TypeParameters.Length);
			foreach (var typeParameter in namedType.TypeParameters) {
				builder.Add (new GenericParameterProxy (typeParameter));
			}

			return builder.ToImmutableArray ();
		}

		public readonly ITypeSymbol Type;

		public string Name { get => Type.MetadataName; }

		public string? Namespace { get => Type.ContainingNamespace?.Name; }

		public bool IsTypeOf (string @namespace, string name) => Type.IsTypeOf (@namespace, name);

		public bool IsTypeOf (WellKnownType wellKnownType) => Type.IsTypeOf (wellKnownType);

		public string GetDisplayName () => Type.GetDisplayName ();

		public override string ToString () => Type.ToString ();
	}
}
