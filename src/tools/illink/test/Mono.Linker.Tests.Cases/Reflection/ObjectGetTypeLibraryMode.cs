// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[SetupLinkerArgument ("-a", "test.exe", "library")]
	[ExpectedNoWarnings]
	[KeptMember (".ctor()")]
	public class ObjectGetTypeLibraryMode
	{
		public static void Main ()
		{
			BasicAnnotationWithNoDerivedClasses.Test ();
			BasicNoAnnotationWithNoDerivedClasses.Test ();
		}

		[Kept]
		class BasicAnnotationWithNoDerivedClasses
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public interface IBasicAnnotatedInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IBasicAnnotatedInterface))]
			class ClassImplementingAnnotatedInterface : IBasicAnnotatedInterface
			{
				[Kept]
				public void UsedMethod () { }
				[Kept] // The type is not sealed, so trimmer will apply the annotation from the interface
				public void UnusedMethod () { }
			}

			[Kept]
			static void TestInterface ()
			{
				var classImplementingInterface = new ClassImplementingAnnotatedInterface ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class BasicAnnotatedClass
			{
				[Kept]
				public void UsedMethod () { }
				[Kept] // The type is not sealed, so trimmer will apply the annotation from the interface
				public void UnusedMethod () { }
			}

			[Kept]
			static void TestClass ()
			{
				var instance = new BasicAnnotatedClass ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			struct BasicAnnotatedStruct
			{
				[Kept]
				public void UsedMethod () { }
				[Kept]
				public void UnusedMethod () { }
			}

			[Kept]
			static void TestStruct ()
			{
				var instance = new BasicAnnotatedStruct ();
			}

			[Kept]
			public static void Test ()
			{
				TestInterface ();
				TestClass ();
				TestStruct ();
			}
		}

		[Kept]
		class BasicNoAnnotationWithNoDerivedClasses
		{
			[Kept]
			public interface IBasicNoAnnotatedInterface
			{
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IBasicNoAnnotatedInterface))]
			class ClassImplementingNoAnnotatedInterface : IBasicNoAnnotatedInterface
			{
				public void UsedMethod () { }
				public void UnusedMethod () { }
			}

			[Kept]
			static void TestInterface ()
			{
				var classImplementingInterface = new ClassImplementingNoAnnotatedInterface ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			class BasicNoAnnotatedClass
			{
				public void UsedMethod () { }
				public void UnusedMethod () { }
			}

			[Kept]
			static void TestClass ()
			{
				var instance = new BasicNoAnnotatedClass ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			struct BasicNoAnnotatedStruct
			{
				public void UsedMethod () { }
				public void UnusedMethod () { }
			}

			[Kept]
			static void TestStruct ()
			{
				var instance = new BasicNoAnnotatedStruct ();
			}

			[Kept]
			public static void Test ()
			{
				TestInterface ();
				TestClass ();
				TestStruct ();
			}
		}
	}
}
