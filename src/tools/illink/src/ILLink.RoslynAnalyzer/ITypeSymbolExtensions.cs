// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
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
			if (wellKnownType.TryGetSpecialType (out var specialType)) {
				// Make sure checking the special type is the same as checking the metadata string names.
				Debug.Assert (symbol.IsTypeOf (wellKnownType.GetNamespace (), wellKnownType.GetName ()) == (symbol.SpecialType == specialType));
				return symbol.SpecialType == specialType;
			}
			var (Namespace, Name) = wellKnownType.GetNamespaceAndName ();
			return symbol.IsTypeOf (Namespace, Name);
		}
	}
}
