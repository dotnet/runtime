using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	/// <summary>
	/// With --keep-com-interfaces false, we apply the unused interface  rules also to com interfaces.
	/// </summary>
	[SetupLinkerArgument ("--keep-com-interfaces", "false")]
	public class UnusedComInterfaceIsRemoved
	{
		public static void Main ()
		{
			var i = new A ();
			i.Foo ();
		}

		[ComImport]
		[Guid ("D7BB1889-3AB7-4681-A115-60CA9158FECA")]
		interface IBar
		{
			void Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
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
