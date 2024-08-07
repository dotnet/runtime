// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
		readonly Action<Diagnostic>? _reportDiagnostic;

		public ReflectionAccessAnalyzer (Action<Diagnostic>? reportDiagnostic) => _reportDiagnostic = reportDiagnostic;

#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
		internal void GetReflectionAccessDiagnostics (Location location, ITypeSymbol typeSymbol, DynamicallyAccessedMemberTypes requiredMemberTypes, bool declaredOnly = false)
		{
			typeSymbol = typeSymbol.OriginalDefinition;
			foreach (var member in typeSymbol.GetDynamicallyAccessedMembers (requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case IMethodSymbol method:
					GetReflectionAccessDiagnosticsForMethod (location, method);
					break;
				case IFieldSymbol field:
					GetDiagnosticsForField (location, field);
					break;
				case IPropertySymbol property:
					GetReflectionAccessDiagnosticsForProperty (location, property);
					break;
				/* Skip Type and InterfaceImplementation marking since doesnt seem relevant for diagnostic generation
				case ITypeSymbol nestedType:
					MarkType (location, nestedType);
					break;
				case InterfaceImplementation interfaceImplementation:
					MarkInterfaceImplementation (location, interfaceImplementation, dependencyKind);
					break;
				*/
				case IEventSymbol @event:
					GetDiagnosticsForEvent (location, @event);
					break;
				}
			}
		}

		internal void GetReflectionAccessDiagnosticsForEventsOnTypeHierarchy (Location location, ITypeSymbol typeSymbol, string name, BindingFlags? bindingFlags)
		{
			foreach (var @event in typeSymbol.GetEventsOnTypeHierarchy (e => e.Name == name, bindingFlags))
				GetDiagnosticsForEvent (location, @event);
		}

		internal void GetReflectionAccessDiagnosticsForFieldsOnTypeHierarchy (Location location, ITypeSymbol typeSymbol, string name, BindingFlags? bindingFlags)
		{
			foreach (var field in typeSymbol.GetFieldsOnTypeHierarchy (f => f.Name == name, bindingFlags))
				GetDiagnosticsForField (location, field);
		}

		internal void GetReflectionAccessDiagnosticsForPropertiesOnTypeHierarchy (Location location, ITypeSymbol typeSymbol, string name, BindingFlags? bindingFlags)
		{
			foreach (var prop in typeSymbol.GetPropertiesOnTypeHierarchy (p => p.Name == name, bindingFlags))
				GetReflectionAccessDiagnosticsForProperty (location, prop);
		}

		internal void GetReflectionAccessDiagnosticsForConstructorsOnType (Location location, ITypeSymbol typeSymbol, BindingFlags? bindingFlags, int? parameterCount)
		{
			foreach (var c in typeSymbol.GetConstructorsOnType (filter: parameterCount.HasValue ? c => c.Parameters.Length == parameterCount.Value : null, bindingFlags: bindingFlags))
				GetReflectionAccessDiagnosticsForMethod (location, c);
		}

		internal void GetReflectionAccessDiagnosticsForPublicParameterlessConstructor (Location location, ITypeSymbol typeSymbol)
		{
			foreach (var c in typeSymbol.GetConstructorsOnType (filter: m => (m.DeclaredAccessibility == Accessibility.Public) && m.Parameters.Length == 0))
				GetReflectionAccessDiagnosticsForMethod (location, c);
		}

		void ReportRequiresUnreferencedCodeDiagnostic (Location location, AttributeData requiresAttributeData, ISymbol member)
		{
			var message = RequiresUnreferencedCodeUtils.GetMessageFromAttribute (requiresAttributeData);
			var url = RequiresAnalyzerBase.GetUrlFromAttribute (requiresAttributeData);
			var diagnosticContext = new DiagnosticContext (location, _reportDiagnostic);
			diagnosticContext.AddDiagnostic (DiagnosticId.RequiresUnreferencedCode, member.GetDisplayName (), message, url);
		}

		internal void GetReflectionAccessDiagnosticsForMethod (Location location, IMethodSymbol methodSymbol)
		{
			if (methodSymbol.IsInRequiresUnreferencedCodeAttributeScope (out var requiresUnreferencedCodeAttributeData)) {
				ReportRequiresUnreferencedCodeDiagnostic (location, requiresUnreferencedCodeAttributeData, methodSymbol);
			} else {
				GetDiagnosticsForReflectionAccessToDAMOnMethod (location, methodSymbol);
			}
		}

		internal void GetDiagnosticsForReflectionAccessToDAMOnMethod (Location location, IMethodSymbol methodSymbol)
		{
			var diagnosticContext = new DiagnosticContext (location, _reportDiagnostic);
			if (methodSymbol.IsVirtual && FlowAnnotations.GetMethodReturnValueAnnotation (methodSymbol) != DynamicallyAccessedMemberTypes.None) {
				diagnosticContext.AddDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
			} else {
				foreach (var parameter in methodSymbol.GetParameters ()) {
					if (FlowAnnotations.GetMethodParameterAnnotation (parameter) != DynamicallyAccessedMemberTypes.None) {
						diagnosticContext.AddDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
						break;
					}
				}
			}
		}

		internal void GetReflectionAccessDiagnosticsForProperty (Location location, IPropertySymbol propertySymbol)
		{
			if (propertySymbol.SetMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (location, propertySymbol.SetMethod);
			if (propertySymbol.GetMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (location, propertySymbol.GetMethod);
		}

		void GetDiagnosticsForEvent (Location location, IEventSymbol eventSymbol)
		{
			if (eventSymbol.AddMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (location, eventSymbol.AddMethod);
			if (eventSymbol.RemoveMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (location, eventSymbol.RemoveMethod);
			if (eventSymbol.RaiseMethod is not null)
				GetReflectionAccessDiagnosticsForMethod (location, eventSymbol.RaiseMethod);
		}

		void GetDiagnosticsForField (Location location, IFieldSymbol fieldSymbol)
		{
			if (fieldSymbol.TryGetRequiresUnreferencedCodeAttribute (out var requiresUnreferencedCodeAttributeData))
				ReportRequiresUnreferencedCodeDiagnostic (location, requiresUnreferencedCodeAttributeData, fieldSymbol);

			if (fieldSymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None) {
				var diagnosticContext = new DiagnosticContext (location, _reportDiagnostic);
				diagnosticContext.AddDiagnostic (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection, fieldSymbol.GetDisplayName ());
			}
		}
	}
}
