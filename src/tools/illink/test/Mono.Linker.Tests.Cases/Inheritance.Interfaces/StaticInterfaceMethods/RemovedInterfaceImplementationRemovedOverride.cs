// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[ExpectedNoWarnings]
	public class RemovedInterfaceImplementationRemovedOverride
	{
		[Kept]
		public static void Main ()
		{
			Basic.Test ();
			GenericInterface.Test ();
			GenericInterfaceGenericType.Test ();
			PrivateExplicitImplementationReflectedOver.Test ();
			InheritedInterfaces.Test ();
		}

		[Kept]
		public class Basic
		{
			[Kept]
			public static void Test ()
			{
				// Get message via generic method
				var message = MessageFactory.GetMessage<TypeWithMethodAccessedViaInterface> ();
				Console.WriteLine (message);

				// Get message directly from type
				var message2 = TypeWithMethodAccessedDirectly.GetMessage ();
				Console.WriteLine (message2);
			}

			[Kept]
			public static class MessageFactory
			{
				[Kept]
				public static string GetMessage<T> () where T : IStaticMethod
				{
					return T.GetMessage ();
				}
			}

			[Kept]
			[KeptInterface (typeof (IStaticMethod))]
			public class TypeWithMethodAccessedViaInterface : IStaticMethod
			{
				// Force the string to have dynamic element so that it doesn't get compiled in as a literal
				[Kept]
				[KeptOverride (typeof (IStaticMethod))]
				public static string GetMessage () => $"Hello from {nameof (TypeWithMethodAccessedViaInterface)}, the time is {TimeOnly.FromDateTime (DateTime.Now)}";
			}

			[Kept]
			public class TypeWithMethodAccessedDirectly : IStaticMethod
			{
				// Force the string to have dynamic element so that it doesn't get compiled in as a literal
				[Kept]
				[RemovedOverride (typeof (IStaticMethod))]
				public static string GetMessage () => $"Hello from {nameof (TypeWithMethodAccessedDirectly)}, the time is {TimeOnly.FromDateTime (DateTime.Now)}";
			}

			[Kept]
			public interface IStaticMethod
			{
				[Kept]
				abstract static string GetMessage ();
			}
		}

		[Kept]
		public class GenericInterface
		{
			[Kept]
			public static void Test ()
			{
				// Get message via generic method
				var message = MessageFactory.GetMessage<TypeWithMethodAccessedViaInterface, int> ();
				Console.WriteLine (message);

				// Get message directly from type
				var message2 = TypeWithMethodAccessedDirectly.GetMessage ();
				Console.WriteLine (message2);
			}

			[Kept]
			public static class MessageFactory
			{
				[Kept]
				public static U GetMessage<T, U> () where T : IStaticAbstractMethodGeneric<U>
				{
					return T.GetMessage ();
				}
			}

			[Kept]
			[KeptInterface (typeof (IStaticAbstractMethodGeneric<int>))]
			public class TypeWithMethodAccessedViaInterface : IStaticAbstractMethodGeneric<int>
			{
				// Force the string to have dynamic element so that it doesn't get compiled in as a literal
				[Kept]
				[KeptOverride (typeof (IStaticAbstractMethodGeneric<int>))]
				public static int GetMessage () => 0;
			}

			[Kept]
			public class TypeWithMethodAccessedDirectly : IStaticAbstractMethodGeneric<int>
			{
				// Force the string to have dynamic element so that it doesn't get compiled in as a literal
				[Kept]
				[RemovedOverride (typeof (IStaticAbstractMethodGeneric<int>))]
				public static int GetMessage () => 0;
			}

			[Kept]
			public interface IStaticAbstractMethodGeneric<T>
			{
				[Kept]
				abstract static T GetMessage ();
			}
		}

		[Kept]
		public class GenericInterfaceGenericType
		{
			[Kept]
			public static void Test ()
			{
				// Get message via generic method
				var message = MessageFactory.GetMessage<TypeWithMethodAccessedViaInterface<int>, int> ();
				Console.WriteLine (message);

				// Get message directly from type
				var message2 = TypeWithMethodAccessedDirectly<int>.GetMessage ();
				Console.WriteLine (message2);
			}

			[Kept]
			public static class MessageFactory
			{
				[Kept]
				public static U GetMessage<T, U> () where T : IStaticAbstractMethodGeneric<U>
				{
					return T.GetMessage ();
				}
			}

			[Kept]
			[KeptInterface (typeof (IStaticAbstractMethodGeneric<>), "T")]
			public class TypeWithMethodAccessedViaInterface<T> : IStaticAbstractMethodGeneric<T>
			{
				// Force the string to have dynamic element so that it doesn't get compiled in as a literal
				[Kept]
				[KeptOverride ("Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.RemovedInterfaceImplementationRemovedOverride/" +
					"GenericInterfaceGenericType/IStaticAbstractMethodGeneric`1<T>")]
				public static T GetMessage () => default;
			}

			[Kept]
			public class TypeWithMethodAccessedDirectly<T> : IStaticAbstractMethodGeneric<T>
			{
				// Force the string to have dynamic element so that it doesn't get compiled in as a literal
				[Kept]
				[RemovedOverride ("Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.RemovedInterfaceImplementationRemovedOverride/" +
					"GenericInterfaceGenericType/IStaticAbstractMethodGeneric`1<T>")]
				public static T GetMessage () => default;
			}

			[Kept]
			public interface IStaticAbstractMethodGeneric<T>
			{
				[Kept]
				abstract static T GetMessage ();
			}
		}

		[Kept]
		public class PrivateExplicitImplementationReflectedOver
		{
			[Kept]
			public static void Test ()
			{
				IInstanceMethod i = new MyType ();
				Console.WriteLine (i.GetMessage ());
				var type = typeof (MyTypeReflected);
				var method = type.GetMethod ("GetMessage");
				method.Invoke (null, null);
			}

			[Kept]
			public interface IInstanceMethod
			{
				[Kept]
				public string GetMessage ();
			}

			[Kept]
			[KeptInterface (typeof (IInstanceMethod))]
			public class MyType : IInstanceMethod
			{
				[Kept]
				public MyType () { }

				[Kept]
				[KeptOverride (typeof (IInstanceMethod))]
				string IInstanceMethod.GetMessage () => "hello";
			}

			[Kept]
			// Reflecting over MyTypeReflected to get the GetMessage method makes MyTypeReflected 'RelevantToVariantCasting' which marks the interface
			// It's hard to think of a scenario where the method would be kept but not the interface implementation
			[KeptInterface (typeof (IInstanceMethod))]
			public class MyTypeReflected : IInstanceMethod
			{
				public MyTypeReflected () { }

				[Kept]
				[KeptOverride (typeof (IInstanceMethod))]
				[ExpectBodyModified]
				string IInstanceMethod.GetMessage () => "hello";
			}
		}

		[Kept]
		public static class InheritedInterfaces
		{
			[Kept]
			public static void Test ()
			{
				UseInterface<UsedThroughInterface> ();
				UsedDirectly.M ();
			}

			[Kept]
			static void UseInterface<T> () where T : IDerived
			{
				T.M ();
			}

			[Kept]
			interface IBase
			{
				[Kept]
				static abstract void M ();
			}

			[Kept]
			[KeptInterface (typeof (IBase))]
			interface IDerived : IBase { }

			[Kept]
			[KeptInterface (typeof (IDerived))]
			[KeptInterface (typeof (IBase))]
			class UsedThroughInterface : IDerived
			{
				[Kept]
				[KeptOverride (typeof (IBase))]
				public static void M () { }
			}

			[Kept]
			class UsedDirectly : IDerived
			{
				[Kept]
				[RemovedOverride (typeof (IBase))]
				public static void M () { }
			}
		}
	}
}
