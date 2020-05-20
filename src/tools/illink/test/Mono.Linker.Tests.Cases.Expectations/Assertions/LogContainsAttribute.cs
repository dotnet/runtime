using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (
		AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field,
		AllowMultiple = true,
		Inherited = false)]
	public class LogContainsAttribute : EnableLoggerAttribute
	{
		public LogContainsAttribute (string message, bool regexMatch = false)
		{
			if (string.IsNullOrEmpty (message))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (message));
		}
	}
}