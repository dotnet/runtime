// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using ILLink.Shared.TypeSystemProxy;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	[StructLayout (LayoutKind.Auto)]
	partial struct RequireDynamicallyAccessedMembersAction
	{
		readonly DiagnosticContext _diagnosticContext;

		public void Invoke (in MultiValue value, ValueWithDynamicallyAccessedMembers targetValue)
		{
			if (targetValue.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.None)
				return;

			foreach (var uniqueValue in value) {
				if (targetValue.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
					&& uniqueValue is GenericParameterValue genericParam
					&& genericParam.GenericParameter.HasDefaultConstructorConstraint ()) {
					// We allow a new() constraint on a generic parameter to satisfy DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
				} else if (uniqueValue is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers) {
					if (uniqueValue is NullableValueWithDynamicallyAccessedMembers nullableValue) {
						MarkTypeForDynamicallyAccessedMembers (nullableValue.NullableType, nullableValue.DynamicallyAccessedMemberTypes);
					}
					var availableMemberTypes = valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes;
					if (!Annotations.SourceHasRequiredAnnotations (availableMemberTypes, targetValue.DynamicallyAccessedMemberTypes, out var missingMemberTypes)) {
						(var diagnosticId, var diagnosticArguments) = Annotations.GetDiagnosticForAnnotationMismatch (valueWithDynamicallyAccessedMembers, targetValue, missingMemberTypes);
						_diagnosticContext.AddDiagnostic (diagnosticId, diagnosticArguments);
					}
				} else if (uniqueValue is SystemTypeValue systemTypeValue) {
					MarkTypeForDynamicallyAccessedMembers (systemTypeValue.RepresentedType, targetValue.DynamicallyAccessedMemberTypes);
				} else if (uniqueValue is KnownStringValue knownStringValue) {
					if (!TryResolveTypeNameAndMark (knownStringValue.Contents, true, out TypeProxy foundType)) {
						// Intentionally ignore - it's not wrong for code to call Type.GetType on non-existing name, the code might expect null/exception back.
					} else {
						MarkTypeForDynamicallyAccessedMembers (foundType, targetValue.DynamicallyAccessedMemberTypes);
					}
				} else if (uniqueValue is NullableSystemTypeValue nullableSystemTypeValue) {
					MarkTypeForDynamicallyAccessedMembers (nullableSystemTypeValue.NullableType, targetValue.DynamicallyAccessedMemberTypes);
					MarkTypeForDynamicallyAccessedMembers (nullableSystemTypeValue.UnderlyingTypeValue.RepresentedType, targetValue.DynamicallyAccessedMemberTypes);
				} else if (uniqueValue == NullValue.Instance) {
					// Ignore - probably unreachable path as it would fail at runtime anyway.
				} else {
					DiagnosticId diagnosticId = targetValue switch {
						MethodParameterValue => DiagnosticId.MethodParameterCannotBeStaticallyDetermined,
						MethodReturnValue => DiagnosticId.MethodReturnValueCannotBeStaticallyDetermined,
						FieldValue => DiagnosticId.FieldValueCannotBeStaticallyDetermined,
						MethodThisParameterValue => DiagnosticId.ImplicitThisCannotBeStaticallyDetermined,
						GenericParameterValue => DiagnosticId.TypePassedToGenericParameterCannotBeStaticallyDetermined,
						_ => throw new NotImplementedException ($"unsupported target value {targetValue}")
					};

					_diagnosticContext.AddDiagnostic (diagnosticId, targetValue.GetDiagnosticArgumentsForAnnotationMismatch ().ToArray ());
				}
			}
		}

		public partial bool TryResolveTypeNameAndMark (string typeName, bool needsAssemblyName, out TypeProxy type);

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);
	}
}
