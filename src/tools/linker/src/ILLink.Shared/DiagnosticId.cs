namespace ILLink.Shared
{
	public enum DiagnosticId
	{
		// Linker diagnostic ids.
		RequiresUnreferencedCode = 2026,
		RequiresUnreferencedCodeAttributeMismatch = 2046,
		CorrectnessOfCOMCannotBeGuaranteed = 2050,
		MakeGenericType = 2055,
		MakeGenericMethod = 2060,
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
