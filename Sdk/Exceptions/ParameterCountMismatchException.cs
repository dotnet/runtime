#if XUNIT_NULLABLE
#nullable enable
#endif

using System;

namespace Xunit.Sdk
{
	/// <summary>
	/// Exception to be thrown from theory execution when the number of
	/// parameter values does not the test method signature.
	/// </summary>
#if XUNIT_VISIBILITY_INTERNAL
	internal
#else
	public
#endif
	class ParameterCountMismatchException : Exception
	{ }
}
