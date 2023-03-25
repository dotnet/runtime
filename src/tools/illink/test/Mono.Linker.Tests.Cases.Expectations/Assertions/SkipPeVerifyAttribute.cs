// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{

	public enum SkipPeVerifyForToolchian
	{
		Pedump
	}

	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SkipPeVerifyAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public SkipPeVerifyAttribute ()
		{
		}

		public SkipPeVerifyAttribute (SkipPeVerifyForToolchian toolchain)
		{
		}

		public SkipPeVerifyAttribute (string assemblyName)
		{
		}
	}
}
