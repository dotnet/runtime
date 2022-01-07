// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is a System.Type value which represents generic parameter (basically result of typeof(T))
	/// Its actual type is unknown, but it can have annotations.
	/// </summary>
	sealed partial record SystemTypeValue
	{
		public SystemTypeValue (INamedTypeSymbol namedTypeSymbol) => NamedTypeSymbol = namedTypeSymbol;

		public readonly INamedTypeSymbol NamedTypeSymbol;

		public override string ToString ()
			=> this.ValueToString (NamedTypeSymbol);
	}
}
