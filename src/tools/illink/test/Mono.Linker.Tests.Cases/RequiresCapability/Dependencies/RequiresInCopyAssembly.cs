// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability.Dependencies
{
	public class RequiresInCopyAssembly
	{
		public RequiresInCopyAssembly ()
		{
		}

		[RequiresUnreferencedCode ("Message for --Method--")]
		[RequiresAssemblyFiles ("Message for --Method--")]
		[RequiresDynamicCode ("Message for --Method--")]
		public void Method ()
		{
		}

		[RequiresUnreferencedCode ("Message for --UncalledMethod--")]
		[RequiresAssemblyFiles ("Message for --UncalledMethod--")]
		[RequiresDynamicCode ("Message for --UncalledMethod--")]
		public void UncalledMethod ()
		{
		}

		[RequiresUnreferencedCode ("Message for --MethodCalledThroughReflection--")]
		[RequiresAssemblyFiles ("Message for --MethodCalledThroughReflection--")]
		[RequiresDynamicCode ("Message for --MethodCalledThroughReflection--")]
		static void MethodCalledThroughReflection ()
		{
		}

		public int UnusedProperty {
			[RequiresUnreferencedCode ("Message for --getter UnusedProperty--")]
			[RequiresAssemblyFiles ("Message for --getter UnusedProperty--")]
			[RequiresDynamicCode ("Message for --getter UnusedProperty--")]
			get { return 42; }

			[RequiresUnreferencedCode ("Message for --setter UnusedProperty--")]
			[RequiresAssemblyFiles ("Message for --setter UnusedProperty--")]
			[RequiresDynamicCode ("Message for --setter UnusedProperty--")]
			set { }
		}

		class UnusedBaseType
		{
			[RequiresUnreferencedCode ("Message for --UnusedBaseTypeCctor--")]
			[RequiresAssemblyFiles ("Message for --UnusedBaseTypeCctor--")]
			[RequiresDynamicCode ("Message for --UnusedBaseTypeCctor--")]
			static UnusedBaseType ()
			{
			}

			[RequiresUnreferencedCode ("Message for --UnusedVirtualMethod1--")]
			[RequiresAssemblyFiles ("Message for --UnusedVirtualMethod1--")]
			[RequiresDynamicCode ("Message for --UnusedVirtualMethod1--")]
			public virtual void UnusedVirtualMethod1 ()
			{
			}

			[RequiresUnreferencedCode ("Message for --UnusedVirtualMethod2--")]
			[RequiresAssemblyFiles ("Message for --UnusedVirtualMethod2--")]
			[RequiresDynamicCode ("Message for --UnusedVirtualMethod2--")]
			public virtual void UnusedVirtualMethod2 ()
			{
			}
		}

		class UnusedDerivedType : UnusedBaseType
		{
			[RequiresUnreferencedCode ("Message for --UnusedVirtualMethod1--")]
			[RequiresAssemblyFiles ("Message for --UnusedVirtualMethod1--")]
			[RequiresDynamicCode ("Message for --UnusedVirtualMethod1--")]
			public override void UnusedVirtualMethod1 ()
			{
			}

			// Should not warn when this is part of a copied assembly.
			public override void UnusedVirtualMethod2 ()
			{
			}
		}

		interface IUnusedInterface
		{
			[RequiresUnreferencedCode ("Message for --IUnusedInterface.UnusedMethod--")]
			[RequiresAssemblyFiles ("Message for --IUnusedInterface.UnusedMethod--")]
			[RequiresDynamicCode ("Message for --IUnusedInterface.UnusedMethod--")]
			public void UnusedMethod ();
		}

		class UnusedImplementationClass : IUnusedInterface
		{
			[RequiresUnreferencedCode ("Message for --UnusedImplementationClass.UnusedMethod--")]
			[RequiresAssemblyFiles ("Message for --UnusedImplementationClass.UnusedMethod--")]
			[RequiresDynamicCode ("Message for --UnusedImplementationClass.UnusedMethod--")]
			public void UnusedMethod ()
			{
			}
		}

		public interface IBaseInterface
		{
			[RequiresUnreferencedCode ("Message for --IBaseInterface.MethodInBaseInterface--")]
			[RequiresAssemblyFiles ("Message for --IBaseInterface.MethodInBaseInterface--")]
			[RequiresDynamicCode ("Message for --IBaseInterface.MethodInBaseInterface--")]
			void MethodInBaseInterface ();
		}

		public interface IDerivedInterface : IBaseInterface
		{
			[RequiresUnreferencedCode ("Message for --IDerivedInterface.MethodInDerivedInterface--")]
			[RequiresAssemblyFiles ("Message for --IDerivedInterface.MethodInDerivedInterface--")]
			[RequiresDynamicCode ("Message for --IDerivedInterface.MethodInDerivedInterface--")]
			void MethodInDerivedInterface ();
		}
	}
}
