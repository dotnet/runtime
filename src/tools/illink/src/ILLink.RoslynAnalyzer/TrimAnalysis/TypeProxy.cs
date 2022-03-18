// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy
	{
		public TypeProxy (ITypeSymbol type) => Type = type;

		public readonly ITypeSymbol Type;

		public string Name { get => Type.MetadataName; }

		public string Namespace { get => Type.ContainingNamespace.Name; }

		public string GetDisplayName () => Type.GetDisplayName ();

		public override string ToString () => Type.ToString ();

		public bool IsTypeOf (string @namespace, string name) => Namespace == @namespace && Name == name;
	}
}
