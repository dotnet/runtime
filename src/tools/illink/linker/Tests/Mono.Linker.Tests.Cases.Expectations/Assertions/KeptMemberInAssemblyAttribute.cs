using System;


namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class KeptMemberInAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute {

		public KeptMemberInAssemblyAttribute (string assemblyFileName, Type type, params string [] memberNames)
		{
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentNullException (nameof (assemblyFileName));
			if (type == null)
				throw new ArgumentNullException (nameof (type));
			if (memberNames == null)
				throw new ArgumentNullException (nameof (memberNames));
		}

		public KeptMemberInAssemblyAttribute (string assemblyFileName, string typeName, params string [] memberNames)
		{
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentNullException (nameof (assemblyFileName));
			if (typeName == null)
				throw new ArgumentNullException (nameof (typeName));
			if (memberNames == null)
				throw new ArgumentNullException (nameof (memberNames));
		}
	}
}
