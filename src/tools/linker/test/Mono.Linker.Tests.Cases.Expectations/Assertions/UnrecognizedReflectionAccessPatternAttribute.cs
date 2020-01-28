using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class UnrecognizedReflectionAccessPatternAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public UnrecognizedReflectionAccessPatternAttribute (Type reflectionMethodType, string reflectionMethodName, Type [] reflectionMethodParameters,
			string message = null)
		{
			if (reflectionMethodType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodType));
			if (reflectionMethodName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodName));
			if (reflectionMethodParameters == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodParameters));

			if (message == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (message));
		}

		public UnrecognizedReflectionAccessPatternAttribute (Type reflectionMethodType, string reflectionMethodName, string [] reflectionMethodParameters,
		string message = null)
		{
			if (reflectionMethodType == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodType));
			if (reflectionMethodName == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodName));
			if (reflectionMethodParameters == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethodParameters));

			if (message == null)
				throw new ArgumentException ("Value cannot be null or empty.", nameof (message));
		}
	}
}
