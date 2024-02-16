// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.Libraries.Dependencies
{
	public abstract class UserAssemblyActionWorks_ChildLib
	{
		public abstract void MustOverride ();

		public static void ChildUnusedMethod (InputType input) { }

		private static void ChildUnusedPrivateMethod () { }

		public void ChildUnusedInstanceMethod () { }

		public int UnusedProperty { get; set; }

		public static int UnusedField;
	}

	public class InputType { }
}
