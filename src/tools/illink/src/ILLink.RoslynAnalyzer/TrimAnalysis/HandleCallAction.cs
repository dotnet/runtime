// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
			_reflectionAccessAnalyzer = new ReflectionAccessAnalyzer ();
			_requireDynamicallyAccessedMembersAction = new (diagnosticContext, _reflectionAccessAnalyzer);
		}

		// TODO: This is relatively expensive on the analyzer since it doesn't cache the annotation information
		// In linker this is an optimization to avoid the heavy lifting of analysis if there's no point
		// it's unclear if the same optimization makes sense for the analyzer.
		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method) => FlowAnnotations.RequiresDataFlowAnalysis (method.Method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method) => FlowAnnotations.GetMethodReturnValueAnnotation (method.Method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method, dynamicallyAccessedMemberTypes);

		private partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter)
			=> new (genericParameter.TypeParameterSymbol);

		private partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method, dynamicallyAccessedMemberTypes);

		private partial MethodParameterValue GetMethodParameterValue (MethodProxy method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method.Parameters[parameterIndex], dynamicallyAccessedMemberTypes);

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

		// TODO: Does the analyzer need to do something here?
		private partial void MarkStaticConstructor (TypeProxy type) { }

		private partial void MarkEventsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForEventsOnTypeHierarchy (_diagnosticContext, type.Type, name, bindingFlags);

		private partial void MarkFieldsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForFieldsOnTypeHierarchy (_diagnosticContext, type.Type, name, bindingFlags);

		private partial void MarkPropertiesOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionAccessAnalyzer.GetReflectionAccessDiagnosticsForPropertiesOnTypeHierarchy (_diagnosticContext, type.Type, name, bindingFlags);

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
