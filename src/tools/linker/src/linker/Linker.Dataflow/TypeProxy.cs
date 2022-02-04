// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy
	{
		public TypeProxy (TypeReference type) => Type = type;

		public TypeReference Type { get; }

		internal partial bool IsVoid () => Type.MetadataType == MetadataType.Void;

		public string Name { get => Type.Name; }

		public string GetDisplayName () => Type.GetDisplayName ();

		public override string ToString () => Type.ToString ();
	}
}
