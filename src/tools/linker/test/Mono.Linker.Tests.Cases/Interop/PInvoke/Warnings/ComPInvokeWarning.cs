using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Warnings
{
	[LogContains ("SomeMethodTakingInterface")]
	[LogContains ("SomeMethodTakingObject")]
	[KeptModuleReference ("Foo")]
	class ComPInvokeWarning
	{
		static void Main ()
		{
			SomeMethodTakingInterface (null);
			SomeMethodTakingObject (null);
		}

		[Kept]
		[DllImport ("Foo")]
		static extern void SomeMethodTakingInterface (IFoo foo);

		[Kept]
		[DllImport ("Foo")]
		static extern void SomeMethodTakingObject ([MarshalAs (UnmanagedType.IUnknown)] object obj);

		[Kept]
		interface IFoo { }
	}
}
