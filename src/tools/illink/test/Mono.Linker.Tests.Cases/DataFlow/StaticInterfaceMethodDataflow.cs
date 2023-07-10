// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	public class StaticInterfaceMethodDataflow
	{
		[Kept]
		public static void Main ()
		{
			DamOnGenericKeepsMethod.Test ();
			DamOnGenericKeepsMethod_UseImplType.Test ();
			DamOnMethodParameter.Test ();
		}

		[Kept]
		static class DamOnGenericKeepsMethod
		{
			[Kept]
			interface IFoo
			{
				[Kept]
				public static virtual void VirtualMethod () { }
			}

			[Kept]
			[KeptInterface (typeof (IFoo))]
			class ImplIFoo : IFoo
			{
				// NativeAOT correctly finds out that the method is not actually used by anything
				// and removes it. The only caveat is GetInterfaceMap - see https://github.com/dotnet/runtimelab/issues/861
				[Kept (By = Tool.Trimmer)]
				public static void VirtualMethod () { }
			}


			[Kept]
			static void MethodWithDamOnType<
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			T> ()
			{
			}

			[Kept]
			public static void Test ()
			{
				MethodWithDamOnType<IFoo> ();
				var _ = typeof (ImplIFoo);
			}
		}

		[Kept]
		static class DamOnGenericKeepsMethod_UseImplType
		{
			[Kept]
			interface IFoo
			{
				[Kept]
				public static virtual void VirtualMethod () { }
			}

			[Kept]
			[KeptInterface (typeof (IFoo))]
			class ImplIFoo : IFoo
			{
				[Kept]
				public static void VirtualMethod () { }
			}


			[Kept]
			static void MethodWithDamOnType<
				[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			T> ()
			{
			}

			[Kept]
			class UseStaticInterface<T> where T : IFoo
			{
				[Kept]
				public static void Do () { T.VirtualMethod (); }
			}

			[Kept]
			public static void Test ()
			{
				MethodWithDamOnType<IFoo> ();
				UseStaticInterface<ImplIFoo>.Do ();
			}
		}

		[Kept]
		static class DamOnMethodParameter
		{
			[Kept]
			interface IFoo
			{
				[Kept]
				static virtual void VirtualMethod () { }
				[Kept]
				static abstract void AbstractMethod ();
			}

			[Kept]
			[KeptInterface (typeof (IFoo))]
			class ImplIFoo : IFoo
			{
				// NativeAOT correctly finds out that the method is not actually used by anything
				// and removes it. The only caveat is GetInterfaceMap - see https://github.com/dotnet/runtimelab/issues/861
				[Kept (By = Tool.Trimmer)]
				public static void VirtualMethod () { }
				// NativeAOT correctly finds out that the method is not actually used by anything
				// and removes it. The only caveat is GetInterfaceMap - see https://github.com/dotnet/runtimelab/issues/861
				[Kept (By = Tool.Trimmer)]
				public static void AbstractMethod () { }
			}

			[Kept]
			static void DamOnParam (
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				Type type)
			{ }

			[Kept]
			public static void Test ()
			{
				DamOnParam (typeof (IFoo));
				var _ = typeof (ImplIFoo);
			}
		}
	}
}
