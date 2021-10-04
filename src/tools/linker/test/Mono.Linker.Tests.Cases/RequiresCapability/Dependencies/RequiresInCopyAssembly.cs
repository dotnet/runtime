// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
		public void Method ()
		{
		}

		[RequiresUnreferencedCode ("Message for --UncalledMethod--")]
		public void UncalledMethod ()
		{
		}

		[RequiresUnreferencedCode ("Message for --MethodCalledThroughReflection--")]
		static void MethodCalledThroughReflection ()
		{
		}

		public int UnusedProperty {
			[RequiresUnreferencedCode ("Message for --getter UnusedProperty--")]
			get { return 42; }

			[RequiresUnreferencedCode ("Message for --setter UnusedProperty--")]
			set { }
		}

		class UnusedBaseType
		{
			[RequiresUnreferencedCode ("Message for --UnusedBaseTypeCctor--")]
			static UnusedBaseType ()
			{
			}

			[RequiresUnreferencedCode ("Message for --UnusedVirtualMethod1--")]
			public virtual void UnusedVirtualMethod1 ()
			{
			}

			[RequiresUnreferencedCode ("Message for --UnusedVirtualMethod2--")]
			public virtual void UnusedVirtualMethod2 ()
			{
			}
		}

		class UnusedDerivedType : UnusedBaseType
		{
			[RequiresUnreferencedCode ("Message for --UnusedVirtualMethod1--")]
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
			public void UnusedMethod ();
		}

		class UnusedImplementationClass : IUnusedInterface
		{
			[RequiresUnreferencedCode ("Message for --UnusedImplementationClass.UnusedMethod--")]
			public void UnusedMethod ()
			{
			}
		}

		public interface IBaseInterface
		{
			[RequiresUnreferencedCode ("Message for --IBaseInterface.MethodInBaseInterface--")]
			void MethodInBaseInterface ();
		}

		public interface IDerivedInterface : IBaseInterface
		{
			[RequiresUnreferencedCode ("Message for --IDerivedInterface.MethodInDerivedInterface--")]
			void MethodInDerivedInterface ();
		}
	}
}
