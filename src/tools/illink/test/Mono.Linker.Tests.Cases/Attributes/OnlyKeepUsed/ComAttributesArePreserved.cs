using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	/// <summary>
	/// COM related attributes are required at runtime
	/// </summary>
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	public class ComAttributesArePreserved
	{
		public static void Main ()
		{
			var tmp = ReturnValueUsedToMarkType ();
		}

		[Kept]
		static A ReturnValueUsedToMarkType ()
		{
			return null;
		}

		[Kept]
		[KeptAttributeAttribute (typeof (GuidAttribute))]
		[KeptAttributeAttribute (typeof (InterfaceTypeAttribute))]
		[ComImport]
		[Guid ("D7BB1889-3AB7-4681-A115-60CA9158FECA")]
		[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
		interface A
		{
		}
	}
}