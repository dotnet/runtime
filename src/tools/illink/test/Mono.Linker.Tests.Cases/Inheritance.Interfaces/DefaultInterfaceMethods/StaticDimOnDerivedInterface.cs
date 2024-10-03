// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	class StaticDimOnDerivedInterface
	{
		[Kept]
		public static void Main ()
		{
			ITest testImpl = new TestClass ();
			testImpl.Invoke ();

			CallStaticInvoke<TestClass> ();
		}

		[Kept]
		public interface ITestBase
		{
			[Kept]
			static virtual string InvokeStatic () => throw new NotSupportedException ();

			[Kept]
			string Invoke () => throw new NotSupportedException ();
		}

		[Kept]
		[KeptInterface (typeof (ITestBase))]
		public interface ITest : ITestBase
		{
			[Kept]
			static string ITestBase.InvokeStatic () => "Test";

			[Kept]
			string ITestBase.Invoke () => "Test";
		}

		[Kept]
		[KeptInterface (typeof (ITest))]
		[KeptInterface (typeof (ITestBase))]
		public class TestClass : ITest
		{
			[Kept]
			public TestClass () { }
		}

		[Kept]
		public static string CallStaticInvoke<T> () where T : ITestBase => T.InvokeStatic ();
	}
}
