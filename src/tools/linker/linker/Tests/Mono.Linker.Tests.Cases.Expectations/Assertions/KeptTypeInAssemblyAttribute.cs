using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class KeptTypeInAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public readonly string AssemblyFileName;
		public readonly Type Type;

		public KeptTypeInAssemblyAttribute(string assemblyFileName, Type type)
		{
			AssemblyFileName = assemblyFileName;
			Type = type;
		}
	}
}
