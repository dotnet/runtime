using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// <summary>
	/// Verifies that a module reference exists in the test case assembly
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class KeptExportedTypeAttribute : KeptAttribute
	{
		public KeptExportedTypeAttribute (Type type)
		{
			if (type is null)
				throw new ArgumentNullException (nameof (type));
		}
	}
}
