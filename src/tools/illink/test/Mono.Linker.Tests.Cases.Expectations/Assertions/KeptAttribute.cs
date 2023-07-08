// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.All, Inherited = false)]
	public class KeptAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		/// <summary>
		/// By default the target should be kept by all platforms
		/// This property can override that by setting only the platforms
		/// which are expected to keep the target.
		/// </summary>
		public Tool By { get; set; } = Tool.TrimmerAnalyzerAndNativeAot;
	}
}
