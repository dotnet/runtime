using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class KeptTypeInAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public readonly string AssemblyFileName;
		public readonly string TypeName;

		public KeptTypeInAssemblyAttribute (string assemblyFileName, Type type)
		{
			AssemblyFileName = assemblyFileName;
			TypeName = type.ToString ();
		}

		public KeptTypeInAssemblyAttribute (string assemblyFileName, string typeName)
		{
			AssemblyFileName = assemblyFileName;
			TypeName = typeName;
		}
	}
}
