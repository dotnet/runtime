// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy
	{
		public TypeProxy (TypeDefinition type) => Type = type;

		public static implicit operator TypeProxy (TypeDefinition type) => new (type);

		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ()
		{
			if (!Type.HasGenericParameters)
				return ImmutableArray<GenericParameterProxy>.Empty;

			var builder = ImmutableArray.CreateBuilder<GenericParameterProxy> (Type.GenericParameters.Count);
			foreach (var genericParameter in Type.GenericParameters) {
				builder.Add (new GenericParameterProxy (genericParameter));
			}

			return builder.ToImmutableArray ();
		}

		public TypeDefinition Type { get; }

		public string Name { get => Type.Name; }

		public string? Namespace { get => Type.Namespace; }

		public bool IsTypeOf (string @namespace, string name) => Type.IsTypeOf (@namespace, name);

		public bool IsTypeOf (WellKnownType wellKnownType) => Type.IsTypeOf (wellKnownType);

		public string GetDisplayName () => Type.GetDisplayName ();

		public override string ToString () => Type.ToString ();
	}
}
