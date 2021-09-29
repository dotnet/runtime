// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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

			var missingMemberTypesList = Enum.GetValues (typeof (DynamicallyAccessedMemberTypes))
				.Cast<DynamicallyAccessedMemberTypes> ()
				.Where (damt => (damt & targetMemberTypes & ~sourceMemberTypes) == damt && damt != DynamicallyAccessedMemberTypes.None)
				.ToList ();

			if (targetMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors) &&
				sourceMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor) &&
				!sourceMemberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors))
				missingMemberTypesList.Add (DynamicallyAccessedMemberTypes.PublicConstructors);

			if (missingMemberTypesList.Contains (DynamicallyAccessedMemberTypes.PublicConstructors) &&
				missingMemberTypesList.Contains (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor))
				missingMemberTypesList.Remove (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor);

			if (missingMemberTypesList.Count == 0)
				return true;

			missingMemberTypesString = targetMemberTypes == DynamicallyAccessedMemberTypes.All
				? $"'{nameof (DynamicallyAccessedMemberTypes)}.{nameof (DynamicallyAccessedMemberTypes.All)}'"
				: string.Join (", ", missingMemberTypesList.Select (mmt => $"'{nameof (DynamicallyAccessedMemberTypes)}.{mmt}'"));

			return false;
		}
	}
}
