using System;
namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class RemovedForwarderAttribute : BaseInAssemblyAttribute
	{
		public RemovedForwarderAttribute (string assemblyFileName, string typeName)
		{
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (assemblyFileName));
			if (string.IsNullOrEmpty (typeName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (typeName));
		}
	}
}
