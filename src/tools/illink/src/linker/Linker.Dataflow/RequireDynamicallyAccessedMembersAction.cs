// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct RequireDynamicallyAccessedMembersAction
	{
		readonly LinkContext _context;
		readonly ReflectionMethodBodyScanner _reflectionMethodBodyScanner;
		readonly ReflectionMethodBodyScanner.AnalysisContext _analysisContext;

		public RequireDynamicallyAccessedMembersAction (
			LinkContext context,
			ReflectionMethodBodyScanner reflectionMethodBodyScanner,
			in ReflectionMethodBodyScanner.AnalysisContext analysisContext)
		{
			_diagnosticContext = new DiagnosticContext (analysisContext.Origin, analysisContext.DiagnosticsEnabled, context);
			_context = context;
			_reflectionMethodBodyScanner = reflectionMethodBodyScanner;
			_analysisContext = analysisContext;
		}

		private partial bool TryResolveTypeNameAndMark (string typeName, out TypeProxy type)
		{
			if (!_context.TypeNameResolver.TryResolveTypeName (typeName, _analysisContext.Origin.Provider, out TypeReference? typeRef, out AssemblyDefinition? typeAssembly)
				|| ReflectionMethodBodyScanner.ResolveToTypeDefinition (_context, typeRef) is not TypeDefinition foundType) {
				type = default;
				return false;
			} else {
				_reflectionMethodBodyScanner.MarkType (_analysisContext, typeRef);
				_context.MarkingHelpers.MarkMatchingExportedType (foundType, typeAssembly, new DependencyInfo (DependencyKind.DynamicallyAccessedMember, foundType), _analysisContext.Origin);
				type = new TypeProxy (foundType);
				return true;
			}
		}

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			_reflectionMethodBodyScanner.MarkTypeForDynamicallyAccessedMembers (_analysisContext, type.Type, dynamicallyAccessedMemberTypes, DependencyKind.DynamicallyAccessedMember);
		}
	}
}
