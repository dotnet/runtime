// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Cecil;

namespace Mono.Linker;

public static class ExportedTypeExtensions {
	public static TypeDefinition? TryResolve (this ExportedType exportedType, ModuleDefinition module) {
		return ((MetadataResolver) module.MetadataResolver).TryResolve (exportedType.CreateReference (module));
	}

	private static TypeReference CreateReference (this ExportedType exportedType, ModuleDefinition module) {
		return new TypeReference (exportedType.Namespace, exportedType.Name, module, exportedType.Scope) {
			DeclaringType = exportedType.DeclaringType?.CreateReference (module),
		};
	}

	private static TypeDefinition? TryResolve (this MetadataResolver metadataResolver, TypeReference type) {
		type = type.GetElementType ();

		var scope = type.Scope;

		if (scope == null)
			return null;

		switch (scope.MetadataScopeType) {
		case MetadataScopeType.AssemblyNameReference:
			var assembly = ((AssemblyResolver) metadataResolver.AssemblyResolver).Resolve ((AssemblyNameReference) scope, probing: true);
			if (assembly == null)
				return null;

			return metadataResolver.GetType (assembly.MainModule, type);
		case MetadataScopeType.ModuleDefinition:
			return metadataResolver.GetType ((ModuleDefinition) scope, type);
		case MetadataScopeType.ModuleReference:
			if (type.Module.Assembly == null)
				return null;

			var modules = type.Module.Assembly.Modules;
			var module_ref = (ModuleReference) scope;
			for (int i = 0; i < modules.Count; i++) {
				var netmodule = modules [i];
				if (netmodule.Name == module_ref.Name)
					return metadataResolver.GetType (netmodule, type);
			}
			break;
		}

		throw new NotSupportedException ();
	}

	private static TypeDefinition? GetType (this MetadataResolver metadataResolver, ModuleDefinition module, TypeReference reference) {
		var type = metadataResolver.GetTypeDefinition (module, reference);
		if (type != null)
			return type;

		if (!module.HasExportedTypes)
			return null;

		var exported_types = module.ExportedTypes;

		for (int i = 0; i < exported_types.Count; i++) {
			var exported_type = exported_types [i];
			if (exported_type.Name != reference.Name)
				continue;

			if (exported_type.Namespace != reference.Namespace)
				continue;

			return exported_type.TryResolve (module);
		}

		return null;
	}

	private static TypeDefinition? GetTypeDefinition (this MetadataResolver metadtaaResolver, ModuleDefinition module, TypeReference type)
	{
		if (!type.IsNested)
			return module.GetType (type.Namespace, type.Name);

		var declaring_type = metadtaaResolver.TryResolve (type.DeclaringType);
		if (declaring_type == null)
			return null;

		return declaring_type.GetNestedType (type.TypeFullName ());
	}

	private static string TypeFullName (this TypeReference self)
	{
		return string.IsNullOrEmpty (self.Namespace)
			? self.Name
			: self.Namespace + '.' + self.Name;
	}

	private static TypeDefinition? GetNestedType (this TypeDefinition self, string fullname)
	{
		if (!self.HasNestedTypes)
			return null;

		var nested_types = self.NestedTypes;

		for (int i = 0; i < nested_types.Count; i++) {
			var nested_type = nested_types [i];

			if (nested_type.TypeFullName () == fullname)
				return nested_type;
		}

		return null;
	}
}
