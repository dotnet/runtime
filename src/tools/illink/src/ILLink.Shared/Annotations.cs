// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared.TrimAnalysis;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared
{
	internal static class Annotations
	{
		public static bool SourceHasRequiredAnnotations (
			DynamicallyAccessedMemberTypes sourceMemberTypes,
			DynamicallyAccessedMemberTypes targetMemberTypes,
			out string missingMemberTypesString)
		{
			missingMemberTypesString = string.Empty;

			var missingMemberTypes = GetMissingMemberTypes (targetMemberTypes, sourceMemberTypes);
			if (missingMemberTypes == DynamicallyAccessedMemberTypes.None)
				return true;

			missingMemberTypesString = GetMemberTypesString (missingMemberTypes);
			return false;
		}

		public static DynamicallyAccessedMemberTypes GetMissingMemberTypes (DynamicallyAccessedMemberTypes requiredMemberTypes, DynamicallyAccessedMemberTypes availableMemberTypes)
		{
			if (availableMemberTypes.HasFlag (requiredMemberTypes))
				return DynamicallyAccessedMemberTypes.None;

			if (requiredMemberTypes == DynamicallyAccessedMemberTypes.All)
				return DynamicallyAccessedMemberTypes.All;

			var missingMemberTypes = requiredMemberTypes & ~availableMemberTypes;

			// PublicConstructors is a special case since its value is 3 - so PublicParameterlessConstructor (1) | _PublicConstructor_WithMoreThanOneParameter_ (2)
			// The above bit logic only works for value with single bit set.
			if (requiredMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors) &&
				!availableMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors))
				missingMemberTypes |= DynamicallyAccessedMemberTypes.PublicConstructors;

			return missingMemberTypes;
		}

		public static string GetMemberTypesString (DynamicallyAccessedMemberTypes memberTypes)
		{
			Debug.Assert (memberTypes != DynamicallyAccessedMemberTypes.None);

			if (memberTypes == DynamicallyAccessedMemberTypes.All)
				return $"'{nameof (DynamicallyAccessedMemberTypes)}.{nameof (DynamicallyAccessedMemberTypes.All)}'";

			var memberTypesList = AllDynamicallyAccessedMemberTypes
				.Where (damt => (memberTypes & damt) == damt && damt != DynamicallyAccessedMemberTypes.None)
				.ToList ();

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors))
				memberTypesList.Remove (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor);

			return string.Join (", ", memberTypesList.Select (mt => {
				string mtName = mt == DynamicallyAccessedMemberTypes.Interfaces
					? nameof (DynamicallyAccessedMemberTypes.Interfaces)
					: mt.ToString ();

				return $"'{nameof (DynamicallyAccessedMemberTypes)}.{mtName}'";
			}));
		}

		private static readonly DynamicallyAccessedMemberTypes[] AllDynamicallyAccessedMemberTypes = GetAllDynamicallyAccessedMemberTypes ();

		private static DynamicallyAccessedMemberTypes[] GetAllDynamicallyAccessedMemberTypes ()
		{
			var values = new HashSet<DynamicallyAccessedMemberTypes> (
								Enum.GetValues (typeof (DynamicallyAccessedMemberTypes))
								.Cast<DynamicallyAccessedMemberTypes> ());
			values.Add (DynamicallyAccessedMemberTypes.Interfaces);
			return values.ToArray ();
		}

		public static (DiagnosticId Id, string[] Arguments) GetDiagnosticForAnnotationMismatch (ValueWithDynamicallyAccessedMembers source, ValueWithDynamicallyAccessedMembers target, string missingAnnotations)
		{
			DiagnosticId diagnosticId = (source, target) switch {
				(MethodParameterValue maybeThisSource, MethodParameterValue maybeThisTarget) when maybeThisSource.IsThisParameter () && maybeThisTarget.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter,
				(MethodParameterValue maybeThis, MethodParameterValue) when maybeThis.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter,
				(MethodParameterValue maybeThis, MethodReturnValue) when maybeThis.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType,
				(MethodParameterValue maybeThis, FieldValue) when maybeThis.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField,
				(MethodParameterValue maybeThis, GenericParameterValue) when maybeThis.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsGenericParameter,
				(MethodParameterValue, MethodParameterValue maybeThis) when maybeThis.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter,
				(MethodParameterValue, MethodParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter,
				(MethodParameterValue, MethodReturnValue) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType,
				(MethodParameterValue, FieldValue) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField,
				(MethodParameterValue, GenericParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsGenericParameter,
				(MethodReturnValue, MethodParameterValue maybeThis) when maybeThis.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter,
				(MethodReturnValue, MethodParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter,
				(MethodReturnValue, MethodReturnValue) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType,
				(MethodReturnValue, FieldValue) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField,
				(MethodReturnValue, GenericParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsGenericParameter,
				(FieldValue, MethodParameterValue maybeThis) when maybeThis.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter,
				(FieldValue, MethodParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter,
				(FieldValue, MethodReturnValue) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType,
				(FieldValue, FieldValue) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField,
				(FieldValue, GenericParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter,
				(GenericParameterValue, MethodParameterValue maybeThis) when maybeThis.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter,
				(GenericParameterValue, MethodParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter,
				(GenericParameterValue, MethodReturnValue) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType,
				(GenericParameterValue, FieldValue) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField,
				(GenericParameterValue, GenericParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter,
				(NullableValueWithDynamicallyAccessedMembers, MethodParameterValue maybeThis) when maybeThis.IsThisParameter () => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter,
				(NullableValueWithDynamicallyAccessedMembers, MethodParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter,
				(NullableValueWithDynamicallyAccessedMembers, MethodReturnValue) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType,
				(NullableValueWithDynamicallyAccessedMembers, FieldValue) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField,
				(NullableValueWithDynamicallyAccessedMembers, GenericParameterValue) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter,

				_ => throw new NotImplementedException ($"Unsupported source context {source} or target context {target}.")
			};

			var args = new List<string> ();
			args.AddRange (target.GetDiagnosticArgumentsForAnnotationMismatch ());
			args.AddRange (source.GetDiagnosticArgumentsForAnnotationMismatch ());
			args.Add (missingAnnotations);
			return (diagnosticId, args.ToArray ());
		}
	}
}
