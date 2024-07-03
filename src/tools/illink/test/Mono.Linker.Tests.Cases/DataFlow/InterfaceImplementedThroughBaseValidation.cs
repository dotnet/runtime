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
		}

		class RUCOnInterfaceMethod ()
		{
			interface Interface {
				[RequiresUnreferencedCode (nameof (Method))]
				void Method ();
			}

			class Base {
				[UnexpectedWarning ("IL2046", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/97676")]
				public void Method () {}
			}

			class Derived : Base, Interface {}

			[ExpectedWarning ("IL2026")]
			public static void Test () {
				Interface i = new Derived ();
				i.Method ();
			}
		}

		class RUCOnBaseMethod ()
		{
			interface Interface {
				void Method ();
			}

			class Base {
				[UnexpectedWarning ("IL2046", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/97676")]
				[RequiresUnreferencedCode (nameof (Method))]
				public void Method () {}
			}

			class Derived : Base, Interface {}

			public static void Test () {
				Interface i = new Derived ();
				i.Method ();
			}
		}

		class DAMOnInterfaceMethod ()
		{
			interface Interface {
				void Method ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t);
			}

			class Base {
				[UnexpectedWarning ("IL2092", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/97676")]
				public void Method (Type t) {}
			}

			class Derived : Base, Interface {}

			public static void Test () {
				Interface i = new Derived ();
				i.Method (typeof (int));
			}
		}

		class DAMOnBaseMethod ()
		{
			interface Interface {
				void Method (Type t);
			}

			class Base {
				[UnexpectedWarning ("IL2092", Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/runtime/issues/97676")]
				public void Method ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type t) {}
			}

			class Derived : Base, Interface {}

			public static void Test () {
				Interface i = new Derived ();
				i.Method (typeof (int));
			}
		}
	}
}
