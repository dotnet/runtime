// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
namespace Mono.Linker.Tests.Cases.Attributes
{
	[TestCaseRequirements (TestRunCharacteristics.TargetingNetCore, "Requires net5 or newer")]
	[SetupCompileBefore ("interface.dll", new[] { "Dependencies/IReferencedAssembly.cs" })]
	[SetupCompileBefore ("impl.dll", new[] { "Dependencies/IReferencedAssemblyImpl.cs" },
		references: new[] { "interface.dll" }, addAsReference: false)]
	[KeptMemberInAssembly ("interface.dll", typeof (IReferencedAssembly), "Foo()")]
	[KeptMemberInAssembly ("impl", "Mono.Linker.Tests.Cases.Attributes.Dependencies.IReferencedAssemblyImpl", "Foo()")]
	[KeptInterfaceOnTypeInAssembly ("impl", "Mono.Linker.Tests.Cases.Attributes.Dependencies.IReferencedAssemblyImpl",
		"interface", "Mono.Linker.Tests.Cases.Attributes.Dependencies.IReferencedAssembly")]
	[SetupLinkerTrimMode ("link")]
	[IgnoreDescriptors (false)]
	public class TypeWithDynamicInterfaceCastableImplementationAttributeIsKept
	{
		public static void Main ()
		{
#if NET
			Foo foo = new Foo ();
			GetBar (foo).Bar ();
			IReferenced baz = GetBaz (foo);
			var bar = new DynamicCastableImplementedInOtherAssembly ();
			IReferencedAssembly iReferenced = GetReferencedInterface (bar);
			iReferenced.Foo ();
#endif
		}

#if NET
		[Kept]
		private static IReferencedAndCalled GetBar (object obj)
		{
			return (IReferencedAndCalled) obj;
		}

		[Kept]
		private static IReferenced GetBaz (object obj)
		{
			return (IReferenced) obj;
		}

		[Kept]
		static IReferencedAssembly GetReferencedInterface (object obj)
		{
			return (IReferencedAssembly) obj;
		}
#endif
	}

#if NET
	[Kept]
	[KeptMember (".ctor()")]
	[KeptInterface (typeof (IDynamicInterfaceCastable))]
	class Foo : IDynamicInterfaceCastable
	{
		[Kept]
		public RuntimeTypeHandle GetInterfaceImplementation (RuntimeTypeHandle interfaceType)
		{
			if (interfaceType.Equals (typeof (IReferencedInIDynamicInterfaceCastableType).TypeHandle)) {
				return typeof (IReferencedInIDynamicInterfaceCastableTypeImpl).TypeHandle;
			}
			return default;
		}

		[Kept]
		public bool IsInterfaceImplemented (RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
		{
			return interfaceType.Equals (typeof (IReferencedInIDynamicInterfaceCastableType).TypeHandle);
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptInterface (typeof (IDynamicInterfaceCastable))]
	class DynamicCastableImplementedInOtherAssembly : IDynamicInterfaceCastable
	{
		[Kept]
		public bool IsInterfaceImplemented (RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
		{
			return interfaceType.Equals (typeof (IReferencedAssembly).TypeHandle);
		}

		[Kept]
		public RuntimeTypeHandle GetInterfaceImplementation (RuntimeTypeHandle interfaceType)
		{
			if (interfaceType.Equals (typeof (IReferencedAssembly).TypeHandle)) {
				var type = Type.GetType ("Mono.Linker.Tests.Cases.Attributes.Dependencies.IReferencedAssemblyImpl,impl");
				return type.TypeHandle;
			}
			return default;
		}
	}

	[Kept]
	interface IReferencedAndCalled
	{
		[Kept]
		void Bar ();
	}

	[Kept]
	[KeptAttributeAttribute (typeof (DynamicInterfaceCastableImplementationAttribute))]
	[KeptInterface (typeof (IReferencedAndCalled))]
	[DynamicInterfaceCastableImplementation]
	interface IReferencedAndCalledImpl : IReferencedAndCalled
	{
		[Kept]
		void IReferencedAndCalled.Bar () { }
	}

	[Kept]
	interface IReferenced
	{
		void Baz ();
	}

	[Kept]
	[KeptAttributeAttribute (typeof (DynamicInterfaceCastableImplementationAttribute))]
	[KeptInterface (typeof (IReferenced))]
	[DynamicInterfaceCastableImplementation]
	interface IReferencedImpl : IReferenced
	{
		void IReferenced.Baz () { }
	}

	interface IUnreferenced
	{
		void Frob () { }
	}

	[DynamicInterfaceCastableImplementation]
	interface IUnreferencedImpl : IUnreferenced
	{
		void IUnreferenced.Frob () { }
	}

	[Kept]
	interface IReferencedInIDynamicInterfaceCastableType
	{
		void Foo () { }
	}

	[Kept]
	[KeptAttributeAttribute (typeof (DynamicInterfaceCastableImplementationAttribute))]
	[KeptInterface (typeof (IReferencedInIDynamicInterfaceCastableType))]
	[DynamicInterfaceCastableImplementation]
	interface IReferencedInIDynamicInterfaceCastableTypeImpl : IReferencedInIDynamicInterfaceCastableType
	{
		void IReferencedInIDynamicInterfaceCastableType.Foo () { }
	}
#endif
}
