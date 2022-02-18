// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct GenericParameterProxy
	{
		public GenericParameterProxy (ITypeParameterSymbol typeParameterSymbol) => TypeParameterSymbol = typeParameterSymbol;

		public readonly ITypeParameterSymbol TypeParameterSymbol;

		public override string ToString () => TypeParameterSymbol.ToString ();
	}
}
