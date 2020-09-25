using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class KeptInitializerData : KeptAttribute
	{

		public KeptInitializerData ()
		{
		}

		public KeptInitializerData (int occurrenceIndexInBody)
		{
			if (occurrenceIndexInBody < 0)
				throw new ArgumentOutOfRangeException (nameof (occurrenceIndexInBody));
		}
	}
}