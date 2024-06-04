// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	readonly struct ReflectionAccessAnalyzer
	{
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
		internal void GetReflectionAccessDiagnostics (in DiagnosticContext diagnosticContext, ITypeSymbol typeSymbol, DynamicallyAccessedMemberTypes requiredMemberTypes, bool declaredOnly = false)
		{
			typeSymbol = typeSymbol.OriginalDefinition;
			foreach (var member in typeSymbol.GetDynamicallyAccessedMembers (requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case IMethodSymbol method:
					GetReflectionAccessDiagnosticsForMethod (diagnosticContext, method);
					break;
				case IFieldSymbol field:
					GetDiagnosticsForField (diagnosticContext, field);
					break;
				case IPropertySymbol property:
					GetReflectionAccessDiagnosticsForProperty (diagnosticContext, property);
					break;
				/* Skip Type and InterfaceImplementation marking since doesnt seem relevant for diagnostic generation
				case ITypeSymbol nestedType:
					MarkType (diagnosticContext, nestedType);
					break;
				case InterfaceImplementation interfaceImplementation:
					MarkInterfaceImplementation (analysisContext, interfaceImplementation, dependencyKind);
					break;
				*/
				case IEventSymbol @event:
					GetDiagnosticsForEvent (diagnosticContext, @event);
					break;
				}
			}
		}

		internal void GetReflectionAccessDiagnosticsForEventsOnTypeHierarchy (in DiagnosticContext diagnosticContext, ITypeSymbol typeSymbol, string name, BindingFlags? bindingFlags)
		{
			foreach (var @event in typeSymbol.GetEventsOnTypeHierarchy (e => e.Name == name, bindingFlags))
				GetDiagnosticsForEvent (diagnosticContext, @event);
		}

		internal void GetReflectionAccessDiagnosticsForFieldsOnTypeHierarchy (in DiagnosticContext diagnosticContext, ITypeSymbol typeSymbol, string name, BindingFlags? bindingFlags)
		{
			foreach (var field in typeSymbol.GetFieldsOnTypeHierarchy (f => f.Name == name, bindingFlags))
				GetDiagnosticsForField (diagnosticContext, field);
		}

		internal void GetReflectionAccessDiagnosticsForPropertiesOnTypeHierarchy (in DiagnosticContext diagnosticContext, ITypeSymbol typeSymbol, string name, BindingFlags? bindingFlags)
		{
			foreach (var prop in typeSymbol.GetPropertiesOnTypeHierarchy (p => p.Name == name, bindingFlags))
				GetReflectionAccessDiagnosticsForProperty (diagnosticContext, prop);
		}

		internal void GetReflectionAccessDiagnosticsForConstructorsOnType (in DiagnosticContext diagnosticContext, ITypeSymbol typeSymbol, BindingFlags? bindingFlags, int? parameterCount)
		{
			foreach (var c in typeSymbol.GetConstructorsOnType (filter: parameterCount.HasValue ? c => c.Parameters.Length == parameterCount.Value : null, bindingFlags: bindingFlags))
				GetReflectionAccessDiagnosticsForMethod (diagnosticContext, c);
		}

		internal void GetReflectionAccessDiagnosticsForPublicParameterlessConstructor (in DiagnosticContext diagnosticContext, ITypeSymbol typeSymbol)
		{
			foreach (var c in typeSymbol.GetConstructorsOnType (filter: m => (m.DeclaredAccessibility == Accessibility.Public) && m.Parameters.Length == 0))
				GetReflectionAccessDiagnosticsForMethod (diagnosticContext, c);
		}

		static void ReportRequiresUnreferencedCodeDiagnostic (in DiagnosticContext diagnosticContext, AttributeData requiresAttributeData, ISymbol member)
		{
			var message = RequiresUnreferencedCodeUtils.GetMessageFromAttribute (requiresAttributeData);
			var url = RequiresAnalyzerBase.GetUrlFromAttribute (requiresAttributeData);
			diagnosticContext.AddDiagnostic (DiagnosticId.RequiresUnreferencedCode, member.GetDisplayName (), message, url);
		}

		internal static void GetReflectionAccessDiagnosticsForMethod (in DiagnosticContext diagnosticContext, IMethodSymbol methodSymbol)
		{
			if (methodSymbol.IsInRequiresUnreferencedCodeAttributeScope (out var requiresUnreferencedCodeAttributeData)) {
				ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresUnreferencedCodeAttributeData, methodSymbol);
			} else {
				foreach (var diagnostic in GetDiagnosticsForReflectionAccessToDAMOnMethod (diagnosticContext, methodSymbol))
					diagnosticContext.AddDiagnostic (diagnostic);
			}
		}

		internal static IEnumerable<Diagnostic> GetDiagnosticsForReflectionAccessToDAMOnMethod (DiagnosticContext diagnosticContext, IMethodSymbol methodSymbol)
		{
			if (methodSymbol.IsVirtual && FlowAnnotations.GetMethodReturnValueAnnotation (methodSymbol) != DynamicallyAccessedMemberTypes.None) {
				yield return diagnosticContext.CreateDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
			} else {
				foreach (var parameter in methodSymbol.GetParameters ()) {
					if (FlowAnnotations.GetMethodParameterAnnotation (parameter) != DynamicallyAccessedMemberTypes.None) {
						yield return diagnosticContext.CreateDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
						yield break;
					}
				}
			}
		}

		internal static void GetReflectionAccessDiagnosticsForProperty (in DiagnosticContext diagnosticContext, IPropertySymbol propertySymbol)
		{
			if (propertySymbol.SetMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (diagnosticContext, propertySymbol.SetMethod);
			if (propertySymbol.GetMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (diagnosticContext, propertySymbol.GetMethod);
		}

		static void GetDiagnosticsForEvent (in DiagnosticContext diagnosticContext, IEventSymbol eventSymbol)
		{
			if (eventSymbol.AddMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (diagnosticContext, eventSymbol.AddMethod);
			if (eventSymbol.RemoveMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (diagnosticContext, eventSymbol.RemoveMethod);
			if (eventSymbol.RaiseMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (diagnosticContext, eventSymbol.RaiseMethod);
		}

		static void GetDiagnosticsForField (in DiagnosticContext diagnosticContext, IFieldSymbol fieldSymbol)
		{
			if (fieldSymbol.TryGetRequiresUnreferencedCodeAttribute (out var requiresUnreferencedCodeAttributeData))
				ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresUnreferencedCodeAttributeData, fieldSymbol);

			if (fieldSymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
				diagnosticContext.AddDiagnostic (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection, fieldSymbol.GetDisplayName ());
		}
	}
}
