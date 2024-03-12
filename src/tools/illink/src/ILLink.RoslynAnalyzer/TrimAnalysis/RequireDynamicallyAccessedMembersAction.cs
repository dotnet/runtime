// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	internal partial struct RequireDynamicallyAccessedMembersAction
	{
		readonly ReflectionAccessAnalyzer _reflectionAccessAnalyzer;
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
#pragma warning disable IDE0060 // Unused parameters - should be removed once methods are actually implemented

		public RequireDynamicallyAccessedMembersAction (
			DiagnosticContext diagnosticContext,
			ReflectionAccessAnalyzer reflectionAccessAnalyzer)
		{
			_diagnosticContext = diagnosticContext;
			_reflectionAccessAnalyzer = reflectionAccessAnalyzer;
		}

		public partial bool TryResolveTypeNameAndMark (string typeName, bool needsAssemblyName, out TypeProxy type)
		{
			// TODO: Implement type name resolution to type symbol
			// https://github.com/dotnet/runtime/issues/95118

			// Important corner cases:
			//   IL2105 (see it's occurences in the tests) - non-assembly qualified type name which doesn't resolve warns
			//     - will need to figure out what analyzer should do around this.

			type = default;
			return false;
		}

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes) =>
			_reflectionAccessAnalyzer.GetReflectionAccessDiagnostics (_diagnosticContext, type.Type, dynamicallyAccessedMemberTypes);
	}
}
