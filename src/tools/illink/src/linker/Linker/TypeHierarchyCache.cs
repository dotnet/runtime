// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker
{
	sealed class TypeHierarchyCache
	{
		[Flags]
		private enum HierarchyFlags
		{
			IsSystemType = 0x01,
			IsSystemReflectionIReflect = 0x02,
		}

		readonly Dictionary<TypeDefinition, HierarchyFlags> _cache = new Dictionary<TypeDefinition, HierarchyFlags> ();
		readonly LinkContext context;

		public TypeHierarchyCache (LinkContext context)
		{
			this.context = context;
		}

		private HierarchyFlags GetFlags (TypeDefinition resolvedType)
		{
			if (_cache.TryGetValue (resolvedType, out var flags)) {
				return flags;
			}

			if (resolvedType.Name == "IReflect" && resolvedType.Namespace == "System.Reflection") {
				flags |= HierarchyFlags.IsSystemReflectionIReflect;
			}

			TypeDefinition? baseType = resolvedType;
			while (baseType != null) {
				if (baseType.IsTypeOf (WellKnownType.System_Type)) {
					flags |= HierarchyFlags.IsSystemType;
				}

				if (baseType.HasInterfaces) {
					foreach (var iface in baseType.Interfaces) {
						if (iface.InterfaceType.Name == "IReflect" && iface.InterfaceType.Namespace == "System.Reflection") {
							flags |= HierarchyFlags.IsSystemReflectionIReflect;
						}
					}
				}

				baseType = context.TryResolve (baseType.BaseType);
			}

			if (resolvedType != null)
				_cache.Add (resolvedType, flags);

			return flags;
		}

		public bool IsSystemType (TypeDefinition type)
		{
			return (GetFlags (type) & HierarchyFlags.IsSystemType) != 0;
		}

		public bool IsSystemReflectionIReflect (TypeDefinition type)
		{
			return (GetFlags (type) & HierarchyFlags.IsSystemReflectionIReflect) != 0;
		}
	}
}
