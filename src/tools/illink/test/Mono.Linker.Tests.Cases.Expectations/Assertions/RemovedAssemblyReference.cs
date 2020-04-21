using System;
namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class RemovedAssemblyReferenceAttribute : BaseInAssemblyAttribute
	{
		public RemovedAssemblyReferenceAttribute (string assemblyFileName, string assemblyReferenceName)
		{
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (assemblyFileName));
			if (string.IsNullOrEmpty (assemblyReferenceName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (assemblyReferenceName));
		}
	}
}
