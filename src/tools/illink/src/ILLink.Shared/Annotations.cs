// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ILLink.Shared
{
	// Temporary workaround - should be removed once linker can be upgraded to build against
	// high enough version of the framework which has this enum value.
	internal static class DynamicallyAccessedMemberTypesOverlay
	{
		public const DynamicallyAccessedMemberTypes Interfaces = (DynamicallyAccessedMemberTypes) 0x2000;
	}

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
				string mtName = mt == DynamicallyAccessedMemberTypesOverlay.Interfaces
					? nameof (DynamicallyAccessedMemberTypesOverlay.Interfaces)
					: mt.ToString ();

				return $"'{nameof (DynamicallyAccessedMemberTypes)}.{mtName}'";
			}));
		}

		static readonly DynamicallyAccessedMemberTypes[] AllDynamicallyAccessedMemberTypes = GetAllDynamicallyAccessedMemberTypes ();

		static DynamicallyAccessedMemberTypes[] GetAllDynamicallyAccessedMemberTypes ()
		{
			var values = new HashSet<DynamicallyAccessedMemberTypes> (
								Enum.GetValues (typeof (DynamicallyAccessedMemberTypes))
								.Cast<DynamicallyAccessedMemberTypes> ());
			if (!values.Contains (DynamicallyAccessedMemberTypesOverlay.Interfaces))
				values.Add (DynamicallyAccessedMemberTypesOverlay.Interfaces);
			return values.ToArray ();
		}
	}
}
