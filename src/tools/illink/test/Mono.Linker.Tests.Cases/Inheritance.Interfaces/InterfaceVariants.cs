// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces
{
	[SetupCompileBefore ("copylibrary.dll", new[] { "Dependencies/CopyLibrary.cs" })]
	[SetupLinkerAction ("copy", "copylibrary")]
	public class InterfaceVariants
	{
		public static void Main ()
		{
			Type t = typeof (UninstantiatedPublicClassWithInterface);
			t = typeof (UninstantiatedClassWithImplicitlyImplementedInterface);
			t = typeof (UninstantiatedPublicClassWithPrivateInterface);
			t = typeof (ImplementsUsedStaticInterface.InterfaceMethodUnused);

			ImplementsUnusedStaticInterface.Test ();
			GenericMethodThatCallsInternalStaticInterfaceMethod
				<ImplementsUsedStaticInterface.InterfaceMethodUsedThroughInterface> ();
			// Use all public interfaces - they're marked as public only to denote them as "used"
			typeof (IPublicInterface).RequiresPublicMethods ();
			typeof (IPublicStaticInterface).RequiresPublicMethods ();
			_ = new InstantiatedClassWithInterfaces ();
			MarkIFormattable (null);
		}

		[Kept]
		static void MarkIFormattable (IFormattable x)
		{ }

		[Kept]
		internal static void GenericMethodThatCallsInternalStaticInterfaceMethod<T> () where T : IStaticInterfaceUsed
		{
			T.StaticMethodUsedThroughInterface ();
		}

		[Kept]
		class ImplementsUsedStaticInterface
		{

			[Kept]
			[KeptInterface (typeof (IStaticInterfaceUsed))]
			internal class InterfaceMethodUsedThroughInterface : IStaticInterfaceUsed
			{
				[Kept]
				public static void StaticMethodUsedThroughInterface ()
				{
				}
				public static void UnusedMethod () { }
			}

			[Kept]
			[KeptInterface (typeof (IStaticInterfaceUsed))]
			internal class InterfaceMethodUnused : IStaticInterfaceUsed
			{
				[Kept]
				public static void StaticMethodUsedThroughInterface ()
				{
				}
				public static void UnusedMethod () { }
			}
		}


		[Kept]
		internal class ImplementsUnusedStaticInterface
		{
			[Kept]
			// The interface methods themselves are not used, but the implementation of these methods is
			internal interface IStaticInterfaceMethodUnused
			{
				static abstract void InterfaceUsedMethodNot ();
			}

			internal interface IStaticInterfaceUnused
			{
				static abstract void InterfaceAndMethodNoUsed ();
			}

			// Methods used, but not relevant to variant casting, so iface implementation not kept
			[Kept]
			internal class InterfaceMethodUsedThroughImplementation : IStaticInterfaceMethodUnused, IStaticInterfaceUnused
			{
				[Kept]
				[RemovedOverride (typeof (IStaticInterfaceMethodUnused))]
				public static void InterfaceUsedMethodNot () { }

				[Kept]
				[RemovedOverride (typeof (IStaticInterfaceUnused))]
				public static void InterfaceAndMethodNoUsed () { }
			}

			[Kept]
			[KeptInterface (typeof (IStaticInterfaceMethodUnused))]
			internal class InterfaceMethodUnused : IStaticInterfaceMethodUnused, IStaticInterfaceUnused
			{
				public static void InterfaceUsedMethodNot () { }

				public static void InterfaceAndMethodNoUsed () { }
			}

			[Kept]
			public static void Test ()
			{
				InterfaceMethodUsedThroughImplementation.InterfaceUsedMethodNot ();
				InterfaceMethodUsedThroughImplementation.InterfaceAndMethodNoUsed ();

				Type t;
				t = typeof (IStaticInterfaceMethodUnused);
				t = typeof (InterfaceMethodUnused);
			}
		}

		// Interfaces are kept despite being uninstantiated because it is relevant to variant casting
		[Kept]
		[KeptInterface (typeof (IPublicInterface))]
		[KeptInterface (typeof (IPublicStaticInterface))]
		[KeptInterface (typeof (ICopyLibraryInterface))]
		[KeptInterface (typeof (ICopyLibraryStaticInterface))]
		public class UninstantiatedPublicClassWithInterface :
			IPublicInterface,
			IPublicStaticInterface,
			IInternalInterface,
			IInternalStaticInterface,
			IEnumerator,
			ICopyLibraryInterface,
			ICopyLibraryStaticInterface
		{
			internal UninstantiatedPublicClassWithInterface () { }

			[Kept]
			[IsOverrideOf ("System.Void Mono.Linker.Tests.Cases.Inheritance.Interfaces.InterfaceVariants/IPublicInterface::PublicInterfaceMethod()")]
			public void PublicInterfaceMethod () { }

			[Kept]
			[IsOverrideOf ("System.Void Mono.Linker.Tests.Cases.Inheritance.Interfaces.InterfaceVariants/IPublicInterface::ExplicitImplementationPublicInterfaceMethod()")]
			void IPublicInterface.ExplicitImplementationPublicInterfaceMethod () { }

			[Kept]
			[IsOverrideOf ("System.Void Mono.Linker.Tests.Cases.Inheritance.Interfaces.InterfaceVariants/IPublicStaticInterface::PublicStaticInterfaceMethod()")]
			public static void PublicStaticInterfaceMethod () { }

			[Kept]
			[IsOverrideOf ("System.Void Mono.Linker.Tests.Cases.Inheritance.Interfaces.InterfaceVariants/IPublicStaticInterface::ExplicitImplementationPublicStaticInterfaceMethod()")]
			static void IPublicStaticInterface.ExplicitImplementationPublicStaticInterfaceMethod () { }

			public void InternalInterfaceMethod () { }

			void IInternalInterface.ExplicitImplementationInternalInterfaceMethod () { }

			public static void InternalStaticInterfaceMethod () { }

			static void IInternalStaticInterface.ExplicitImplementationInternalStaticInterfaceMethod () { }


			bool IEnumerator.MoveNext () { throw new PlatformNotSupportedException (); }

			object IEnumerator.Current {
				get { throw new PlatformNotSupportedException (); }
			}

			void IEnumerator.Reset () { }

			[Kept]
			public void CopyLibraryInterfaceMethod () { }

			[Kept]
			void ICopyLibraryInterface.CopyLibraryExplicitImplementationInterfaceMethod () { }

			[Kept]
			public static void CopyLibraryStaticInterfaceMethod () { }

			[Kept]
			static void ICopyLibraryStaticInterface.CopyLibraryExplicitImplementationStaticInterfaceMethod () { }
		}

		[Kept]
		[KeptInterface (typeof (IFormattable))]
		public class UninstantiatedClassWithImplicitlyImplementedInterface : IInternalInterface, IFormattable
		{
			internal UninstantiatedClassWithImplicitlyImplementedInterface () { }

			public void InternalInterfaceMethod () { }

			void IInternalInterface.ExplicitImplementationInternalInterfaceMethod () { }

			[Kept]
			[ExpectBodyModified]
			[ExpectLocalsModified]
			public string ToString (string format, IFormatProvider formatProvider)
			{
				return "formatted string";
			}
		}

		[Kept]
		[KeptInterface (typeof (IPublicInterface))]
		[KeptInterface (typeof (IPublicStaticInterface))]
		[KeptInterface (typeof (ICopyLibraryInterface))]
		[KeptInterface (typeof (ICopyLibraryStaticInterface))]
		public class InstantiatedClassWithInterfaces :
			IPublicInterface,
			IPublicStaticInterface,
			IInternalInterface,
			IInternalStaticInterface,
			IEnumerator,
			ICopyLibraryInterface,
			ICopyLibraryStaticInterface
		{
			[Kept]
			public InstantiatedClassWithInterfaces () { }

			[Kept]
			public void PublicInterfaceMethod () { }

			[Kept]
			void IPublicInterface.ExplicitImplementationPublicInterfaceMethod () { }

			[Kept]
			public static void PublicStaticInterfaceMethod () { }

			[Kept]
			static void IPublicStaticInterface.ExplicitImplementationPublicStaticInterfaceMethod () { }

			public void InternalInterfaceMethod () { }

			void IInternalInterface.ExplicitImplementationInternalInterfaceMethod () { }

			public static void InternalStaticInterfaceMethod () { }

			static void IInternalStaticInterface.ExplicitImplementationInternalStaticInterfaceMethod () { }

			bool IEnumerator.MoveNext () { throw new PlatformNotSupportedException (); }

			object IEnumerator.Current { get { throw new PlatformNotSupportedException (); } }

			void IEnumerator.Reset () { }

			[Kept]
			public void CopyLibraryInterfaceMethod () { }

			[Kept]
			void ICopyLibraryInterface.CopyLibraryExplicitImplementationInterfaceMethod () { }

			[Kept]
			public static void CopyLibraryStaticInterfaceMethod () { }

			[Kept]
			static void ICopyLibraryStaticInterface.CopyLibraryExplicitImplementationStaticInterfaceMethod () { }
		}

		[Kept]
		public class UninstantiatedPublicClassWithPrivateInterface : IPrivateInterface
		{
			internal UninstantiatedPublicClassWithPrivateInterface () { }

			void IPrivateInterface.PrivateInterfaceMethod () { }
		}

		[Kept]
		public interface IPublicInterface
		{
			[Kept]
			void PublicInterfaceMethod ();

			[Kept]
			void ExplicitImplementationPublicInterfaceMethod ();
		}

		[Kept]
		public interface IPublicStaticInterface
		{
			[Kept]
			static abstract void PublicStaticInterfaceMethod ();

			[Kept]
			static abstract void ExplicitImplementationPublicStaticInterfaceMethod ();
		}

		internal interface IInternalInterface
		{
			void InternalInterfaceMethod ();

			void ExplicitImplementationInternalInterfaceMethod ();
		}

		internal interface IInternalStaticInterface
		{
			static abstract void InternalStaticInterfaceMethod ();

			static abstract void ExplicitImplementationInternalStaticInterfaceMethod ();
		}

		// The interface methods themselves are used through the interface
		[Kept]
		internal interface IStaticInterfaceUsed
		{
			[Kept]
			static abstract void StaticMethodUsedThroughInterface ();

			static abstract void UnusedMethod ();
		}

		private interface IPrivateInterface
		{
			void PrivateInterfaceMethod ();
		}
	}
}
