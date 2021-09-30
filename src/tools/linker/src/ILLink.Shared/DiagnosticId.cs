// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ILLink.Shared
{
	public enum DiagnosticId
	{
		// Linker diagnostic ids.
		RequiresUnreferencedCode = 2026,
		RequiresUnreferencedCodeAttributeMismatch = 2046,
		MakeGenericType = 2055,
		MakeGenericMethod = 2060,
		RequiresUnreferencedCodeOnStaticConstructor = 2116,

		// Dynamically Accessed Members attribute mismatch.
		DynamicallyAccessedMembersMismatchParameterTargetsParameter = 2067,
		DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType = 2068,
		DynamicallyAccessedMembersMismatchParameterTargetsField = 2069,
		DynamicallyAccessedMembersMismatchParameterTargetsThisParameter = 2070,
		DynamicallyAccessedMembersMismatchParameterTargetsGenericParameter = 2071,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter = 2072,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType = 2073,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField = 2074,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter = 2075,
		DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsGenericParameter = 2076,
		DynamicallyAccessedMembersMismatchFieldTargetsParameter = 2077,
		DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType = 2078,
		DynamicallyAccessedMembersMismatchFieldTargetsField = 2079,
		DynamicallyAccessedMembersMismatchFieldTargetsThisParameter = 2080,
		DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter = 2081,
		DynamicallyAccessedMembersMismatchThisParameterTargetsParameter = 2082,
		DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType = 2083,
		DynamicallyAccessedMembersMismatchThisParameterTargetsField = 2084,
		DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter = 2085,
		DynamicallyAccessedMembersMismatchThisParameterTargetsGenericParameter = 2086,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter = 2087,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType = 2088,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsField = 2089,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter = 2090,
		DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter = 2091,

		// Single-file diagnostic ids.
		AvoidAssemblyLocationInSingleFile = 3000,
		AvoidAssemblyGetFilesInSingleFile = 3001,
		RequiresAssemblyFiles = 3002,
		RequiresAssembyFilesAttributeMismatch = 3003
	}

	public static class DiagnosticIdExtensions
	{
		public static string AsString (this DiagnosticId diagnosticId) => $"IL{(int) diagnosticId}";
	}
}
