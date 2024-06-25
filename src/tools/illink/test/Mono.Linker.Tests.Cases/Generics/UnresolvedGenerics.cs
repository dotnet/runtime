using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Generics.Dependencies;

namespace Mono.Linker.Tests.Cases.Generics
{
	[IgnoreTestCase ("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/103843", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	[SetupCompileBefore ("UnresolvedGenericsLibrary.dll", new[] { "Dependencies/UnresolvedGenericsLibrary.cs" },
		removeFromLinkerInput: true)]
	class UnresolvedGenerics
	{
		static void Main ()
		{
			UnresolvedGenericType ();
			UnresolvedGenericBaseType ();
			UnresolvedGenericMethod ();
		}

		[Kept]
		class GenericTypeArgument { }

		[Kept]
		static void UnresolvedGenericType ()
		{
			new UnresolvedGenericsLibrary.GenericClass<GenericTypeArgument> ();
		}

		[Kept]
		class BaseTypeGenericArgument { }

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (UnresolvedGenericsLibrary.GenericClass<BaseTypeGenericArgument>))]
		class HasBaseType : UnresolvedGenericsLibrary.GenericClass<BaseTypeGenericArgument> { }

		[Kept]
		static void UnresolvedGenericBaseType ()
		{
			new HasBaseType ();
		}

		[Kept]
		class GenericMethodArgument { }

		[Kept]
		static void UnresolvedGenericMethod ()
		{
			UnresolvedGenericsLibrary.GenericMethod<GenericMethodArgument> ();
		}
	}
}
