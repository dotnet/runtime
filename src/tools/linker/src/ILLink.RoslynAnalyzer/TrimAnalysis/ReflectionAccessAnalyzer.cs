// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
					GetDiagnosticsForMethod (diagnosticContext, method);
					break;
				case IFieldSymbol field:
					GetDiagnosticsForField (diagnosticContext, field);
					break;
				case IPropertySymbol property:
					GetDiagnosticsForProperty (diagnosticContext, property);
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

		static void ReportRequiresUnreferencedCodeDiagnostic (in DiagnosticContext diagnosticContext, AttributeData requiresAttributeData, ISymbol member)
		{
			var message = RequiresUnreferencedCodeUtils.GetMessageFromAttribute (requiresAttributeData);
			var url = RequiresAnalyzerBase.GetUrlFromAttribute (requiresAttributeData);
			diagnosticContext.AddDiagnostic (DiagnosticId.RequiresUnreferencedCode, member.GetDisplayName (), message, url);
		}

		static void GetDiagnosticsForMethod (in DiagnosticContext diagnosticContext, IMethodSymbol methodSymbol)
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

		static void GetDiagnosticsForProperty (in DiagnosticContext diagnosticContext, IPropertySymbol propertySymbol)
		{
			if (propertySymbol.SetMethod is not null)
				GetDiagnosticsForMethod (diagnosticContext, propertySymbol.SetMethod);
			if (propertySymbol.GetMethod is not null)
				GetDiagnosticsForMethod (diagnosticContext, propertySymbol.GetMethod);
		}

		static void GetDiagnosticsForEvent (in DiagnosticContext diagnosticContext, IEventSymbol eventSymbol)
		{
			if (eventSymbol.AddMethod is not null)
				GetDiagnosticsForMethod (diagnosticContext, eventSymbol.AddMethod);
			if (eventSymbol.RemoveMethod is not null)
				GetDiagnosticsForMethod (diagnosticContext, eventSymbol.RemoveMethod);
			if (eventSymbol.RaiseMethod is not null)
				GetDiagnosticsForMethod (diagnosticContext, eventSymbol.RaiseMethod);
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
