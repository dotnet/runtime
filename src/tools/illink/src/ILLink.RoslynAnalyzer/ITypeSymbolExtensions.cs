// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	static class ITypeSymbolExtensions
	{
		[Flags]
		private enum HierarchyFlags
		{
			IsSystemType = 0x01,
			IsSystemReflectionIReflect = 0x02,
		}

		public static bool IsTypeInterestingForDataflow (this ITypeSymbol type)
		{
			if (type.SpecialType == SpecialType.System_String)
				return true;

			var flags = GetFlags (type);
			return IsSystemType (flags) || IsSystemReflectionIReflect (flags);
		}

		private static HierarchyFlags GetFlags (ITypeSymbol type)
		{
			HierarchyFlags flags = 0;
			if (type.IsTypeOf (WellKnownType.System_Reflection_IReflect)) {
				flags |= HierarchyFlags.IsSystemReflectionIReflect;
			}

			ITypeSymbol? baseType = type;
			while (baseType != null) {
				if (baseType.IsTypeOf (WellKnownType.System_Type))
					flags |= HierarchyFlags.IsSystemType;

				foreach (var iface in baseType.Interfaces) {
					if (iface.IsTypeOf (WellKnownType.System_Reflection_IReflect)) {
						flags |= HierarchyFlags.IsSystemReflectionIReflect;
					}
				}

				baseType = baseType.BaseType;
			}
			return flags;
		}

		private static bool IsSystemType (HierarchyFlags flags) => (flags & HierarchyFlags.IsSystemType) != 0;

		private static bool IsSystemReflectionIReflect (HierarchyFlags flags) => (flags & HierarchyFlags.IsSystemReflectionIReflect) != 0;

		public static bool IsTypeOf (this ITypeSymbol symbol, string @namespace, string name)
		{
			return symbol.ContainingNamespace?.GetDisplayName () == @namespace && symbol.MetadataName == name;
		}

		public static bool IsTypeOf (this ITypeSymbol symbol, WellKnownType wellKnownType)
		{
			return symbol.TryGetWellKnownType () == wellKnownType;
		}

		public static WellKnownType? TryGetWellKnownType (this ITypeSymbol symbol)
		{
			return symbol.SpecialType switch {
				SpecialType.System_String => WellKnownType.System_String,
				SpecialType.System_Nullable_T => WellKnownType.System_Nullable_T,
				SpecialType.System_Array => WellKnownType.System_Array,
				SpecialType.System_Object => WellKnownType.System_Object,
				_ => WellKnownTypeExtensions.GetWellKnownType (symbol.ContainingNamespace?.GetDisplayName () ?? "", symbol.MetadataName)
			};
		}
	}
}
