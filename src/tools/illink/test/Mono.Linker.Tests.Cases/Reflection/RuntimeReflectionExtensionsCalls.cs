using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System.Reflection;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class RuntimeReflectionExtensionsCalls
	{
		public static void Main ()
		{
			TestGetRuntimeEvent ();
			TestGetRuntimeField ();
			TestGetRuntimeProperty ();

			TestGetRuntimeMethod ();
		}

		[Kept]
		public static void TestGetRuntimeEvent ()
		{
			typeof (Foo).GetRuntimeEvent ("Event");
		}

		[Kept]
		public static void TestGetRuntimeField ()
		{
			typeof (Foo).GetRuntimeField ("Field");
		}

		[Kept]
		public static void TestGetRuntimeProperty ()
		{
			typeof (Foo).GetRuntimeProperty ("Property");
		}

		[Kept]
		public static void TestGetRuntimeMethod ()
		{
			typeof (Foo).GetRuntimeMethod ("Method1", Type.EmptyTypes);
		}

		class Foo
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			event EventHandler<EventArgs> Event;

			[Kept]
			int Field;

			[Kept]
			[KeptBackingField]
			public long Property { [Kept] get; [Kept] set; }

			[Kept]
			public void Method1 (int someArg)
			{
			}
		}
	}
}
