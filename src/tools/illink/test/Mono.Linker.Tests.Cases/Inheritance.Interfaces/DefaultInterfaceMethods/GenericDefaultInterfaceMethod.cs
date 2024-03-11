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
	class GenericDefaultInterfaceMethod
	{
		public static void Main()
		{
			new C ();
			((I00<string>) null!).M (0);
		}
		[Kept]
		public interface I0<T>
		{
			[Kept]
			void M (T value);
		}
		[Kept]
		public interface I00<U> : I0<int>
		{
			[Kept]
			void I0<int>.M (int value) { }
		}
		[Kept]
		public class C : I00<int>, I0<int>
		{
		}
	}
}
