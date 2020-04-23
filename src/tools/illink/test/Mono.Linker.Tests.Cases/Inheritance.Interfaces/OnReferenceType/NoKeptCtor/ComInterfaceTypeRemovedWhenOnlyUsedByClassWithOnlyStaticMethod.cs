using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor
{
	public class ComInterfaceTypeRemovedWhenOnlyUsedByClassWithOnlyStaticMethod
	{
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[ComImport]
		[Guid ("D7BB1889-3AB7-4681-A115-60CA9158FECA")]
		interface IUnusedInterface
		{
			void Foo ();
		}

		[Kept]
		class StaticMethodOnlyUsed : IUnusedInterface
		{
			public void Foo ()
			{
			}

			[Kept]
			public static void StaticMethod ()
			{
			}
		}
	}
}