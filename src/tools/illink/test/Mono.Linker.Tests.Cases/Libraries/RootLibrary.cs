// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[IgnoreTestCase ("NativeAOT doesn't implement library trimming the same way", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	[SetupCompileBefore ("copylibrary.dll", new[] { "Dependencies/CopyLibrary.cs" })]
	[SetupLinkerAction ("copy", "copylibrary")]
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[VerifyMetadataNames]
	public class RootLibrary
	{
		private int field;

		[Kept]
		public RootLibrary ()
		{
		}

		[Kept]
		public static void Main ()
		{
			var t = typeof (SerializationTestPrivate);
			t = typeof (SerializationTestNested.SerializationTestPrivate);
		}

		[Kept]
		public void UnusedPublicMethod ()
		{
		}

		[Kept]
		protected void UnusedProtectedMethod ()
		{
		}

		[Kept]
		protected internal void UnusedProtectedInternalMethod ()
		{
		}

		protected private void UnusedProtectedPrivateMethod ()
		{
		}

		internal void UnusedInternalMethod ()
		{
		}

		private void UnusedPrivateMethod ()
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DynamicDependencyAttribute))]
		[DynamicDependency (nameof (MethodWithDynamicDependencyTarget))]
		public void MethodWithDynamicDependency ()
		{
		}

		[Kept]
		private void MethodWithDynamicDependencyTarget ()
		{
		}

		[Kept]
		public class SerializationTest
		{
			[Kept]
			private SerializationTest (SerializationInfo info, StreamingContext context)
			{
			}
		}

		[Kept]
		private class SerializationTestPrivate
		{
			[Kept]
			private SerializationTestPrivate (SerializationInfo info, StreamingContext context)
			{
			}

			public void NotUsed ()
			{
			}

			[Kept]
			[OnSerializing]
			[KeptAttributeAttribute (typeof (OnSerializingAttribute))]
			private void OnSerializingMethod (StreamingContext context)
			{
			}

			[Kept]
			[OnSerialized]
			[KeptAttributeAttribute (typeof (OnSerializedAttribute))]
			private void OnSerializedMethod (StreamingContext context)
			{
			}

			[Kept]
			[OnDeserializing]
			[KeptAttributeAttribute (typeof (OnDeserializingAttribute))]
			private void OnDeserializingMethod (StreamingContext context)
			{
			}

			[Kept]
			[OnDeserialized]
			[KeptAttributeAttribute (typeof (OnDeserializedAttribute))]
			private void OnDeserializedMethod (StreamingContext context)
			{
			}
		}

		[Kept]
		private class SerializationTestNested
		{
			internal class SerializationTestPrivate
			{
				[Kept]
				private SerializationTestPrivate (SerializationInfo info, StreamingContext context)
				{
				}

				public void NotUsed ()
				{
				}
			}

			public void NotUsed ()
			{
			}
		}

		[Kept]
		public class SubstitutionsTest
		{
			private static bool FalseProp { get { return false; } }

			[Kept]
			[ExpectBodyModified]
			public SubstitutionsTest ()
			{
				if (FalseProp)
					LocalMethod ();
			}

			private void LocalMethod ()
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (I))]
		public class IfaceClass : I
		{
			[Kept]
			public IfaceClass ()
			{
			}

			[Kept]
			public override string ToString ()
			{
				return "test";
			}
		}

		[Kept]
		public interface I
		{
		}

		[Kept]
		[KeptInterface (typeof (IEnumerator))]
		[KeptInterface (typeof (IPublicInterface))]
		[KeptInterface (typeof (IPublicStaticInterface))]
		[KeptInterface (typeof (IInternalInterface))]
		[KeptInterface (typeof (IInternalStaticInterface))]
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
			public void PublicInterfaceMethod () { }

			[Kept]
			void IPublicInterface.ExplicitImplementationPublicInterfaceMethod () { }

			[Kept]
			public static void PublicStaticInterfaceMethod () { }

			[Kept]
			static void IPublicStaticInterface.ExplicitImplementationPublicStaticInterfaceMethod () { }

			[Kept]
			public void InternalInterfaceMethod () { }

			[Kept]
			void IInternalInterface.ExplicitImplementationInternalInterfaceMethod () { }

			[Kept]
			public static void InternalStaticInterfaceMethod () { }

			[Kept]
			static void IInternalStaticInterface.ExplicitImplementationInternalStaticInterfaceMethod () { }

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
		[KeptInterface (typeof (IInternalInterface))]
		[KeptInterface (typeof (IFormattable))]
		public class UninstantiatedPublicClassWithImplicitlyImplementedInterface : IInternalInterface, IFormattable
		{
			internal UninstantiatedPublicClassWithImplicitlyImplementedInterface () { }

			[Kept]
			public void InternalInterfaceMethod () { }

			[Kept]
			void IInternalInterface.ExplicitImplementationInternalInterfaceMethod () { }

			[Kept]
			public string ToString (string format, IFormatProvider formatProvider)
			{
				return "formatted string";
			}
		}

		[Kept]
		[KeptInterface (typeof (IEnumerator))]
		[KeptInterface (typeof (IPublicInterface))]
		[KeptInterface (typeof (IPublicStaticInterface))]
		[KeptInterface (typeof (IInternalInterface))]
		[KeptInterface (typeof (IInternalStaticInterface))]
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

			[Kept]
			public void InternalInterfaceMethod () { }

			[Kept]
			void IInternalInterface.ExplicitImplementationInternalInterfaceMethod () { }

			[Kept]
			public static void InternalStaticInterfaceMethod () { }

			[Kept]
			static void IInternalStaticInterface.ExplicitImplementationInternalStaticInterfaceMethod () { }

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
		[KeptInterface (typeof (IPrivateInterface))]
		public class UninstantiatedPublicClassWithPrivateInterface : IPrivateInterface
		{
			internal UninstantiatedPublicClassWithPrivateInterface () { }

			[Kept]
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

		[Kept]
		internal interface IInternalInterface
		{
			[Kept]
			void InternalInterfaceMethod ();

			[Kept]
			void ExplicitImplementationInternalInterfaceMethod ();
		}

		[Kept]
		internal interface IInternalStaticInterface
		{
			[Kept] // https://github.com/dotnet/linker/issues/2733
			static abstract void InternalStaticInterfaceMethod ();

			[Kept]
			static abstract void ExplicitImplementationInternalStaticInterfaceMethod ();
		}

		[Kept]
		private interface IPrivateInterface
		{
			[Kept]
			void PrivateInterfaceMethod ();
		}
	}

	internal class RootLibrary_Internal
	{
		protected RootLibrary_Internal (SerializationInfo info, StreamingContext context)
		{
		}

		internal void Unused ()
		{
		}
	}
}
