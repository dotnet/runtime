using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	public sealed class KeptBaseTypeAttribute : KeptAttribute
	{
		public KeptBaseTypeAttribute (Type baseType)
		{
			if (baseType == null)
				throw new ArgumentNullException (nameof (baseType));
		}

		public KeptBaseTypeAttribute (Type baseType, params object[] typeArguments)
		{
			if (baseType == null)
				throw new ArgumentNullException (nameof (baseType));
			if (typeArguments == null)
				throw new ArgumentNullException (nameof (typeArguments));
		}
	}
}