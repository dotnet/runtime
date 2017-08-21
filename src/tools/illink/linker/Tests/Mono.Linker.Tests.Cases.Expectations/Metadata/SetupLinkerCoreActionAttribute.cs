using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false)]
	public class SetupLinkerCoreActionAttribute : BaseMetadataAttribute
	{
		public SetupLinkerCoreActionAttribute (string action)
		{
			switch (action) {
			case "link":
			case "copy":
			case "skip":
				break;
			default:
				throw new ArgumentOutOfRangeException (nameof (action));
			}
		}
	}
}
