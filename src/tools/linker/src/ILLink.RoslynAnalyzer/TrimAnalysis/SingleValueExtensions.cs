// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public static class SingleValueExtensions
	{
		public static SingleValue? FromTypeSymbol (ITypeSymbol type)
		{
			if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
				var underlyingType = (type as INamedTypeSymbol)?.TypeArguments.FirstOrDefault ();
				return underlyingType?.TypeKind switch {
					TypeKind.TypeParameter =>
						new NullableValueWithDynamicallyAccessedMembers (new TypeProxy (type),
							new GenericParameterValue ((ITypeParameterSymbol) underlyingType)),
					// typeof(Nullable<>) 
					TypeKind.Error => new SystemTypeValue (new TypeProxy (type)),
					TypeKind.Class or TypeKind.Struct or TypeKind.Interface =>
						new NullableSystemTypeValue (new TypeProxy (type), new SystemTypeValue (new TypeProxy (underlyingType))),
					_ => UnknownValue.Instance
				};
			}
			return type.Kind switch {
				SymbolKind.TypeParameter => new GenericParameterValue ((ITypeParameterSymbol) type),
				SymbolKind.NamedType => new SystemTypeValue (new TypeProxy (type)),
				// If the symbol is an Array type, the BaseType is System.Array
				SymbolKind.ArrayType => new SystemTypeValue (new TypeProxy (type.BaseType!)),
				SymbolKind.ErrorType => UnknownValue.Instance,
				_ => null
			};

		}
	}
}
