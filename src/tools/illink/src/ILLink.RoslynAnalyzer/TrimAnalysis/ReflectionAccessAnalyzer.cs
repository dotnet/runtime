// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	class ReflectionAccessAnalyzer
	{
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
		internal void GetReflectionAccessDiagnostics (in DiagnosticContext diagnosticContext, ITypeSymbol typeSymbol, DynamicallyAccessedMemberTypes requiredMemberTypes, bool declaredOnly = false)
		{
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

		static void ReportRequiresUnreferencedCodeDiagnostic (in DiagnosticContext diagnosticContext, AttributeData requiresAttributeData, ISymbol member)
		{
			var message = RequiresUnreferencedCodeUtils.GetMessageFromAttribute (requiresAttributeData);
			var url = RequiresAnalyzerBase.GetUrlFromAttribute (requiresAttributeData);
			diagnosticContext.AddDiagnostic (DiagnosticId.RequiresUnreferencedCode, member.GetDisplayName (), message, url);
		}

		internal static void GetReflectionAccessDiagnosticsForMethod (in DiagnosticContext diagnosticContext, IMethodSymbol methodSymbol)
		{
			if (methodSymbol.TryGetRequiresUnreferencedCodeAttribute (out var requiresAttributeData))
				ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, methodSymbol);

			if (!methodSymbol.IsStatic && methodSymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
				diagnosticContext.AddDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
			else if (methodSymbol.IsVirtual && FlowAnnotations.GetMethodReturnValueAnnotation (methodSymbol) != DynamicallyAccessedMemberTypes.None)
				diagnosticContext.AddDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
			else {
				foreach (var parameter in methodSymbol.Parameters) {
					if (FlowAnnotations.GetMethodParameterAnnotation (parameter) != DynamicallyAccessedMemberTypes.None) {
						diagnosticContext.AddDiagnostic (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection, methodSymbol.GetDisplayName ());
						break;
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
			if (fieldSymbol.TryGetRequiresUnreferencedCodeAttribute (out var requiresAttributeData))
				ReportRequiresUnreferencedCodeDiagnostic (diagnosticContext, requiresAttributeData, fieldSymbol);

			if (fieldSymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
				diagnosticContext.AddDiagnostic (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection, fieldSymbol.GetDisplayName ());
		}
	}
}
