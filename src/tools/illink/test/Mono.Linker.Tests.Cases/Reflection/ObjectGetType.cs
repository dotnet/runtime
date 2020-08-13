// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class ObjectGetType
	{
		public static void Main ()
		{
			TestSealed ();
			TestUnsealed ();
		}

		[Kept]
		static void TestSealed ()
		{
			s_sealedClassField = new SealedClass ();
			s_sealedClassField.GetType ().GetMethod ("Method");
		}

		[Kept]
		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetMethod), new Type[] { typeof (string) }, messageCode: "IL2075")]
		static void TestUnsealed ()
		{
			s_unsealedClassField = new UnsealedClass ();

			// GetType call on an unsealed type is not recognized and produces a warning
			s_unsealedClassField.GetType ().GetMethod ("Method");
		}

		[Kept]
		static SealedClass s_sealedClassField;

		[Kept]
		sealed class SealedClass
		{
			[Kept]
			public SealedClass () { }

			[Kept]
			public static void Method () { }

			public static void UnusedMethod () { }
		}

		[Kept]
		static UnsealedClass s_unsealedClassField;

		[Kept]
		class UnsealedClass
		{
			[Kept]
			public UnsealedClass () { }

			public static void Method () { }
		}
	}
}
