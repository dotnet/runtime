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
		readonly ReflectionMarker _reflectionMarker;

		public RequireDynamicallyAccessedMembersAction (
			LinkContext context,
			ReflectionMarker reflectionMarker,
			in DiagnosticContext diagnosticContext)
		{
			_context = context;
			_reflectionMarker = reflectionMarker;
			_diagnosticContext = diagnosticContext;
		}

		public partial bool TryResolveTypeNameAndMark (string typeName, bool needsAssemblyName, out TypeProxy type)
		{
			if (_reflectionMarker.TryResolveTypeNameAndMark (typeName, _diagnosticContext.Origin, needsAssemblyName, out TypeDefinition? foundType)) {
				type = new (foundType);
				return true;
			} else {
				type = default;
				return false;
			}
		}

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			_reflectionMarker.MarkTypeForDynamicallyAccessedMembers (_diagnosticContext.Origin, type.Type, dynamicallyAccessedMemberTypes, DependencyKind.DynamicallyAccessedMember);
		}
	}
}
