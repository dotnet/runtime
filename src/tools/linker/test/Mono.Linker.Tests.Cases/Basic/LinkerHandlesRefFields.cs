// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.Basic
{
	/// <summary>
	/// This test is only to ensure that the linker does not crash when programs use ref fields (new to dotnet 7). This test does not validate any expected behaviors around ref fields.
	/// </summary>
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	class LinkerHandlesRefFields
	{
		ref struct RS
		{
			public ref int i;
			public ref double d;
			public RS2 rs2;
			public ref object obj;
			public ref Type t;
			public void MoveThis ()
			{
				this = new RS ();
			}
		}

		ref struct RS2
		{
			public ref int i;
			public ref double d;
			public ref object obj;
			public ref Type t;
			public void MoveThis ()
			{
				this = new RS2 ();
			}
		}

		ref struct RsAnnotations
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public ref Type tDam;

			[RequiresUnreferencedCode ("message for " + nameof (RefReturnWithMethods_HasRuc))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static ref Type RefReturnWithMethods_HasRuc (ref RsAnnotations rs)
			{
				return ref rs.tDam;
			}
		}

		interface IRS
		{
			void RSParam (ref RS rsRef, RS rs);
			RS RSReturn ();
			ref RS2 RSParamAndReturn (ref RS rs);
		}

		class Base : IRS
		{
			public virtual void RSParam (ref RS rsRef, RS rs) { }
			public virtual RS RSReturn () { return new RS (); }
			public virtual ref RS2 RSParamAndReturn (ref RS rs)
			{
				return ref rs.rs2;
			}
		}

		class Derived : Base, IRS
		{
			public override void RSParam (ref RS rsRef, RS rs) { }
			public override RS RSReturn () { return new RS (); }
		}

		static void TestVirtualMethods ()
		{
			typeof (Base).RequiresPublicMethods ();
			typeof (Derived).RequiresPublicMethods ();
		}

		// Ref structs can't implement interfaces

		delegate RS2 RsParamRs2Return (RS rs);
		delegate ref RS2 RefRSParamRefRs2Return (ref RS rs);
		delegate ref int RsParamRefIntReturn (RS rs);

		[ExpectedWarning ("IL2026", "message for " + nameof (RsAnnotations.RefReturnWithMethods_HasRuc))]
		static void CallAnnotatedMethod ()
		{
			RsAnnotations rsa = new RsAnnotations ();
			RsAnnotations.RefReturnWithMethods_HasRuc (ref rsa);
		}
		public static void Main (string[] args)
		{
			scoped RS rs = new RS ();
			scoped RS2 rs2 = new RS2 ();

			// Assign by value
			rs.i = rs2.i;
			rs2.d = rs2.d;
			rs.obj = rs2.obj;
			rs2.t = rs.t;

			// Assign by ref
			rs.i = ref rs2.i;
			rs2.d = ref rs2.d;
			rs.obj = ref rs2.obj;
			rs2.t = ref rs.t;

			// Assign in different basic blocks
			if (args[0] == "") {
				rs.i = ref rs2.i;
				rs.d = rs2.d;
				rs.obj = ref rs2.obj;
				rs.t = rs2.t;
			} else {
				rs2.i = rs.i;
				rs2.d = ref rs.d;
				rs2.obj = rs.obj;
				rs2.t = ref rs.t;
			}

			// Cast fields
			rs.t = (Type) rs.obj;
			rs.i = (int) rs.d;
			rs2.i = (int) rs.d;

			// In Lambdas
			RsParamRs2Return f = (RS rs) => rs.rs2;
			rs.rs2 = f (rs);
			RefRSParamRefRs2Return h = (ref RS rs) => ref rs.rs2;
			rs.rs2 = h (ref rs);
			RsParamRefIntReturn g = (RS rs) => ref f (rs).i;
			rs.i = g (rs);

			// As parameters and returns for local functions
			RS LocalMethod (RS2 rs)
			{
				rs.d = 0.2;
				return new RS ();
			}
			rs = LocalMethod (rs.rs2);

			ref RS LocalMethodRef (ref RS rs)
			{
				return ref rs;
			}
			ref RS refRs = ref LocalMethodRef (ref rs);
			refRs = LocalMethodRef (ref rs);

			ref int ReturnRefInt (ref int i)
			{
				return ref i;
			}
			rs.i = ReturnRefInt (ref rs2.i);

			// Call methods with this
			rs.MoveThis ();
			rs2.MoveThis ();

			CallAnnotatedMethod ();
			TestVirtualMethods ();
		}
	}
}
