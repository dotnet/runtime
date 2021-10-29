// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
