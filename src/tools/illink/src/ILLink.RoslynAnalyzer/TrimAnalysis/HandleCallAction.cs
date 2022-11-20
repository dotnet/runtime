// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ILLink.RoslynAnalyzer;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct HandleCallAction
	{
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
#pragma warning disable IDE0060 // Unused parameters - the other partial implementation may need the parameter

		readonly ISymbol _owningSymbol;
		readonly IOperation _operation;
		readonly ReflectionAccessAnalyzer _reflectionAccessAnalyzer;

		public HandleCallAction (in DiagnosticContext diagnosticContext, ISymbol owningSymbol, IOperation operation)
		{
			_owningSymbol = owningSymbol;
			_operation = operation;
			_diagnosticContext = diagnosticContext;
			_annotations = FlowAnnotations.Instance;
			_reflectionAccessAnalyzer = new ReflectionAccessAnalyzer ();
			_requireDynamicallyAccessedMembersAction = new (diagnosticContext, _reflectionAccessAnalyzer);
		}

		private partial IEnumerable<SystemReflectionMethodBaseValue> GetMethodsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
		{
			foreach (var method in type.Type.GetMethodsOnTypeHierarchy (m => m.Name == name, bindingFlags))
				yield return new SystemReflectionMethodBaseValue (new MethodProxy (method));
		}

		private partial IEnumerable<SystemTypeValue> GetNestedTypesOnType (TypeProxy type, string name, BindingFlags? bindingFlags)
		{
			foreach (var nestedType in type.Type.GetNestedTypesOnType (t => t.Name == name, bindingFlags))
				yield return new SystemTypeValue (new TypeProxy (nestedType));
		}

		private partial bool MethodIsTypeConstructor (MethodProxy method)
		{
			if (!method.Method.IsConstructor ())
				return false;
			var type = method.Method.ContainingType;
			while (type is not null) {
				if (type.IsTypeOf (WellKnownType.System_Type))
					return true;
				type = type.BaseType;
			}
			return false;
		}

		private partial bool TryGetBaseType (TypeProxy type, out TypeProxy? baseType)
		{
			if (type.Type.BaseType is not null) {
				baseType = new TypeProxy (type.Type.BaseType);
				return true;
			}

			baseType = null;
			return false;
		}

		private partial bool TryResolveTypeNameForCreateInstanceAndMark (in MethodProxy calledMethod, string assemblyName, string typeName, out TypeProxy resolvedType)
		{
			// Intentionally never resolve anything. Analyzer can really only see types from the current compilation unit. For other assemblies
			// it typically only sees reference assemblies and thus just public API. It's not worth (at least for now) to try to resolve
			// the assembly name and type name as it should be rare this is actually ever used and even rarer to have problems (Warnings).
			// In any case the trimmer will process this correctly as it has a global view.
			resolvedType = default;
			return false;
		}

		// TODO: Does the analyzer need to do something here?
		private partial void MarkStaticConstructor (TypeProxy type) { }

		private partial void MarkEventsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForEventsOnTypeHierarchy (_diagnosticContext, type.Type, name, bindingFlags);

		private partial void MarkFieldsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForFieldsOnTypeHierarchy (_diagnosticContext, type.Type, name, bindingFlags);

		private partial void MarkPropertiesOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForPropertiesOnTypeHierarchy (_diagnosticContext, type.Type, name, bindingFlags);

		private partial void MarkPublicParameterlessConstructorOnType (TypeProxy type)
			=> _reflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForPublicParameterlessConstructor (_diagnosticContext, type.Type);

		private partial void MarkConstructorsOnType (TypeProxy type, BindingFlags? bindingFlags, int? parameterCount)
			=> _reflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForConstructorsOnType (_diagnosticContext, type.Type, bindingFlags, parameterCount);

		private partial void MarkMethod (MethodProxy method)
			=> ReflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForMethod (_diagnosticContext, method.Method);

		// TODO: Does the analyzer need to do something here?
		private partial void MarkType (TypeProxy type) { }

		private partial bool MarkAssociatedProperty (MethodProxy method)
		{
			if (method.Method.MethodKind == MethodKind.PropertyGet || method.Method.MethodKind == MethodKind.PropertySet) {
				var property = (IPropertySymbol) method.Method.AssociatedSymbol!;
				Debug.Assert (property != null);
				ReflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForProperty (_diagnosticContext, property!);
				return true;
			}

			return false;
		}

		private partial string GetContainingSymbolDisplayName () => _operation.FindContainingSymbol (_owningSymbol).GetDisplayName ();
	}
}
