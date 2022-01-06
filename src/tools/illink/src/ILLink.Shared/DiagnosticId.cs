// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILLink.Shared
{
	public enum DiagnosticId
	{
		// Linker error ids.
		XmlFeatureDoesNotSpecifyFeatureValue = 1001,
		XmlUnsupportedNonBooleanValueForFeature = 1002,
		XmlException = 1003,
		CouldNotFindMethodInAssembly = 1005,
		CannotStubConstructorWhenBaseTypeDoesNotHaveConstructor = 1006,
		CouldNotFindType = 1007,
		CouldNotFindConstructor = 1008,
		CouldNotFindAssemblyReference = 1009,
		CouldNotLoadAssembly = 1010,
		FailedToWriteOutput = 1011,
		LinkerUnexpectedError = 1012,
		ErrorProcessingXmlLocation = 1013,
		XmlDocumentLocationHasInvalidFeatureDefault = 1014,
		UnrecognizedCommandLineOption = 1015,
		InvalidWarningVersion = 1016,
		InvalidGenerateWarningSuppressionsValue = 1017,
		MissingArgumentForCommanLineOptionName = 1018,
		CustomDataFormatIsInvalid = 1019,
		NoFilesToLinkSpecified = 1020,
		NewMvidAndDeterministicCannotBeUsedAtSameTime = 1021,
		AssemblyInCustomStepOptionCouldNotBeFound = 1022,
		AssemblyPathInCustomStepMustBeFullyQualified = 1023,
		InvalidArgForCustomStep = 1024,
		ExpectedSignToControlNewStepInsertion = 1025,
		PipelineStepCouldNotBeFound = 1026,
		CustomStepTypeCouldNotBeFound = 1027,
		CustomStepTypeIsIncompatibleWithLinkerVersion = 1028,
		InvalidOptimizationValue = 1029,
		InvalidArgumentForTokenOption = 1030,
		InvalidAssemblyAction = 1031,
		RootAssemblyCouldNotBeFound = 1032,
		XmlDescriptorCouldNotBeFound = 1033,
		RootAssemblyDoesNotHaveEntryPoint = 1034,
		RootAssemblyCannotUseAction = 1035,
		InvalidAssemblyName = 1036,
		InvalidAssemblyRootMode = 1037,
		ExportedTypeCannotBeResolved = 1038,
		ReferenceAssemblyCouldNotBeLoaded = 1039,
		FailedToResolveMetadataElement = 1040,
		TypeUsedWithAttributeValueCouldNotBeFound = 1041,
		CannotConverValueToType = 1042,
		CustomAttributeArgumentForTypeRequiresNestedNode = 1043,
		CouldNotResolveCustomAttributeTypeValue = 1044,
		UnexpectedAttributeArgumentType = 1045,
		InvalidMetadataOption = 1046,

		// Linker diagnostic ids.
		RequiresUnreferencedCode = 2026,
		RequiresUnreferencedCodeAttributeMismatch = 2046,
		CorrectnessOfCOMCannotBeGuaranteed = 2050,
		MakeGenericType = 2055,
		MakeGenericMethod = 2060,
		RequiresOnBaseClass = 2109,
		RequiresUnreferencedCodeOnStaticConstructor = 2116,

		// Single-file diagnostic ids.
		AvoidAssemblyLocationInSingleFile = 3000,
		AvoidAssemblyGetFilesInSingleFile = 3001,
		RequiresAssemblyFiles = 3002,
		RequiresAssemblyFilesAttributeMismatch = 3003,

		// Dynamic code diagnostic ids.
		RequiresDynamicCode = 3050,
		RequiresDynamicCodeAttributeMismatch = 3051
	}

	public static class DiagnosticIdExtensions
	{
		public static string AsString (this DiagnosticId diagnosticId) => $"IL{(int) diagnosticId}";
	}
}
