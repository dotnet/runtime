using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class RecognizedReflectionAccessPatternAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public RecognizedReflectionAccessPatternAttribute (Type reflectionMethodType, string reflectionMethodName, Type[] reflectionMethodParameters,
			Type accessedItemType, string accessedItemName, Type[] accessedItemParameters)
		{
			if (reflectionMethodType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodType));
			if (reflectionMethodName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodName));
			if (reflectionMethodParameters == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodParameters));

			if (accessedItemType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (accessedItemType));
			if (accessedItemName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (accessedItemName));
		}

		public RecognizedReflectionAccessPatternAttribute (Type reflectionMethodType, string reflectionMethodName, Type [] reflectionMethodParameters,
			Type accessedItemType, string accessedItemName, string [] accessedItemParameters)
		{
			if (reflectionMethodType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodType));
			if (reflectionMethodName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodName));
			if (reflectionMethodParameters == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodParameters));

			if (accessedItemType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (accessedItemType));
			if (accessedItemName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (accessedItemName));
		}
	}
}
