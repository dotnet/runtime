// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class DynamicallyAccessedMembersAnalyzer : DiagnosticAnalyzer
	{
		internal const string DynamicallyAccessedMembers = nameof (DynamicallyAccessedMembers);
		internal const string DynamicallyAccessedMembersAttribute = nameof (DynamicallyAccessedMembersAttribute);
		public const string attributeArgument = "attributeArgument";
		public const string FullyQualifiedDynamicallyAccessedMembersAttribute = "System.Diagnostics.CodeAnalysis." + DynamicallyAccessedMembersAttribute;
		public const string FullyQualifiedFeatureGuardAttribute  = "System.Diagnostics.CodeAnalysis.FeatureGuardAttribute";
		public static Lazy<ImmutableArray<RequiresAnalyzerBase>> RequiresAnalyzers { get; } = new Lazy<ImmutableArray<RequiresAnalyzerBase>> (GetRequiresAnalyzers);
		static ImmutableArray<RequiresAnalyzerBase> GetRequiresAnalyzers () =>
			ImmutableArray.Create<RequiresAnalyzerBase> (
				new RequiresAssemblyFilesAnalyzer (),
				new RequiresUnreferencedCodeAnalyzer (),
				new RequiresDynamicCodeAnalyzer ());

		public static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics ()
		{
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> (26);
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersIsNotAllowedOnMethods));
			AddRange (DiagnosticId.MethodParameterCannotBeStaticallyDetermined, DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter);
			AddRange (DiagnosticId.DynamicallyAccessedMembersOnFieldCanOnlyApplyToTypesOrStrings, DiagnosticId.DynamicallyAccessedMembersOnPropertyCanOnlyApplyToTypesOrStrings);
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnrecognizedTypeInRuntimeHelpersRunClassConstructor));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnImplicitThisBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.PropertyAccessorParameterInLinqExpressionsCannotBeStaticallyDetermined));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.MakeGenericType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.MakeGenericMethod));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.CaseInsensitiveTypeGetTypeCallIsNotSupported));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnrecognizedTypeNameInTypeGetType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.UnrecognizedParameterInMethodCreateInstance));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ParametersOfAssemblyCreateInstanceCannotBeAnalyzed));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ReturnValueDoesNotMatchFeatureGuards));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.InvalidFeatureGuard));

			foreach (var requiresAnalyzer in RequiresAnalyzers.Value) {
				foreach (var diagnosticDescriptor in requiresAnalyzer.SupportedDiagnostics)
					diagDescriptorsArrayBuilder.Add (diagnosticDescriptor);
			}

			return diagDescriptorsArrayBuilder.ToImmutable ();

			void AddRange (DiagnosticId first, DiagnosticId last)
			{
				Debug.Assert ((int) first < (int) last);

				for (int i = (int) first;
					i <= (int) last; i++) {
					diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor ((DiagnosticId) i));
				}
			}
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		static Location GetPrimaryLocation (ImmutableArray<Location> locations) => locations.Length > 0 ? locations[0] : Location.None;

		public override void Initialize (AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

			if (!Debugger.IsAttached)
				context.EnableConcurrentExecution ();

			context.RegisterCompilationStartAction (context => {
				var dataFlowAnalyzerContext = DataFlowAnalyzerContext.Create (context.Options, context.Compilation, RequiresAnalyzers.Value);
				if (!dataFlowAnalyzerContext.AnyAnalyzersEnabled)
					return;

				context.RegisterOperationBlockAction (context => {
					foreach (var operationBlock in context.OperationBlocks) {
						TrimDataFlowAnalysis trimDataFlowAnalysis = new (context, dataFlowAnalyzerContext, operationBlock);
						trimDataFlowAnalysis.InterproceduralAnalyze ();
						foreach (var diagnostic in trimDataFlowAnalysis.CollectDiagnostics ())
							context.ReportDiagnostic (diagnostic);
					}
				});

				// Remaining actions are only for DynamicallyAccessedMembers analysis.
				if (!dataFlowAnalyzerContext.EnableTrimAnalyzer)
					return;

				// Examine generic instantiations in base types and interface list
				context.RegisterSymbolAction (context => {
					var type = (INamedTypeSymbol) context.Symbol;
					// RUC on type doesn't silence DAM warnings about generic base/interface types.
					// This knowledge lives in IsInRequiresUnreferencedCodeAttributeScope,
					// which we still call for consistency here, but it is expected to return false.
					if (type.IsInRequiresUnreferencedCodeAttributeScope (out _))
						return;

					var location = GetPrimaryLocation (type.Locations);
					DiagnosticContext diagnosticContext = new (location);

					if (type.BaseType is INamedTypeSymbol baseType)
						GenericArgumentDataFlow.ProcessGenericArgumentDataFlow (diagnosticContext, baseType);

					foreach (var interfaceType in type.Interfaces)
						GenericArgumentDataFlow.ProcessGenericArgumentDataFlow (diagnosticContext, interfaceType);

					foreach (var diagnostic in diagnosticContext.Diagnostics)
						context.ReportDiagnostic (diagnostic);
				}, SymbolKind.NamedType);
				context.RegisterSymbolAction (context => {
					VerifyMemberOnlyApplyToTypesOrStrings (context, context.Symbol);
					VerifyDamOnPropertyAndAccessorMatch (context, (IMethodSymbol) context.Symbol);
					VerifyDamOnDerivedAndBaseMethodsMatch (context, (IMethodSymbol) context.Symbol);
				}, SymbolKind.Method);
				context.RegisterSymbolAction (context => {
					VerifyDamOnInterfaceAndImplementationMethodsMatch (context, (INamedTypeSymbol) context.Symbol);
				}, SymbolKind.NamedType);
				context.RegisterSymbolAction (context => {
					VerifyMemberOnlyApplyToTypesOrStrings (context, context.Symbol);
				}, SymbolKind.Property);
				context.RegisterSymbolAction (context => {
					VerifyMemberOnlyApplyToTypesOrStrings (context, context.Symbol);
				}, SymbolKind.Field);
			});
		}

		static void VerifyMemberOnlyApplyToTypesOrStrings (SymbolAnalysisContext context, ISymbol member)
		{
			var location = GetPrimaryLocation (member.Locations);
			if (member is IFieldSymbol field && field.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None && !field.Type.IsTypeInterestingForDataflow ())
				context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnFieldCanOnlyApplyToTypesOrStrings), location, member.GetDisplayName ()));
			else if (member is IMethodSymbol method) {
				if (method.GetDynamicallyAccessedMemberTypesOnReturnType () != DynamicallyAccessedMemberTypes.None && !method.ReturnType.IsTypeInterestingForDataflow ())
					context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings), location, member.GetDisplayName ()));
				if (method.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None && !method.ContainingType.IsTypeInterestingForDataflow ())
					context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersIsNotAllowedOnMethods), location));
				foreach (var parameter in method.Parameters) {
					if (parameter.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None && !parameter.Type.IsTypeInterestingForDataflow ())
						context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnMethodParameterCanOnlyApplyToTypesOrStrings), location, parameter.GetDisplayName (), member.GetDisplayName ()));
				}
			} else if (member is IPropertySymbol property && property.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None && !property.Type.IsTypeInterestingForDataflow ()) {
				context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersOnPropertyCanOnlyApplyToTypesOrStrings), location, member.GetDisplayName ()));
			}
		}

		static void VerifyDamOnDerivedAndBaseMethodsMatch (SymbolAnalysisContext context, IMethodSymbol methodSymbol)
		{
			if (methodSymbol.TryGetOverriddenMember (out var overriddenSymbol) && overriddenSymbol is IMethodSymbol overriddenMethod
				&& context.Symbol is IMethodSymbol method) {
				VerifyDamOnMethodsMatch (context, method, overriddenMethod);
			}
		}

		static void VerifyDamOnMethodsMatch (SymbolAnalysisContext context, IMethodSymbol overrideMethod, IMethodSymbol baseMethod)
		{
			var overrideMethodReturnAnnotation = FlowAnnotations.GetMethodReturnValueAnnotation (overrideMethod);
			var baseMethodReturnAnnotation = FlowAnnotations.GetMethodReturnValueAnnotation (baseMethod);
			if (overrideMethodReturnAnnotation != baseMethodReturnAnnotation) {

				(IMethodSymbol attributableMethod, DynamicallyAccessedMemberTypes missingAttribute) = GetTargetAndRequirements (overrideMethod,
					baseMethod, overrideMethodReturnAnnotation, baseMethodReturnAnnotation);

				Location attributableSymbolLocation = GetPrimaryLocation (attributableMethod.Locations);

				// code fix does not support merging multiple attributes. If an attribute is present or the method is not in source, do not provide args for code fix.
				(Location[]? sourceLocation, Dictionary<string, string?>? DAMArgs) = (!attributableSymbolLocation.IsInSource
					|| (overrideMethod.TryGetReturnAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _)
						&& baseMethod.TryGetReturnAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _))
						) ? (null, null) : CreateArguments (attributableSymbolLocation, missingAttribute);

				context.ReportDiagnostic (Diagnostic.Create (
					DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides),
					GetPrimaryLocation (overrideMethod.Locations), sourceLocation, DAMArgs?.ToImmutableDictionary (), overrideMethod.GetDisplayName (), baseMethod.GetDisplayName ()));
			}

			foreach (var overrideParam in overrideMethod.GetMetadataParameters ()) {
				var baseParam = baseMethod.GetParameter (overrideParam.Index);
				var baseParameterAnnotation = FlowAnnotations.GetMethodParameterAnnotation (baseParam);
				var overrideParameterAnnotation = FlowAnnotations.GetMethodParameterAnnotation (overrideParam);
				if (overrideParameterAnnotation != baseParameterAnnotation) {
					(IMethodSymbol attributableMethod, DynamicallyAccessedMemberTypes missingAttribute) = GetTargetAndRequirements (overrideMethod,
						baseMethod, overrideParameterAnnotation, baseParameterAnnotation);

					Location attributableSymbolLocation = attributableMethod.GetParameter (overrideParam.Index).Location!;

					// code fix does not support merging multiple attributes. If an attribute is present or the method is not in source, do not provide args for code fix.
					(Location[]? sourceLocation, Dictionary<string, string?>? DAMArgs) = (!attributableSymbolLocation.IsInSource
						|| (overrideParam.ParameterSymbol!.TryGetAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _)
							&& baseParam.ParameterSymbol!.TryGetAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _))
							) ? (null, null) : CreateArguments (attributableSymbolLocation, missingAttribute);

					context.ReportDiagnostic (Diagnostic.Create (
						DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides),
						overrideParam.Location, sourceLocation, DAMArgs?.ToImmutableDictionary (),
						overrideParam.GetDisplayName (), overrideMethod.GetDisplayName (), baseParam.GetDisplayName (), baseMethod.GetDisplayName ()));
				}
			}

			for (int i = 0; i < overrideMethod.TypeParameters.Length; i++) {
				var methodTypeParameterAnnotation = overrideMethod.TypeParameters[i].GetDynamicallyAccessedMemberTypes ();
				var overriddenMethodTypeParameterAnnotation = baseMethod.TypeParameters[i].GetDynamicallyAccessedMemberTypes ();
				if (methodTypeParameterAnnotation != overriddenMethodTypeParameterAnnotation) {

					(IMethodSymbol attributableMethod, DynamicallyAccessedMemberTypes missingAttribute) = GetTargetAndRequirements (overrideMethod, baseMethod, methodTypeParameterAnnotation, overriddenMethodTypeParameterAnnotation);

					var attributableSymbol = attributableMethod.TypeParameters[i];
					Location attributableSymbolLocation = GetPrimaryLocation (attributableSymbol.Locations);

					// code fix does not support merging multiple attributes. If an attribute is present or the method is not in source, do not provide args for code fix.
					(Location[]? sourceLocation, Dictionary<string, string?>? DAMArgs) = (!attributableSymbolLocation.IsInSource
						|| (overrideMethod.TypeParameters[i].TryGetAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _)
							&& baseMethod.TypeParameters[i].TryGetAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute, out var _))
							) ? (null, null) : CreateArguments (attributableSymbolLocation, missingAttribute);

					context.ReportDiagnostic (Diagnostic.Create (
						DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides),
						GetPrimaryLocation (overrideMethod.TypeParameters[i].Locations), sourceLocation, DAMArgs?.ToImmutableDictionary (),
						overrideMethod.TypeParameters[i].GetDisplayName (), overrideMethod.GetDisplayName (),
						baseMethod.TypeParameters[i].GetDisplayName (), baseMethod.GetDisplayName ()));
				}
			}

			if (!overrideMethod.IsStatic && overrideMethod.GetDynamicallyAccessedMemberTypes () != baseMethod.GetDynamicallyAccessedMemberTypes ())
				context.ReportDiagnostic (Diagnostic.Create (
					DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnImplicitThisBetweenOverrides),
					GetPrimaryLocation (overrideMethod.Locations),
					overrideMethod.GetDisplayName (), baseMethod.GetDisplayName ()));
		}

		static void VerifyDamOnInterfaceAndImplementationMethodsMatch (SymbolAnalysisContext context, INamedTypeSymbol type)
		{
			foreach (var (interfaceMember, implementationMember) in type.GetMemberInterfaceImplementationPairs ()) {
				if (implementationMember is IMethodSymbol implementationMethod
					&& interfaceMember is IMethodSymbol interfaceMethod)
					VerifyDamOnMethodsMatch (context, implementationMethod, interfaceMethod);
			}
		}

		static void VerifyDamOnPropertyAndAccessorMatch (SymbolAnalysisContext context, IMethodSymbol methodSymbol)
		{
			if ((methodSymbol.MethodKind != MethodKind.PropertyGet && methodSymbol.MethodKind != MethodKind.PropertySet)
				|| (methodSymbol.AssociatedSymbol?.GetDynamicallyAccessedMemberTypes () == DynamicallyAccessedMemberTypes.None))
				return;

			// None on the return type of 'get' matches unannotated
			if (methodSymbol.MethodKind == MethodKind.PropertyGet
				&& methodSymbol.GetDynamicallyAccessedMemberTypesOnReturnType () != DynamicallyAccessedMemberTypes.None
				// None on parameter of 'set' matches unannotated
				|| methodSymbol.MethodKind == MethodKind.PropertySet
				&& methodSymbol.Parameters[methodSymbol.Parameters.Length - 1].GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None) {
				var associatedSymbol = methodSymbol.AssociatedSymbol!;
				context.ReportDiagnostic (Diagnostic.Create (
					DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersConflictsBetweenPropertyAndAccessor),
					GetPrimaryLocation (associatedSymbol.Locations),
					associatedSymbol.GetDisplayName (),
					methodSymbol.GetDisplayName ()
				));
				return;
			}
		}

		private static (IMethodSymbol Method, DynamicallyAccessedMemberTypes Requirements) GetTargetAndRequirements (IMethodSymbol method, IMethodSymbol overriddenMethod, DynamicallyAccessedMemberTypes methodAnnotation, DynamicallyAccessedMemberTypes overriddenMethodAnnotation)
		{
			DynamicallyAccessedMemberTypes mismatchedArgument;
			IMethodSymbol paramNeedsAttributes;
			if (methodAnnotation == DynamicallyAccessedMemberTypes.None) {
				mismatchedArgument = overriddenMethodAnnotation;
				paramNeedsAttributes = method;
			} else {
				mismatchedArgument = methodAnnotation;
				paramNeedsAttributes = overriddenMethod;
			}
			return (paramNeedsAttributes, mismatchedArgument);
		}

		private static (Location[]?, Dictionary<string, string?>?) CreateArguments (Location attributableSymbolLocation, DynamicallyAccessedMemberTypes mismatchedArgument)
		{
			Dictionary<string, string?>? DAMArgument = new ();
			Location[]? sourceLocation = new Location[] { attributableSymbolLocation };
			DAMArgument.Add (DynamicallyAccessedMembersAnalyzer.attributeArgument, mismatchedArgument.ToString ());
			return (sourceLocation, DAMArgument);
		}
	}
}
