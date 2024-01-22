// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	internal static class GenericArgumentDataFlow
	{
		public static void ProcessGenericArgumentDataFlow (DiagnosticContext diagnosticContext, INamedTypeSymbol type)
		{
			ProcessGenericArgumentDataFlow (diagnosticContext, type.TypeArguments, type.TypeParameters);
		}

		public static void ProcessGenericArgumentDataFlow (DiagnosticContext diagnosticContext, IMethodSymbol method)
		{
			ProcessGenericArgumentDataFlow (diagnosticContext, method.TypeArguments, method.TypeParameters);

			ProcessGenericArgumentDataFlow (diagnosticContext, method.ContainingType);
		}

		public static void ProcessGenericArgumentDataFlow (DiagnosticContext diagnosticContext, IFieldSymbol field)
		{
			ProcessGenericArgumentDataFlow (diagnosticContext, field.ContainingType);
		}

		static void ProcessGenericArgumentDataFlow (
			DiagnosticContext diagnosticContext,
			ImmutableArray<ITypeSymbol> typeArguments,
			ImmutableArray<ITypeParameterSymbol> typeParameters)
		{
			for (int i = 0; i < typeArguments.Length; i++) {
				var typeArgument = typeArguments[i];
				// Apply annotations to the generic argument
				var genericParameterValue = new GenericParameterValue (typeParameters[i]);
				if (genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None) {
					SingleValue genericArgumentValue = SingleValueExtensions.FromTypeSymbol (typeArgument)!;
					var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (diagnosticContext, default (ReflectionAccessAnalyzer));
					requireDynamicallyAccessedMembersAction.Invoke (genericArgumentValue, genericParameterValue);
				}

				// Recursively process generic argument data flow on the generic argument if it itself is generic
				if (typeArgument is INamedTypeSymbol namedTypeArgument && namedTypeArgument.IsGenericType) {
					ProcessGenericArgumentDataFlow (diagnosticContext, namedTypeArgument);
				}
			}
		}

		public static bool RequiresGenericArgumentDataFlow (INamedTypeSymbol type)
		{
			if (type.IsGenericType) {
				if (RequiresGenericArgumentDataFlow (type.TypeParameters))
					return true;

				foreach (var typeArgument in type.TypeArguments) {
					if (typeArgument is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType
						&& RequiresGenericArgumentDataFlow (namedTypeSymbol))
						return true;
				}
			}

			return false;
		}

		public static bool RequiresGenericArgumentDataFlow (IMethodSymbol method)
		{
			if (method.IsGenericMethod) {
				if (RequiresGenericArgumentDataFlow (method.TypeParameters))
					return true;

				foreach (var typeArgument in method.TypeArguments) {
					if (typeArgument is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType
						&& RequiresGenericArgumentDataFlow (namedTypeSymbol))
						return true;
				}
			}

			return RequiresGenericArgumentDataFlow (method.ContainingType);
		}

		public static bool RequiresGenericArgumentDataFlow (IFieldSymbol field)
		{
			return RequiresGenericArgumentDataFlow (field.ContainingType);
		}

		static bool RequiresGenericArgumentDataFlow (ImmutableArray<ITypeParameterSymbol> typeParameters)
		{
			foreach (var typeParameter in typeParameters) {
				var genericParameterValue = new GenericParameterValue (typeParameter);
				if (genericParameterValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
					return true;
			}

			return false;
		}
	}
}
