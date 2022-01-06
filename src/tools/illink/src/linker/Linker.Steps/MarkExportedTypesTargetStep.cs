// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

			TypeDefinition type = exportedType.Resolve ();
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