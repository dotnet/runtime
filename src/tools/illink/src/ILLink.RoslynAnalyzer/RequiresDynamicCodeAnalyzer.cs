// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresDynamicCodeAnalyzer : RequiresAnalyzerBase
	{
		const string RequiresDynamicCodeAttribute = nameof (RequiresDynamicCodeAttribute);
		public const string FullyQualifiedRequiresDynamicCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresDynamicCodeAttribute;

		static readonly DiagnosticDescriptor s_requiresDynamicCodeOnStaticCtor = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresDynamicCodeOnStaticConstructor);
		static readonly DiagnosticDescriptor s_requiresDynamicCodeRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresDynamicCode);
		static readonly DiagnosticDescriptor s_requiresDynamicCodeAttributeMismatch = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresDynamicCodeAttributeMismatch);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create (s_requiresDynamicCodeRule, s_requiresDynamicCodeAttributeMismatch, s_requiresDynamicCodeOnStaticCtor);

		private protected override string RequiresAttributeName => RequiresDynamicCodeAttribute;

		internal override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresDynamicCodeAttribute;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Class;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresDynamicCodeRule;

		private protected override DiagnosticId RequiresDiagnosticId => DiagnosticId.RequiresDynamicCode;

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresDynamicCodeAttributeMismatch;

		private protected override DiagnosticDescriptor RequiresOnStaticCtor => s_requiresDynamicCodeOnStaticCtor;

		internal override bool IsAnalyzerEnabled (AnalyzerOptions options) =>
			options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableAotAnalyzer);

		internal override bool IsIntrinsicallyHandled (IMethodSymbol calledMethod, MultiValue instance, ImmutableArray<MultiValue> arguments) {
			MethodProxy method = new (calledMethod);
			var intrinsicId = Intrinsics.GetIntrinsicIdForMethod (method);

			switch (intrinsicId) {
			case IntrinsicId.Type_MakeGenericType: {
					if (!instance.IsEmpty ()) {
						foreach (var value in instance.AsEnumerable ()) {
							if (value is SystemTypeValue typeValue) {
								if (!IsKnownInstantiation (arguments[0])
									&& !IsConstrainedToBeReferenceTypes(typeValue.RepresentedType.GetGenericParameters())) {
									return false;
								}
							} else {
								return false;
							}
						}
					}
					return true;
				}
			case IntrinsicId.MethodInfo_MakeGenericMethod: {
					if (!instance.IsEmpty ()) {
						foreach (var methodValue in instance.AsEnumerable ()) {
							if (methodValue is SystemReflectionMethodBaseValue methodBaseValue) {
								if (!IsKnownInstantiation (arguments[0])
									&& !IsConstrainedToBeReferenceTypes(methodBaseValue.RepresentedMethod.GetGenericParameters())) {
									return false;
								}
							} else {
								return false;
							}
						}
					}
					return true;
				}
			}

			return false;

			static bool IsKnownInstantiation(MultiValue genericParametersArray) {
				var typesValue = genericParametersArray.AsSingleValue ();
				if (typesValue is NullValue) {
					// This will fail at runtime but no warning needed
					return true;
				}

				// Is this an array we model?
				if (typesValue is not ArrayValue array) {
					return false;
				}

				int? size = array.Size.AsConstInt ();
				if (size == null) {
					return false;
				}

				for (int i = 0; i < size.Value; i++) {
					// Go over each element of the array. If the value is unknown, bail.
					if (!array.TryGetValueByIndex (i, out MultiValue value)) {
						return false;
					}

					var singleValue = value.AsSingleValue ();

					if (singleValue is not SystemTypeValue and not GenericParameterValue and not NullableSystemTypeValue) {
						return false;
					}
				}

				return true;
			}

			static bool IsConstrainedToBeReferenceTypes(ImmutableArray<GenericParameterProxy> parameters)
			{
				foreach (GenericParameterProxy param in parameters)
					if (!param.TypeParameterSymbol.HasReferenceTypeConstraint)
						return false;
				return true;
			}
		}

		private protected override bool IsRequiresCheck (IPropertySymbol propertySymbol, Compilation compilation) {
			var runtimeFeaturesType = compilation.GetTypeByMetadataName ("System.Runtime.CompilerServices.RuntimeFeature");
			if (runtimeFeaturesType == null)
				return false;

			var isDynamicCodeSupportedProperty = runtimeFeaturesType.GetMembers ("IsDynamicCodeSupported").OfType<IPropertySymbol> ().FirstOrDefault ();
			if (isDynamicCodeSupportedProperty == null)
				return false;

			return SymbolEqualityComparer.Default.Equals (propertySymbol, isDynamicCodeSupportedProperty);
		}

		protected override bool VerifyAttributeArguments (AttributeData attribute) =>
			attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments is [ { Type.SpecialType: SpecialType.System_String }, ..];

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = (string) requiresAttribute!.ConstructorArguments[0].Value!;
			return MessageFormat.FormatRequiresAttributeMessageArg (message);
		}
	}
}
