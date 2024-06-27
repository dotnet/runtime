using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Metadata
{
	[VerifyMetadataNames]
	public class DebuggerDisplayNamesAreKept
	{
		public static void Main ()
		{
			var t = new TypeWithDebuggerDisplayAttribute ();
			var u = new TypeWithDebuggerTypeProxyAttribute ();
		}

		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("{MemberNotFound}")]
		class TypeWithDebuggerDisplayAttribute
		{
			[Kept]
			public void MethodWithKeptParameterName (int arg)
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerTypeProxyAttribute))]
		[DebuggerTypeProxy (typeof (DebuggerTypeProxy))]
		class TypeWithDebuggerTypeProxyAttribute
		{
		}

		[KeptMember (".ctor()")]
		class DebuggerTypeProxy
		{
			[Kept]
			public void MethodWithKeptParameterName (int arg)
			{
			}
		}
	}
}
