using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	public sealed class KeptBaseTypeAttribute : KeptAttribute
	{
		public readonly Type BaseType;
		public readonly object [] GenericParameterNames;

		public KeptBaseTypeAttribute (Type baseType)
		{
			BaseType = baseType;
		}

		public KeptBaseTypeAttribute (Type baseType, params object[] typeArguments)
		{
			BaseType = baseType;
			GenericParameterNames = typeArguments;
		}
	}
}