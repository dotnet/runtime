// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using System.Collections.Immutable;

namespace ILLink.Shared.TrimAnalysis
{
	internal partial struct RequireDynamicallyAccessedMembersAction
	{
		readonly Compilation _compilation;
		readonly Location _location;
		readonly Action<Diagnostic>? _reportDiagnostic;
		readonly ReflectionAccessAnalyzer _reflectionAccessAnalyzer;
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
#pragma warning disable IDE0060 // Unused parameters - should be removed once methods are actually implemented

		public RequireDynamicallyAccessedMembersAction (
			Compilation compilation,
			Location location,
			Action<Diagnostic>? reportDiagnostic,
			ReflectionAccessAnalyzer reflectionAccessAnalyzer)
		{
			_compilation = compilation;
			_location = location;
			_reportDiagnostic = reportDiagnostic;
			_reflectionAccessAnalyzer = reflectionAccessAnalyzer;
			_diagnosticContext = new (location, reportDiagnostic);
		}

		public partial bool TryResolveTypeNameAndMark (string typeName, bool needsAssemblyName, out TypeProxy type)
		{
			var diagnosticContext = new DiagnosticContext (_location, _reportDiagnostic);
			if (_reflectionAccessAnalyzer.TryResolveTypeNameAndMark (_compilation, typeName, diagnosticContext, needsAssemblyName, out ITypeSymbol? foundType)) {
				if (foundType is INamedTypeSymbol namedType && namedType.IsGenericType)
					GenericArgumentDataFlow.ProcessGenericArgumentDataFlow (_compilation, _location, namedType, _reportDiagnostic);

				type = new TypeProxy (foundType);
				return true;
			}

			type = default;
			return false;
		}

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes) =>
			_reflectionAccessAnalyzer.GetReflectionAccessDiagnostics (_location, type.Type, dynamicallyAccessedMemberTypes);
	}
}
