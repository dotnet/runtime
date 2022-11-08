// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public static class MarkExportedTypesTarget
	{
		public static void ProcessAssembly (AssemblyDefinition assembly, LinkContext context)
		{
			if (!assembly.MainModule.HasExportedTypes)
				return;

			foreach (var type in assembly.MainModule.ExportedTypes)
				InitializeExportedType (type, context, assembly);
		}

		static void InitializeExportedType (ExportedType exportedType, LinkContext context, AssemblyDefinition assembly)
		{
			if (!context.Annotations.IsMarked (exportedType))
				return;

			if (!context.Annotations.TryGetPreservedMembers (exportedType, out TypePreserveMembers members))
				return;

			TypeDefinition? type = context.TryResolve (exportedType);
			if (type == null) {
				if (!context.IgnoreUnresolved)
					context.LogError (null, DiagnosticId.ExportedTypeCannotBeResolved, exportedType.Name);

				return;
			}

			context.Annotations.Mark (type, new DependencyInfo (DependencyKind.ExportedType, exportedType), new MessageOrigin (assembly));
			context.Annotations.SetMembersPreserve (type, members);
		}
	}
}