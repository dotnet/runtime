using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	public class InterfaceMethod
	{
		public static void Main ()
		{
			UsedToMarkMethod (null);
		}

		[Kept]
		static void UsedToMarkMethod (Foo f)
		{
			IFoo i = f;
			i.Method ();
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Method ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo
		{
			[Kept]
			[ExpectBodyModified]
			public void Method ()
			{
				UsedByMethod ();
			}

			void UsedByMethod ()
			{
			}
		}
	}
}