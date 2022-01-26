// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared.TypeSystemProxy;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct RequireDynamicallyAccessedMembersAction
	{
		public void Invoke (in DiagnosticContext diagnosticContext, in MultiValue value, ValueWithDynamicallyAccessedMembers targetValue)
		{
			foreach (var uniqueValue in value) {
				if (targetValue.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
					&& uniqueValue is GenericParameterValue genericParam
					&& genericParam.HasDefaultConstructorConstraint ()) {
					// We allow a new() constraint on a generic parameter to satisfy DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
				} else if (uniqueValue is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers) {
					var availableMemberTypes = valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes;
					if (!Annotations.SourceHasRequiredAnnotations (availableMemberTypes, targetValue.DynamicallyAccessedMemberTypes, out var missingMemberTypes)) {
						(var diagnosticId, var diagnosticArguments) = Annotations.GetDiagnosticForAnnotationMismatch (valueWithDynamicallyAccessedMembers, targetValue, missingMemberTypes);
						diagnosticContext.ReportDiagnostic (diagnosticId, diagnosticArguments);
					}
				} else if (uniqueValue is SystemTypeValue systemTypeValue) {
					MarkTypeForDynamicallyAccessedMembers (systemTypeValue.GetRepresentedType (), targetValue.DynamicallyAccessedMemberTypes);
				} else if (uniqueValue is KnownStringValue knownStringValue) {
					if (!TryResolveTypeNameAndMark (knownStringValue.Contents, out TypeProxy foundType)) {
						// Intentionally ignore - it's not wrong for code to call Type.GetType on non-existing name, the code might expect null/exception back.
					} else {
						MarkTypeForDynamicallyAccessedMembers (foundType, targetValue.DynamicallyAccessedMemberTypes);
					}
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

					diagnosticContext.ReportDiagnostic (diagnosticId, targetValue.GetDiagnosticArgumentsForAnnotationMismatch ().ToArray ());
				}
			}
		}

		private partial bool TryResolveTypeNameAndMark (string typeName, out TypeProxy type);

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);
	}
}
