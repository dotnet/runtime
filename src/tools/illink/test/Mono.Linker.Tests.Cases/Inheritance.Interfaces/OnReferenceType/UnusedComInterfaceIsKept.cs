using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	/// <summary>
	/// It's much harder to know if a com interface will be needed since so much can be on the native side.
	/// As a precaution we will not apply the unused interface rules to com interfaces
	/// </summary>
	public class UnusedComInterfaceIsKept
	{
		public static void Main ()
		{
			var i = new A ();
			i.Foo ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (GuidAttribute))]
		[ComImport]
		[Guid ("D7BB1889-3AB7-4681-A115-60CA9158FECA")]
		interface IBar
		{
			// Trimming may remove members from COM interfaces
			// even when keeping the COM-related attributes.
			// https://github.com/dotnet/runtime/issues/101128
			void Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IBar))]
		class A : IBar
		{
			[Kept]
			public void Foo ()
			{
			}

			public void Bar ()
			{
			}
		}
	}
}