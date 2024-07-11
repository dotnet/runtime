// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class InterfaceImplementedThroughBaseValidation
	{
		public static void Main ()
		{
			RUCOnInterfaceMethod.Test ();
			RUCOnBaseMethod.Test ();
			DAMOnInterfaceMethod.Test ();
			DAMOnBaseMethod.Test ();

			RUCOnInterfaceWithDIM.Test ();
			RUCOnDIM.Test ();
			DAMOnInterfaceWithDIM.Test ();
			DAMOnDIM.Test ();

			GenericVirtualMethod.Test ();
			GenericVirtualMethodWithDIM.Test ();

			CalledThroughConstraint.Test ();
			CalledThroughConstraintWithDIM.Test ();
		}

		class RUCOnInterfaceMethod
		{
			interface Interface {
				[RequiresUnreferencedCode (nameof (Method))]
				void Method ();
			}

			class Base {
				public void Method () {}
			}

			[ExpectedWarning ("IL2046")]
			class Derived : Base, Interface {}

			[ExpectedWarning ("IL2026")]
			public static void Test () {
				Interface i = new Derived ();
				i.Method ();
			}
		}

		class RUCOnBaseMethod
		{
			interface Interface {
				void Method ();
			}

			class Base {
				[RequiresUnreferencedCode (nameof (Method))]
				public void Method () {}
			}

			[ExpectedWarning ("IL2046")]
			class Derived : Base, Interface {}

			public static void Test () {
				Interface i = new Derived ();
				i.Method ();
			}
		}

		class DAMOnInterfaceMethod
		{
			interface Interface {
				void Method ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t);
			}

			class Base {
				public void Method (Type t) {}
			}

			[ExpectedWarning ("IL2092")]
			class Derived : Base, Interface {}

			public static void Test () {
				Interface i = new Derived ();
				i.Method (typeof (int));
			}
		}

		class DAMOnBaseMethod
		{
			interface Interface {
				void Method (Type t);
			}

			class Base {
				public void Method ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t) {}
			}

			[ExpectedWarning ("IL2092")]
			class Derived : Base, Interface {}

			public static void Test () {
				Interface i = new Derived ();
				i.Method (typeof (int));
			}
		}

		class RUCOnInterfaceWithDIM
		{
			interface Interface {
				[RequiresUnreferencedCode (nameof (Method))]
				void Method ();
			}

			interface InterfaceImpl : Interface {
				[ExpectedWarning ("IL2046")]
				[ExpectedWarning ("IL2046", Tool.Analyzer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/104627")]
				[ExpectedWarning ("IL2046", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/104627")]
				void Interface.Method() {}
			}

			class C : InterfaceImpl {}

			class D : InterfaceImpl {}

			[ExpectedWarning ("IL2026")]
			public static void Test () {
				Interface i = new C ();
				i = new D ();
				i.Method ();
			}
		}

		class RUCOnDIM
		{
			interface Interface {
				void Method ();
			}

			interface InterfaceImpl : Interface {
				[ExpectedWarning ("IL2046")]
				[ExpectedWarning ("IL2046", Tool.Analyzer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/104627")]
				[ExpectedWarning ("IL2046", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/104627")]
				[RequiresUnreferencedCode (nameof (Method))]
				void Interface.Method() {}
			}

			class C : InterfaceImpl {}

			class D : InterfaceImpl {}

			public static void Test () {
				Interface i = new C ();
				i = new D ();
				i.Method ();
			}
		}

		class DAMOnInterfaceWithDIM
		{
			interface Interface {
				void Method ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t);
			}

			interface InterfaceImpl : Interface {
				[ExpectedWarning ("IL2092")]
				[ExpectedWarning ("IL2092", Tool.Analyzer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/104627")]
				[ExpectedWarning ("IL2092", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/104627")]
				void Interface.Method (Type t) {}
			}

			class C : InterfaceImpl {}

			class D : InterfaceImpl {}

			public static void Test () {
				Interface i = new C ();
				i = new D ();
				i.Method (typeof (int));
			}
		}

		class DAMOnDIM
		{
			interface Interface {
				void Method (Type t);
			}

			interface InterfaceImpl : Interface {
				[ExpectedWarning ("IL2092")]
				[ExpectedWarning ("IL2092", Tool.Analyzer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/104627")]
				[ExpectedWarning ("IL2092", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/104627")]
				void Interface.Method ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t) {}
			}

			class C : InterfaceImpl {}

			class D : InterfaceImpl {}

			public static void Test () {
				Interface i = new C ();
				i = new D ();
				i.Method (typeof (int));
			}
		}

		class CalledThroughConstraint
		{
			interface Interface {
				[RequiresUnreferencedCode (nameof (Method))]
				void Method ();
			}

			struct Struct : Interface {
				[ExpectedWarning ("IL2046", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/97676")]
				// NativeAot produces one warning for each call to NoteOverridingMethod.
				// This is done on each callsite while compiling generic code with constraints.
				[ExpectedWarning ("IL2046", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/97676")]
				[ExpectedWarning ("IL2046", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/97676")]
				public void Method () {}
			}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL2026")]
			static void Call<T> (T t) where T : Interface {
				t.Method ();
				t.Method ();
				t.Method ();
			}

			public static void Test () {
				Call(new Struct ());
			}
		}

		class CalledThroughConstraintWithDIM
		{
			// Instance DIMs require boxing the struct.
			// let's try a static DIM.
			interface Interface {
				[RequiresUnreferencedCode (nameof (Method))]
				static abstract void Method ();
			}

			interface InterfaceImpl : Interface {
				[ExpectedWarning ("IL2046")]
				[ExpectedWarning ("IL2046", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/104702")]
				static void Interface.Method() {}
			}

			struct Struct : InterfaceImpl {}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL2026")]
			static void Call<T> () where T : Interface {
				T.Method ();
				T.Method ();
			}

			public static void Test () {
				Call<Struct> ();
			}
		}
		
		class GenericVirtualMethod
		{
			interface Interface {
				[RequiresUnreferencedCode (nameof (Method))]
				void Method<T> ();
			}

			class Base {
				public void Method<T> () {}
			}

			[ExpectedWarning ("IL2046")]
			class Derived : Base, Interface {}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL2026")]
			public static void Test () {
				Interface i = new Derived ();
				i.Method<int> ();
				i.Method<int> ();
			}
		} 

		class GenericVirtualMethodWithDIM
		{
			interface Interface {
				[RequiresUnreferencedCode (nameof (Method))]
				static abstract void Method<T> ();
			}

			interface Impl : Interface {
				[ExpectedWarning ("IL2046")]
				[ExpectedWarning ("IL2046", Tool.NativeAot, "https://github.com/dotnet/runtime/issues/104702")]
				static void Interface.Method<T> () {}
			}

			class Class : Impl {}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL2026")]
			static void Call<T> () where T : Interface {
				T.Method<int> ();
				T.Method<int> ();
			}

			public static void Test () {
				Call<Class> ();
			}
		}
	}
}
