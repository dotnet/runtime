using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupLinkerActionAttribute : BaseMetadataAttribute
	{
		public SetupLinkerActionAttribute (string action, string assembly)
		{
			if (string.IsNullOrEmpty (action))
				throw new ArgumentNullException (nameof (action));
		}
	}
}
