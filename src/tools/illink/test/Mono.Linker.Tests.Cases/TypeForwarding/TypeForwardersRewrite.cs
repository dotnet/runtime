using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - This assembly, Forwarder.dll and Implementation.dll

	[SetupLinkerArgument ("--skip-unresolved", "true")]

	[SetupCompileArgument ("/unsafe")]
	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/TypeForwardersRewriteLib.cs" })]

	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/TypeForwardersRewriteLib.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/TypeForwardersRewriteForwarders.cs" }, references: new[] { "Implementation.dll" })]

	[RemovedAssembly ("Forwarder.dll")]
	[RemovedAssemblyReference ("test", "Forwarder")]
	unsafe class TypeForwardersRewrite
	{
		static void Main ()
		{
#if NETCOREAPP
			Test (null);
#endif
			Test2 (null);
			Test3<C> (ref c);
			Test4 (null, null);
			Test5 (null);
			Test6<string> (null);
			c = null;
			g = null;
			e += null;
			I tc = new TC ();
			tc.Test (null);
			I ti = new TS ();
			ti.Test (null);
			var gc = new GC<TC> ();
		}

		[Kept]
		static C c;
		[Kept]
		static G<C> g;

		[Kept]
		[KeptBackingField]
		[KeptEventAddMethod]
		[KeptEventRemoveMethod]
		static event D e;

		[Kept]
		[KeptBaseType (typeof (MulticastDelegate))]
		[KeptMember (".ctor(System.Object,System.IntPtr)")]
		[KeptMember ("Invoke()")]
		delegate C D ();

#if NETCOREAPP
		[Kept]
		static void Test (delegate*<C, S*> arg)
		{
		}
#endif

		[Kept]
		static C Test2 (C c)
		{
			C lv = null;
			return lv;
		}

		[Kept]
		static C[] Test3<T> (ref C c) where T : C
		{
			return null;
		}

		[Kept]
		static G<C> Test4 (G<C>[] a, object b)
		{
			Console.WriteLine (typeof (C));
			Console.WriteLine (typeof (G<>));
			Console.WriteLine (typeof (G<>.N));
			C c = (C) b;
			Console.WriteLine (c);
			return null;
		}

		[Kept]
		static G<C>.N Test5 (G<C>.N arg)
		{
			return null;
		}

		[Kept]
		static void Test6<T> (G<T>.N arg)
		{
		}

		[Kept]
		[KeptBaseType (typeof (C))]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (I))]
		class TC : C, I
		{
			[Kept]
			void I.Test (C c)
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (I))]
		struct TS : I
		{
			[Kept]
			public void Test (C c)
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		class GC<T> where T : I
		{
		}
	}
}
