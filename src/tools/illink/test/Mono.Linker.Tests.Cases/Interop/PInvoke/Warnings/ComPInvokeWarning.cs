using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Warnings
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[KeptModuleReference ("Foo")]
	class ComPInvokeWarning
	{
		[UnconditionalSuppressMessage ("trim", "IL2026")]
		static void Main ()
		{
			SomeMethodTakingInterface (null);
			SomeMethodTakingObject (null);
			GetInterface ();
			CanSuppressWarningOnParameter (null);
			CanSuppressWarningOnReturnType ();
			CanSuppressWithRequiresUnreferencedCode (null);
		}

		[ExpectedWarning ("IL2050")]
		[DllImport ("Foo")]
		static extern void SomeMethodTakingInterface (IFoo foo);

		[ExpectedWarning ("IL2050")]
		[DllImport ("Foo")]
		static extern void SomeMethodTakingObject ([MarshalAs (UnmanagedType.IUnknown)] object obj);

		[ExpectedWarning ("IL2050")]
		[DllImport ("Foo")]
		static extern IFoo GetInterface ();

		[UnconditionalSuppressMessage ("trim", "IL2050")]
		[DllImport ("Foo")]
		static extern void CanSuppressWarningOnParameter ([MarshalAs (UnmanagedType.IUnknown)] object obj);

		[UnconditionalSuppressMessage ("trim", "IL2050")]
		[DllImport ("Foo")]
		static extern IFoo CanSuppressWarningOnReturnType ();

		[ExpectedWarning ("IL2050")] // Issue https://github.com/mono/linker/issues/1989
		[RequiresUnreferencedCode ("test")]
		[DllImport ("Foo")]
		static extern void CanSuppressWithRequiresUnreferencedCode (IFoo foo);

		interface IFoo { }
	}
}
