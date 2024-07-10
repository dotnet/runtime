// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces
{
	public class InterfaceVariantsGeneric
	{
		public static void Main ()
		{
			G<int, float> g = new C ();
			g.M (1, 2.0f);
		}

		[Kept]
		interface G<T, U>
		{
			[Kept]
			void M (T t, U u);
		}
		[Kept]
		public class MyT { }
		[Kept]
		public class MyU { }
		[Kept]
		[KeptInterface (typeof (G<int, float>))]
		[KeptInterface (typeof (G<long, double>))]
		[KeptInterface (typeof (G<MyT, MyU>))]
		[KeptMember (".ctor()")]
		public class C : G<int, float>, G<long, double>, G<MyT, MyU>
		{
			[Kept]
			public void M (int t, float u) { }

			public void M (long t, double u) { }

			[Kept]
			public void M (MyT t, MyU u) { }

			[Kept]
			void G<long,double>.M(long t, double u) { }
		}
	}
}
