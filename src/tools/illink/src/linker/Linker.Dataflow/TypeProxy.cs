// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy
	{
		public TypeProxy (TypeDefinition type) => Type = type;

		public static implicit operator TypeProxy (TypeDefinition type) => new (type);

		public TypeDefinition Type { get; }

		public string Name { get => Type.Name; }

		public string Namespace { get => Type.Namespace; }

		public string GetDisplayName () => Type.GetDisplayName ();

		public override string ToString () => Type.ToString ();

		public bool IsTypeOf (string @namespace, string name) => Type.IsTypeOf (@namespace, name);
	}
}
