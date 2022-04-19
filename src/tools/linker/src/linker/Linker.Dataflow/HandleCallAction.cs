// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct HandleCallAction
	{
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods

		readonly LinkContext _context;
		readonly ReflectionMethodBodyScanner _reflectionMethodBodyScanner;
		readonly ReflectionMethodBodyScanner.AnalysisContext _analysisContext;
		readonly MethodDefinition _callingMethodDefinition;

		public HandleCallAction (
			LinkContext context,
			ReflectionMethodBodyScanner reflectionMethodBodyScanner,
			in ReflectionMethodBodyScanner.AnalysisContext analysisContext,
			MethodDefinition callingMethodDefinition)
		{
			_context = context;
			_reflectionMethodBodyScanner = reflectionMethodBodyScanner;
			_analysisContext = analysisContext;
			_callingMethodDefinition = callingMethodDefinition;
			_diagnosticContext = new DiagnosticContext (analysisContext.Origin, analysisContext.DiagnosticsEnabled, context);
			_requireDynamicallyAccessedMembersAction = new (context, reflectionMethodBodyScanner, analysisContext);
		}

		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method)
			=> _context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (method.Method);

		private partial bool MethodIsTypeConstructor (MethodProxy method)
		{
			if (!method.Method.IsConstructor)
				return false;
			TypeDefinition? type = method.Method.DeclaringType;
			while (type is not null) {
				if (type.IsTypeOf (WellKnownType.System_Type))
					return true;
				type = _context.Resolve (type.BaseType);
			}
			return false;
		}

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method)
			=> _context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (method.Method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (ReflectionMethodBodyScanner.ResolveToTypeDefinition (_context, method.Method.ReturnType), method.Method, dynamicallyAccessedMemberTypes);

		private partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter)
			=> new (genericParameter.GenericParameter, _context.Annotations.FlowAnnotations.GetGenericParameterAnnotation (genericParameter.GenericParameter));

		private partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method)
			=> GetMethodThisParameterValue (method, _context.Annotations.FlowAnnotations.GetParameterAnnotation (method.Method, 0));

		private partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method, dynamicallyAccessedMemberTypes);

		private partial DynamicallyAccessedMemberTypes GetMethodParameterAnnotation (MethodProxy method, int parameterIndex)
		{
			Debug.Assert (method.Method.Parameters.Count > parameterIndex);
			return _context.Annotations.FlowAnnotations.GetParameterAnnotation (method.Method, parameterIndex + (method.IsStatic () ? 0 : 1));
		}

		private partial MethodParameterValue GetMethodParameterValue (MethodProxy method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (
				ReflectionMethodBodyScanner.ResolveToTypeDefinition (_context, method.Method.Parameters[parameterIndex].ParameterType),
				method.Method,
				parameterIndex,
				dynamicallyAccessedMemberTypes);

		private partial IEnumerable<SystemReflectionMethodBaseValue> GetMethodsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
		{
			foreach (var method in type.Type.GetMethodsOnTypeHierarchy (_context, m => m.Name == name, bindingFlags))
				yield return new SystemReflectionMethodBaseValue (new MethodProxy (method));
		}

		private partial IEnumerable<SystemTypeValue> GetNestedTypesOnType (TypeProxy type, string name, BindingFlags? bindingFlags)
		{
			foreach (var nestedType in type.Type.GetNestedTypesOnType (t => t.Name == name, bindingFlags))
				yield return new SystemTypeValue (new TypeProxy (nestedType));
		}

		private partial bool TryGetBaseType (TypeProxy type, out TypeProxy? baseType)
		{
			if (type.Type.BaseType is TypeReference baseTypeRef && _context.TryResolve (baseTypeRef) is TypeDefinition baseTypeDefinition) {
				baseType = new TypeProxy (baseTypeDefinition);
				return true;
			}

			baseType = null;
			return false;
		}

		private partial void MarkStaticConstructor (TypeProxy type)
			=> _reflectionMethodBodyScanner.MarkStaticConstructor (_analysisContext, type.Type);

		private partial void MarkEventsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionMethodBodyScanner.MarkEventsOnTypeHierarchy (_analysisContext, type.Type, e => e.Name == name, bindingFlags);

		private partial void MarkFieldsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionMethodBodyScanner.MarkFieldsOnTypeHierarchy (_analysisContext, type.Type, f => f.Name == name, bindingFlags);

		private partial void MarkPropertiesOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionMethodBodyScanner.MarkPropertiesOnTypeHierarchy (_analysisContext, type.Type, p => p.Name == name, bindingFlags);

		private partial void MarkPublicParameterlessConstructorOnType (TypeProxy type)
			=> _reflectionMethodBodyScanner.MarkConstructorsOnType (_analysisContext, type.Type, m => m.IsPublic && m.Parameters.Count == 0);

		private partial void MarkConstructorsOnType (TypeProxy type, BindingFlags? bindingFlags)
			=> _reflectionMethodBodyScanner.MarkConstructorsOnType (_analysisContext, type.Type, null, bindingFlags);

		private partial void MarkMethod (MethodProxy method)
			=> _reflectionMethodBodyScanner.MarkMethod (_analysisContext, method.Method);

		private partial void MarkType (TypeProxy type)
			=> _reflectionMethodBodyScanner.MarkType (_analysisContext, type.Type);

		private partial bool MarkAssociatedProperty (MethodProxy method)
		{
			if (method.Method.TryGetProperty (out PropertyDefinition? propertyDefinition)) {
				_reflectionMethodBodyScanner.MarkProperty (_analysisContext, propertyDefinition);
				return true;
			}

			return false;
		}

		private partial string GetContainingSymbolDisplayName () => _callingMethodDefinition.GetDisplayName ();
	}
}
