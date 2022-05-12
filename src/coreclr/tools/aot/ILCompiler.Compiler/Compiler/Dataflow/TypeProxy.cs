// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using ILCompiler;
using ILCompiler.Dataflow;
using Internal.TypeSystem;

#nullable enable

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy
	{
		public TypeProxy (TypeDesc type) => Type = type;

		public static implicit operator TypeProxy (TypeDesc type) => new (type);

		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ()
		{
			if (!Type.HasInstantiation)
				return ImmutableArray<GenericParameterProxy>.Empty;

			var builder = ImmutableArray.CreateBuilder<GenericParameterProxy> (Type.Instantiation.Length);
			foreach (var genericParameter in Type.Instantiation) {
				builder.Add (new GenericParameterProxy ((GenericParameterDesc)genericParameter));
			}

			return builder.ToImmutableArray ();
		}

		public TypeDesc Type { get; }

		public string Name { get => Type is MetadataType metadataType ? metadataType.Name : string.Empty; }

		public string? Namespace { get => Type is MetadataType metadataType ? metadataType.Namespace : null; }

		public bool IsTypeOf (string @namespace, string name) => Type.IsTypeOf (@namespace, name);

		public bool IsTypeOf (WellKnownType wellKnownType) => Type.IsTypeOf (wellKnownType);

		public string GetDisplayName () => Type.GetDisplayName ();

		public override string ToString () => Type.ToString ();
	}
}
