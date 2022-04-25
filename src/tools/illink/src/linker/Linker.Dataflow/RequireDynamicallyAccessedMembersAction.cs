// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
		readonly MessageOrigin _origin;

		public RequireDynamicallyAccessedMembersAction (
			LinkContext context,
			ReflectionMethodBodyScanner reflectionMethodBodyScanner,
			in MessageOrigin origin,
			bool diagnosticsEnabled)
		{
			_diagnosticContext = new DiagnosticContext (origin, diagnosticsEnabled, context);
			_context = context;
			_origin = origin;
			_reflectionMethodBodyScanner = reflectionMethodBodyScanner;
		}

		private partial bool TryResolveTypeNameAndMark (string typeName, out TypeProxy type)
		{
			if (!_context.TypeNameResolver.TryResolveTypeName (typeName, _origin.Provider, out TypeReference? typeRef, out AssemblyDefinition? typeAssembly)
				|| typeRef.ResolveToTypeDefinition (_context) is not TypeDefinition foundType) {
				type = default;
				return false;
			} else {
				_reflectionMethodBodyScanner.MarkType (_origin, typeRef);
				_context.MarkingHelpers.MarkMatchingExportedType (foundType, typeAssembly, new DependencyInfo (DependencyKind.DynamicallyAccessedMember, foundType), _origin);
				type = new TypeProxy (foundType);
				return true;
			}
		}

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			_reflectionMethodBodyScanner.MarkTypeForDynamicallyAccessedMembers (_origin, type.Type, dynamicallyAccessedMemberTypes, DependencyKind.DynamicallyAccessedMember);
		}
	}
}
