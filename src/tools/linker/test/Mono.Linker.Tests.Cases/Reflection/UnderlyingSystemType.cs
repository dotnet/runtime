// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	class UnderlyingSystemType
	{
		public static void Main ()
		{
			TestTypeUsedWithUnderlyingSystemType ();
			TestNullValue ();
			TestNoValue ();
		}

		[Kept]
		static class TypeUsedWithUnderlyingSystemType
		{
			[Kept]
			public static void Method () { }

			public static void OtherMethod () { }
		}

		[Kept]
		static void TestTypeUsedWithUnderlyingSystemType ()
		{
			_ = typeof (TypeUsedWithUnderlyingSystemType).UnderlyingSystemType.GetMethod (nameof (TypeUsedWithUnderlyingSystemType.Method));
		}

		[Kept]
		static void TestNullValue ()
		{
			Type t = null;
			t.UnderlyingSystemType.RequiresAll ();
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			t.UnderlyingSystemType.RequiresAll ();
		}
	}
}
