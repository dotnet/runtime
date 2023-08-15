// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker
{
	public class MarkingHelpers
	{
		public LinkContext Context { get; }

		public MarkingHelpers (LinkContext context)
		{
			Context = context;
		}

		public void MarkMatchingExportedType (TypeDefinition typeToMatch, AssemblyDefinition assembly, in DependencyInfo reason, in MessageOrigin origin)
		{
			if (typeToMatch == null || assembly == null)
				return;

			if (assembly.MainModule.GetMatchingExportedType (typeToMatch, Context, out var exportedType))
				MarkExportedType (exportedType, assembly.MainModule, reason, origin);
		}

		public void MarkExportedType (ExportedType exportedType, ModuleDefinition module, in DependencyInfo reason, in MessageOrigin origin)
		{
			if (!Context.Annotations.MarkProcessed (exportedType, reason))
				return;

			Context.Annotations.Mark (module, reason, origin);
		}

		public void MarkForwardedScope (TypeReference typeReference, in MessageOrigin origin)
		{
			if (typeReference == null)
				return;

			if (typeReference.Scope is AssemblyNameReference) {
				var assembly = Context.Resolve (typeReference.Scope);
				if (assembly != null &&
					Context.TryResolve (typeReference) is TypeDefinition typeDefinition &&
					assembly.MainModule.GetMatchingExportedType (typeDefinition, Context, out var exportedType))
					MarkExportedType (exportedType, assembly.MainModule, new DependencyInfo (DependencyKind.ExportedType, typeReference), origin);
			}
		}
	}
}
