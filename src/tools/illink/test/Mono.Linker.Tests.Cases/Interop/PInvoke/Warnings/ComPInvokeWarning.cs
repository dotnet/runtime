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
			Call_SomeMethodReturningAutoLayoutClass ();
			Call_SomeMethodTakingInterface ();
			Call_SomeMethodTakingObject ();
			Call_SomeMethodTakingArray ();
			Call_SomeMethodTakingString ();
			Call_SomeMethodTakingStringBuilder ();
			Call_SomeMethodTakingCriticalHandle ();
			Call_SomeMethodTakingSafeHandle ();
			Call_SomeMethodTakingExplicitLayoutClass ();
			Call_SomeMethodTakingSequentialLayoutClass ();
			Call_SomeMethodTakingAutoLayoutClass ();
			Call_GetInterface ();
			Call_CanSuppressWarningOnParameter ();
			Call_CanSuppressWarningOnReturnType ();
			Call_CanSuppressWithRequiresUnreferencedCode ();
			Call_CanSuppressPInvokeWithRequiresUnreferencedCode ();
			Call_PInvokeWithRequiresUnreferencedCode ();
		}

		[ExpectedWarning ("IL2050")]
		static void Call_SomeMethodTakingInterface ()
		{
			SomeMethodTakingInterface (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingInterface (IFoo foo);

		[ExpectedWarning ("IL2050")]
		static void Call_SomeMethodTakingObject ()
		{
			SomeMethodTakingObject (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingObject ([MarshalAs (UnmanagedType.IUnknown)] object obj);

		[ExpectedWarning ("IL2050")]
		static void Call_SomeMethodTakingArray ()
		{
			SomeMethodTakingArray (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingArray (Array array);

		static void Call_SomeMethodTakingStringBuilder ()
		{
			SomeMethodTakingStringBuilder (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingStringBuilder (StringBuilder str);

		static void Call_SomeMethodTakingCriticalHandle ()
		{
			SomeMethodTakingCriticalHandle (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingCriticalHandle (MyCriticalHandle handle);


		static void Call_SomeMethodTakingSafeHandle ()
		{
			SomeMethodTakingSafeHandle (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingSafeHandle (TestSafeHandle handle);

		static void Call_SomeMethodTakingExplicitLayoutClass ()
		{
			SomeMethodTakingExplicitLayout (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingExplicitLayout (ExplicitLayout _class);

		static void Call_SomeMethodTakingSequentialLayoutClass ()
		{
			SomeMethodTakingSequentialLayout (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingSequentialLayout (SequentialLayout _class);

		[ExpectedWarning ("IL2050")]
		static void Call_SomeMethodTakingAutoLayoutClass ()
		{
			SomeMethodTakingAutoLayout (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingAutoLayout (AutoLayout _class);

		[ExpectedWarning ("IL2050")]
		static void Call_SomeMethodReturningAutoLayoutClass ()
		{
			SomeMethodReturningAutoLayout ();
		}

		[DllImport ("Foo")]
		static extern AutoLayout SomeMethodReturningAutoLayout ();

		static void Call_SomeMethodTakingString ()
		{
			SomeMethodTakingString (null);
		}

		[DllImport ("Foo")]
		static extern void SomeMethodTakingString (String str);

		[ExpectedWarning ("IL2050")]
		static void Call_GetInterface ()
		{
			GetInterface ();
		}

		[DllImport ("Foo")]
		static extern IFoo GetInterface ();

		[UnconditionalSuppressMessage ("trim", "IL2050")]
		static void Call_CanSuppressWarningOnParameter ()
		{
			CanSuppressWarningOnParameter (null);
		}

		[DllImport ("Foo")]
		static extern void CanSuppressWarningOnParameter ([MarshalAs (UnmanagedType.IUnknown)] object obj);

		[UnconditionalSuppressMessage ("trim", "IL2050")]
		static void Call_CanSuppressWarningOnReturnType ()
		{
			CanSuppressWarningOnReturnType ();
		}

		[DllImport ("Foo")]
		static extern IFoo CanSuppressWarningOnReturnType ();

		[RequiresUnreferencedCode ("test")]
		// Bug https://github.com/dotnet/linker/issues/2378
		[ExpectedWarning ("IL2050", ProducedBy = ProducedBy.Analyzer)]
		static void Call_CanSuppressWithRequiresUnreferencedCode ()
		{
			CanSuppressWithRequiresUnreferencedCode (null);
		}

		[DllImport ("Foo")]
		static extern void CanSuppressWithRequiresUnreferencedCode (IFoo foo);

		[RequiresUnreferencedCode ("test")]
		// Bug https://github.com/dotnet/linker/issues/2378
		[ExpectedWarning ("IL2050", ProducedBy = ProducedBy.Analyzer)]
		static void Call_CanSuppressPInvokeWithRequiresUnreferencedCode ()
		{
			CanSuppressPInvokeWithRequiresUnreferencedCode (null);
		}

		[RequiresUnreferencedCode ("test")]
		[DllImport ("Foo")]
		static extern void CanSuppressPInvokeWithRequiresUnreferencedCode (IFoo foo);

		[ExpectedWarning ("IL2050")]
		[ExpectedWarning ("IL2026")]
		static void Call_PInvokeWithRequiresUnreferencedCode ()
		{
			PInvokeWithRequiresUnreferencedCode (null);
		}

		[RequiresUnreferencedCode ("test")]
		[DllImport ("Foo")]
		static extern void PInvokeWithRequiresUnreferencedCode (IFoo foo);

		interface IFoo { }

		class TestSafeHandle : SafeHandle
		{
			public TestSafeHandle ()
				: base (IntPtr.Zero, true)
			{ }

			public override bool IsInvalid => handle == IntPtr.Zero;

			protected override bool ReleaseHandle ()
			{
				return true;
			}
		}

		class MyCriticalHandle : CriticalHandle
		{
			public MyCriticalHandle () : base (new IntPtr (-1)) { }

			public override bool IsInvalid {
				get { return false; }
			}

			protected override bool ReleaseHandle ()
			{
				return false;
			}
		}

		[StructLayout (LayoutKind.Explicit)]
		public class ExplicitLayout
		{
		}

		[StructLayout (LayoutKind.Sequential)]
		public class SequentialLayout
		{
		}

		[StructLayout (LayoutKind.Auto)]
		public class AutoLayout
		{
		}

	}
}
