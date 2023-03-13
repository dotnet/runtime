// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (
		AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Field,
		AllowMultiple = false,
		Inherited = false)]
	public class ExpectedNoWarningsAttribute : EnableLoggerAttribute
	{
		public ExpectedNoWarningsAttribute () { }
		public ExpectedNoWarningsAttribute (string warningCode) { }
	}
}
