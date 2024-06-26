// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	class BaseProvidesInterfaceMethod
	{
		[Kept]
		public static void Main ()
		{
			CallMethod<Derived> ();
			CallMN<D> ();
		}

		[Kept]
		static void CallMethod<T> () where T : IFoo
		{
			T.Method ();
		}
		[Kept]
		interface IFoo
		{
			[Kept]
			static abstract int Method ();
		}

		[Kept]
		class Base
		{
			[Kept]
			public static int Method () => 0;
		}

		[KeptInterface (typeof (IFoo))]
		[KeptBaseType (typeof (Base))]
		[KeptMember ("Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.BaseProvidesInterfaceMethod.IFoo.Method()")]
		// Compiler generates private explicit implementation that calls Base.Method()
		class Derived : Base, IFoo
		{ }

		[Kept]
		static void CallMN<T> () where T : I
		{
			T.M ();
			T.N ();
		}

		[Kept]
		interface I
		{
			[Kept]
			static abstract string M ();
			[Kept]
			static abstract string N ();
		}

		[Kept]
		[KeptInterface (typeof (I))]
		class B : I
		{
			[Kept]
			public static string M () => "B.M";
			[Kept]
			static string I.N () => "B's I.N";
		}

		[Kept]
		[KeptBaseType (typeof (B))]
		[KeptInterface (typeof (I))]
		class D : B, I
		{
			[Kept]
			static string I.N () => "D's I.N";
		}
	}
}
