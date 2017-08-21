using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupLinkerActionAttribute : BaseMetadataAttribute
	{
		public SetupLinkerActionAttribute (string action, string assembly)
		{
			switch (action) {
			case "link": case "copy": case "skip":
				break;
			default:
				throw new ArgumentOutOfRangeException (nameof (action));
			}
		}
	}
}
