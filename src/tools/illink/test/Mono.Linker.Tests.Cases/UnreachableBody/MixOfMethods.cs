using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	public class MixOfMethods
	{
		public static void Main ()
		{
			UseInstanceMethods (null);
			Foo.PublicStatic ();
			Base2.Base2PublicStatic ();
			Base.BasePublicStatic ();
		}

		[Kept]
		static void UseInstanceMethods (Foo f)
		{
			f.Method1 ();
			f.Method2 ();
			f.Method3 ();

			f.BaseMethod1 ();
			f.BaseMethod2 ();
			f.BaseMethod3 ();

			f.Base2Method1 ();
			f.Base2Method2 ();
			f.Base2Method3 ();
		}

		[Kept]
		class Base
		{
			[Kept]
			[ExpectBodyModified]
			public void BaseMethod1 ()
			{
				UsedByInstance ();
			}

			[Kept]
			[ExpectBodyModified]
			public void BaseMethod2 ()
			{
				UsedByInstance ();
			}

			[Kept]
			[ExpectBodyModified]
			public void BaseMethod3 ()
			{
				UsedByInstance ();
			}

			void UsedByInstance ()
			{
			}

			[Kept]
			public static void BasePublicStatic ()
			{
				UsedByStatic ();
			}

			[Kept]
			static void UsedByStatic ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Base2 : Base
		{
			[Kept]
			[ExpectBodyModified]
			public void Base2Method1 ()
			{
				UsedByInstance ();
			}

			[Kept]
			[ExpectBodyModified]
			public void Base2Method2 ()
			{
				UsedByInstance ();
			}

			[Kept]
			[ExpectBodyModified]
			public void Base2Method3 ()
			{
				UsedByInstance ();
			}

			void UsedByInstance ()
			{
			}

			[Kept]
			public static void Base2PublicStatic ()
			{
				UsedByStatic ();
			}

			[Kept]
			static void UsedByStatic ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base2))]
		class Foo : Base2
		{
			[Kept]
			[ExpectBodyModified]
			public void Method1 ()
			{
				UsedByInstance ();
			}

			[Kept]
			[ExpectBodyModified]
			public void Method2 ()
			{
				UsedByInstance ();
			}

			[Kept]
			[ExpectBodyModified]
			public void Method3 ()
			{
				UsedByInstance ();
			}

			void UsedByInstance ()
			{
			}

			[Kept]
			public static void PublicStatic ()
			{
				UsedByStatic ();
			}

			[Kept]
			static void UsedByStatic ()
			{
			}
		}
	}
}