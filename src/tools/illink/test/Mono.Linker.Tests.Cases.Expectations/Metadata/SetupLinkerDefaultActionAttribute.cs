using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public class SetupLinkerDefaultActionAttribute : BaseMetadataAttribute
	{
		public SetupLinkerDefaultActionAttribute (string action)
		{
			if (string.IsNullOrEmpty (action))
				throw new ArgumentNullException (nameof (action));
		}
	}
}