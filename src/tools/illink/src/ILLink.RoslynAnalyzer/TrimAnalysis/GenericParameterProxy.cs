// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct GenericParameterProxy
	{
		public GenericParameterProxy (ITypeParameterSymbol typeParameterSymbol) => TypeParameterSymbol = typeParameterSymbol;

		internal partial bool HasDefaultConstructorConstraint () =>
			TypeParameterSymbol.HasConstructorConstraint |
			TypeParameterSymbol.HasValueTypeConstraint |
			TypeParameterSymbol.HasUnmanagedTypeConstraint;

		public readonly ITypeParameterSymbol TypeParameterSymbol;

		public override string ToString () => TypeParameterSymbol.ToString ();
	}
}
