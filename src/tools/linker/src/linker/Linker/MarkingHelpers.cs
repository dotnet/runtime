// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;

namespace Mono.Linker
{
    public class MarkingHelpers
    {
        protected readonly LinkContext _context;

        public MarkingHelpers(LinkContext context)
        {
            _context = context;
        }

        public void MarkMatchingExportedType(TypeDefinition typeToMatch, AssemblyDefinition assembly, in DependencyInfo reason, in MessageOrigin origin)
        {
            if (typeToMatch == null || assembly == null)
                return;

            if (assembly.MainModule.GetMatchingExportedType(typeToMatch, out var exportedType))
                MarkExportedType(exportedType, assembly.MainModule, reason, origin);
        }

        public void MarkExportedType(ExportedType exportedType, ModuleDefinition module, in DependencyInfo reason, in MessageOrigin origin)
        {
            if (!_context.Annotations.MarkProcessed(exportedType, reason))
                return;

            _context.Annotations.Mark(module, reason, origin);
        }

        public void MarkForwardedScope(TypeReference typeReference, in MessageOrigin origin)
        {
            if (typeReference == null)
                return;

            if (typeReference.Scope is AssemblyNameReference)
            {
                var assembly = _context.Resolve(typeReference.Scope);
                if (assembly != null &&
                    _context.TryResolve(typeReference) is TypeDefinition typeDefinition &&
                    assembly.MainModule.GetMatchingExportedType(typeDefinition, out var exportedType))
                    MarkExportedType(exportedType, assembly.MainModule, new DependencyInfo(DependencyKind.ExportedType, typeReference), origin);
            }
        }
    }
}
