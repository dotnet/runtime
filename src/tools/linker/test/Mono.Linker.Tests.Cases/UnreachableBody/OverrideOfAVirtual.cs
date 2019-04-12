using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.UnreachableBody {
	public class OverrideOfAVirtual {
		public static void Main()
		{
			UsedToMarkMethod (null);
		}

		[Kept]
		static void UsedToMarkMethod (Foo f)
		{
			f.Method ();
		}

		[Kept]
		class Base {
			[Kept]
			[ExpectBodyModified]
			public virtual void Method ()
			{
				UsedByMethod();
			}
			
			void UsedByMethod ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Foo : Base {
			// A callvirt to Base.Method() appears in the IL so this override can be removed entirely
			public override void Method ()
			{
				UsedByMethod ();
			}

			void UsedByMethod ()
			{
			}
		}
	}
}