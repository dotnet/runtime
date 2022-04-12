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
			t = typeof (UninstantiatedPublicClassWithImplicitlyImplementedInterface);
			t = typeof (UninstantiatedPublicClassWithPrivateInterface);

			UninstantiatedPublicClassWithInterface.InternalStaticInterfaceMethodUsed ();
			InstantiatedClassWithInterfaces.InternalStaticInterfaceMethodUsed ();

			// Use all public interfaces - they're marked as public only to denote them as "used"
			typeof (IPublicInterface).RequiresPublicMethods ();
			typeof (IPublicStaticInterface).RequiresPublicMethods ();

			var a = new InstantiatedClassWithInterfaces ();
		}

		[Kept]
		[KeptInterface (typeof (IEnumerator))]
		[KeptInterface (typeof (IPublicInterface))]
		[KeptInterface (typeof (IPublicStaticInterface))]
		[KeptInterface (typeof (IInternalStaticInterfaceWithUsedMethod))] // https://github.com/dotnet/linker/issues/2733
		[KeptInterface (typeof (ICopyLibraryInterface))]
		[KeptInterface (typeof (ICopyLibraryStaticInterface))]
		public class UninstantiatedPublicClassWithInterface :
			IPublicInterface,
			IPublicStaticInterface,
			IInternalInterface,
			IInternalStaticInterface,
			IInternalStaticInterfaceWithUsedMethod,
			IEnumerator,
			ICopyLibraryInterface,
			ICopyLibraryStaticInterface
		{
			internal UninstantiatedPublicClassWithInterface () { }

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

			[Kept]
			public static void InternalStaticInterfaceMethodUsed () { }

			[Kept]
			[ExpectBodyModified]
			bool IEnumerator.MoveNext () { throw new PlatformNotSupportedException (); }

			[Kept]
			object IEnumerator.Current {
				[Kept]
				[ExpectBodyModified]
				get { throw new PlatformNotSupportedException (); }
			}

			[Kept]
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
		public class UninstantiatedPublicClassWithImplicitlyImplementedInterface : IInternalInterface, IFormattable
		{
			internal UninstantiatedPublicClassWithImplicitlyImplementedInterface () { }

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
		[KeptInterface (typeof (IEnumerator))]
		[KeptInterface (typeof (IPublicInterface))]
		[KeptInterface (typeof (IPublicStaticInterface))]
		[KeptInterface (typeof (IInternalStaticInterfaceWithUsedMethod))] // https://github.com/dotnet/linker/issues/2733
		[KeptInterface (typeof (ICopyLibraryInterface))]
		[KeptInterface (typeof (ICopyLibraryStaticInterface))]
		public class InstantiatedClassWithInterfaces :
			IPublicInterface,
			IPublicStaticInterface,
			IInternalInterface,
			IInternalStaticInterface,
			IInternalStaticInterfaceWithUsedMethod,
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

			[Kept]
			public static void InternalStaticInterfaceMethodUsed () { }

			[Kept]
			bool IEnumerator.MoveNext () { throw new PlatformNotSupportedException (); }

			[Kept]
			object IEnumerator.Current { [Kept] get { throw new PlatformNotSupportedException (); } }

			[Kept]
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

		// The interface methods themselves are not used, but the implentation of these methods is
		// https://github.com/dotnet/linker/issues/2733
		[Kept]
		internal interface IInternalStaticInterfaceWithUsedMethod
		{
			[Kept] // https://github.com/dotnet/linker/issues/2733
			static abstract void InternalStaticInterfaceMethodUsed ();
		}

		private interface IPrivateInterface
		{
			void PrivateInterfaceMethod ();
		}
	}
}
