using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.OverrideRemoval
{
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]

	// The order is important - since the bug this uncovers depends on order of processing of assemblies in the sweep step
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/OverrideOfAbstractIsKeptNonEmptyLibrary.cs" })]
	[SetupCompileBefore (
		"librarywithnonempty.dll",
		new[] { "Dependencies/OverrideOfAbstractIsKeptNonEmptyLibraryWithNonEmpty.cs" },
		new[] { "library.dll" })]

	[KeptAssembly ("library.dll")]
	[KeptAssembly ("librarywithnonempty.dll")]

	[RemovedTypeInAssembly ("library.dll", typeof (Dependencies.OverrideOfAbstractIsKeptNonEmpty_UnusedType))]

	public class OverrideOfAbstractIsKeptNonEmpty
	{
		public static void Main ()
		{
			Base b = HelperToMarkFooAndRequireBase ();
			b.Method ();

			Dependencies.OverrideOfAbstractIsKeptNonEmpty_BaseType c = HelperToMarkLibraryAndRequireItsBase ();
			c.Method ();
		}

		[Kept]
		static Foo HelperToMarkFooAndRequireBase ()
		{
			return null;
		}

		[Kept]
		static Dependencies.OverrideOfAbstractIsKeptNonEmptyLibraryWithNonEmpty HelperToMarkLibraryAndRequireItsBase ()
		{
			return null;
		}

		[Kept]
		abstract class Base
		{
			[Kept]
			public abstract void Method ();
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		abstract class Base2 : Base
		{
		}

		[Kept]
		[KeptBaseType (typeof (Base2))]
		abstract class Base3 : Base2
		{
		}

		[Kept]
		[KeptBaseType (typeof (Base3))]
		class Foo : Base3
		{
			Dependencies.OverrideOfAbstractIsKeptNonEmpty_UnusedType _field;

			[Kept]
			[ExpectBodyModified]
			public override void Method ()
			{
				Other ();
				_field = null;
			}

			static void Other ()
			{
			}
		}
	}
}